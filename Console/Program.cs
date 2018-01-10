using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class Program
    {
        private const int RepeatCount = 5;
        private const double DefaultTestSeconds = 1;

        private static readonly ThreadPriority ThreadPriority = ThreadPriority.Normal;
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        private static int _readyCount;
        private static int _finished;

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

                PerformTest<MathWorker>(testSeconds);
                PerformTest<LocalMemoryWorker>(testSeconds);
                PerformTest<FarMemoryWorker>(testSeconds);

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

            Reporter.DisplayMatrixResults(matrix, ProcessorCount);

            DisplayResults<TWorker>(() => RunTest(index => index % 2 == 1, workers, secondsPerTest), "Odd cores");
            DisplayResults<TWorker>(() => RunTest(index => index % 2 == 0, workers, secondsPerTest), "Even cores");
            DisplayResults<TWorker>(() => RunTest(_ => true, workers, secondsPerTest), "All cores");

            Reporter.DisplayTestGroupComplete();
        }

        private static void DisplayResults<TWorker>(Func<IReadOnlyList<WorkerArgs>> action, string description)
        {
            Reporter.DisplayTestInfo(description, typeof(TWorker).Name);

            var args = action();

            Reporter.DisplayResults(args);
        }

        private static IReadOnlyList<WorkerArgs> RunTest(Func<int, bool> coreSelector, IReadOnlyList<IWorker> workers, double testSeconds)
        {
            var bestResultMetric = double.MaxValue;
            IReadOnlyList<WorkerArgs> bestResults = null;

            for (int i = 0; i < RepeatCount; i++)
            {
                var results = RunSingleBenchmark(coreSelector, workers, testSeconds);
                var resultMetric = results.Select(result => result.Report.OpsPerMs).Sum();

                if (resultMetric < bestResultMetric)
                {
                    bestResults = results;
                    bestResultMetric = resultMetric;
                }
            }

            return bestResults;
        }

        private static IReadOnlyList<WorkerArgs> RunSingleBenchmark(Func<int, bool> coreSelector, IReadOnlyList<IWorker> workers, double testSeconds)
        {
            Volatile.Write(ref _finished, 0);
            Volatile.Write(ref _readyCount, 0);

            var coreIndexes = Enumerable.Range(0, ProcessorCount)
                .Where(coreSelector).ToList();

            var workerArgs = coreIndexes
                .Select(index => new WorkerArgs(workers[index], CreateThread(), index, coreIndexes.Count))
                .ToList();

            GC.Collect(2);

            var beforeGc0 = GC.CollectionCount(0);
            var beforeGc1 = GC.CollectionCount(1);
            var beforeGc2 = GC.CollectionCount(2);

            foreach (var workerArg in workerArgs)
            {
                workerArg.Thread.Start(workerArg);
            }

            var cancelSource = new CancellationTokenSource();
            cancelSource.Token.Register(() => Volatile.Write(ref _finished, 1));
            cancelSource.CancelAfter(TimeSpan.FromSeconds(testSeconds));

            foreach (var workerArg in workerArgs)
            {
                workerArg.Thread.Join();
            }

            var afterGc0 = GC.CollectionCount(0);
            var afterGc1 = GC.CollectionCount(1);
            var afterGc2 = GC.CollectionCount(2);

            foreach (var workerArg in workerArgs)
            {
                workerArg.Report.GC0Count = afterGc0 - beforeGc0;
                workerArg.Report.GC1Count = afterGc1 - beforeGc1;
                workerArg.Report.GC2Count = afterGc2 - beforeGc2;
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

            bool ready = false;
            var count = 0;
            var beforeProcessorTime = TimeSpan.Zero;
            var stopwatch = Stopwatch.StartNew();

            Interlocked.Increment(ref _readyCount);

            while (Volatile.Read(ref _finished) == 0)
            {
                workerArgs.Worker.DoWork();

                if (ready)
                {
                    count++;
                }
                else
                {
                    if (Volatile.Read(ref _readyCount) == workerArgs.WorkerCount)
                    {
                        beforeProcessorTime = currentThread.UserProcessorTime;
                        stopwatch.Restart();
                        ready = true;
                    }
                }
            }

            var afterProcessorTime = currentThread.UserProcessorTime;
            stopwatch.Stop();

            SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);
            if (processorNumber.Number != procNumber)
            {
                switchCount++;
            }

            var report = workerArgs.Report;

            report.OperationsCount = count;
            report.Elapsed = stopwatch.Elapsed;
            report.ProcessorTime = afterProcessorTime - beforeProcessorTime;
            report.CoreSwitchCount = switchCount;
        }
    }
}