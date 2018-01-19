using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp.Detectors
{
    internal sealed class HyperthreadingDetector : IDetector
    {
        private const int RepeatCount = 5;

        private static readonly int ProcessorCount = Environment.ProcessorCount;
        private static int _readyToPhase;
        private static int _phaseCompleted;
        private static int _coreCount;

        private static double[] _singleRunResult = new double[ProcessorCount];
        private static double[,] _coreResults = new double[ProcessorCount, ProcessorCount];

        private static CancellationTokenSource _cancelSource;

        public void Perform(double secondsPerTest, double warmSeconds)
        {
            Reporter.DisplayStartTests("Hyperthreading");

            Reporter.DisplayProgressBar((1 + ProcessorCount) * ProcessorCount / 2);

            for (var i = 0; i < ProcessorCount; i++)
            {
                for (var j = i; j < ProcessorCount; j++)
                {
                    RunSingleBenchmark(i, j, secondsPerTest, warmSeconds);

                    Reporter.DisplayOneTestItemDone();
                }
            }

            Reporter.DisplayComplete();

            Reporter.DisplayMatrixResults(_coreResults, ProcessorCount);

            Reporter.DisplayTestGroupComplete();
        }

        public TimeSpan CalculateTime(double testSeconds, double warmSeconds)
        {
            var repeatCountPerSuite = (1 + ProcessorCount) * ProcessorCount / 2;

            return TimeSpan.FromSeconds((testSeconds * RepeatCount + warmSeconds) * repeatCountPerSuite);
        }

        private static void RunSingleBenchmark(int firstCore, int secondCore, double testSeconds, double warmSeconds)
        {
            var allRunners = new List<CoreRunner>();
            allRunners.Add(new CoreRunner(firstCore, Run));
            if (firstCore != secondCore)
            {
                allRunners.Add(new CoreRunner(secondCore, Run));
            }

            _coreCount = allRunners.Count;
            Volatile.Write(ref _phaseCompleted, 0);
            Volatile.Write(ref _readyToPhase, 0);

            GC.Collect(2);

            foreach (var runner in allRunners)
            {
                runner.Start();
            }

            var warmFinishSource = new CancellationTokenSource();
            warmFinishSource.Token.Register(() => CompletePhase(testSeconds));
            warmFinishSource.CancelAfter(TimeSpan.FromSeconds(warmSeconds));

            _cancelSource = warmFinishSource;

            foreach (var runner in allRunners)
            {
                runner.Join();
            }

            _coreResults[firstCore, secondCore] = (_singleRunResult[firstCore] + _singleRunResult[secondCore] ) / 2;
        }

        private static void CompletePhase(double testSeconds)
        {
            Volatile.Write(ref _readyToPhase, 0);
            if (Interlocked.Increment(ref _phaseCompleted) >= RepeatCount + 1)
            {
                return;
            }

            var cancelSource = new CancellationTokenSource();
            cancelSource.Token.Register(() => CompletePhase(testSeconds));
            cancelSource.CancelAfter(TimeSpan.FromSeconds(testSeconds));

            _cancelSource = cancelSource;
        }

        private static void Run(int coreIndex)
        {
            var coreCount = _coreCount;

            var results = new double[RepeatCount];
            var worker = new MathWorker();

            var stopwatch = Stopwatch.StartNew();

            // Warm
            Work(worker, stopwatch, coreCount);

            for (int i = 0; i < RepeatCount; i++)
            {
                var operations = Work(worker, stopwatch, coreCount);

                results[i] = (double)operations / stopwatch.ElapsedMilliseconds;
            }

            _singleRunResult[coreIndex] = results.Max();
        }

        private static int Work(IWorker worker, Stopwatch stopwatch, int readyCount)
        {
            var count = 0;

            var startPhaseNumber = Volatile.Read(ref _phaseCompleted);

            var ready = Interlocked.Increment(ref _readyToPhase) == readyCount;
            stopwatch.Restart();

            while (Volatile.Read(ref _phaseCompleted) == startPhaseNumber)
            {
                worker.DoWork();

                if (ready)
                {
                    count++;
                }
                else
                {
                    if (Volatile.Read(ref _readyToPhase) == readyCount)
                    {
                        stopwatch.Restart();
                        ready = true;
                    }
                }
            }

            stopwatch.Stop();

            return count;
        }
    }
}