using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class Program
    {
        private const int RepeatCount = 5;
        private const double DefaultTestSeconds = 15;

        private static readonly ThreadPriority ThreadPriority = ThreadPriority.Normal;
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        public static void Main(string[] args)
        {
            Reporter.DisplayHello();

            try
            {
                var testSeconds = DefaultTestSeconds;
                if (args.Length != 1 || !double.TryParse(args[0], out testSeconds))
                {
                    Reporter.DisplayUsage();
                }

                Reporter.DisplayCpuInfo();

                var repeatCountPerSuite = 3 + (1 + ProcessorCount) * ProcessorCount / 2;
                var timeToRunTests = TimeSpan.FromSeconds(3 * testSeconds * repeatCountPerSuite * RepeatCount);
                Reporter.DisplayBeforeTestInfo(testSeconds, timeToRunTests);

                for (int i = 0; i < RepeatCount; i++)
                {
                    Reporter.DisplayTestRetry(i, RepeatCount);
                    PerformTest<MathWorker>(testSeconds);
                    PerformTest<LocalMemoryWorker>(testSeconds);
                    PerformTest<FarMemoryWorker>(testSeconds);
                }

                Reporter.DisplayBye();
            }
            catch (Exception ex)
            {
                Reporter.DisplayError(ex);
            }
        }

        private static void PerformTest<TWorker>(double secondsPerTest) where TWorker : IWorker, new()
        {
            GC.Collect(2);

            var workers = Enumerable.Range(0, ProcessorCount).Select(_ => (IWorker)new TWorker()).ToList();

            GC.Collect(2);

            foreach (var worker in workers)
            {
                worker.DoWork();
            }

            Reporter.DisplayPairTest(typeof(TWorker).Name);

            var matrix = new double[ProcessorCount, ProcessorCount];

            using (new GCInfo())
            {
                for (var i = 0; i < ProcessorCount; i++)
                {
                    for (var j = i; j < ProcessorCount; j++)
                    {
                        Func<int, bool> coreSelector = index => index == i || index == j;

                        var results = RunTest(coreSelector, workers, secondsPerTest);

                        matrix[i, j] = results.Select(x => x.Report.OpsPerMs).Average();

                        Reporter.DisplayOneMatrixTestDone();
                    }
                }

                Reporter.DisplayMatrixTestsDone();
            }

            Reporter.DisplayMatrixResults(matrix, ProcessorCount);

            DisplayResults<TWorker>(() => RunTest(index => index % 2 == 1, workers, secondsPerTest), "Odd cores");
            DisplayResults<TWorker>(() => RunTest(index => index % 2 == 0, workers, secondsPerTest), "Even cores");
            DisplayResults<TWorker>(() => RunTest(_ => true, workers, secondsPerTest), "All cores");

            Reporter.DisplayTestGroupComplete();
        }

        private static void DisplayResults<TWorker>(Func<IReadOnlyList<WorkerArgs>> action, string description)
        {
            Reporter.DisplayTestInfo(description, typeof(TWorker).Name);

            IReadOnlyList<WorkerArgs> args;
            using (new GCInfo())
            {
                args = action();
            }


            Reporter.DisplayResults(args);
        }

        private static IReadOnlyList<WorkerArgs> RunTest(Func<int, bool> coreSelector, IReadOnlyList<IWorker> workers, double testSeconds)
        {
            var cancelSource = new CancellationTokenSource();

            var workerArgs = Enumerable.Range(0, ProcessorCount)
                .Where(coreSelector)
                .Select(index => new WorkerArgs(workers[index], CreateThread(), index, cancelSource.Token))
                .ToList();

            foreach (var workerArg in workerArgs)
            {
                workerArg.Thread.Start(workerArg);
            }

            cancelSource.CancelAfter(TimeSpan.FromSeconds(testSeconds));

            foreach (var workerArg in workerArgs)
            {
                workerArg.Thread.Join();
            }

            return workerArgs;
        }

        private static Thread CreateThread()
        {
            var thread = new Thread(Runner);
            thread.Priority = ThreadPriority;

            return thread;
        }

        private static void Runner(object arg)
        {
            var processorNumber = new SystemInfoHelper.PROCESSOR_NUMBER();
            var workerArgs = (WorkerArgs) arg;

            var currentThreadId = AppDomain.GetCurrentThreadId();
            var currentThread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>()
                .First(thread => thread.Id == currentThreadId);

            currentThread.ProcessorAffinity = (IntPtr) (1 << workerArgs.Index);

            SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);
            var procNumber = processorNumber.Number;

            int switchCount = 0;

            var processorTimeOnStart = currentThread.TotalProcessorTime;

            var count = 0;
            var stopwatch = Stopwatch.StartNew();

            while (!workerArgs.CancellationToken.IsCancellationRequested)
            {
                SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);
                if (processorNumber.Number != procNumber)
                {
                    switchCount++;
                }

                workerArgs.Worker.DoWork();
                count++;
            }

            stopwatch.Stop();

            var processorTime = currentThread.TotalProcessorTime - processorTimeOnStart;

            var report = workerArgs.Report;

            report.OperationsCount = count;
            report.Elapsed = stopwatch.Elapsed;
            report.CoreSwitchCount = switchCount;
            report.ProcessorTime = processorTime;
        }
    }
}