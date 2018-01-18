using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    public static class NumaNodeDetectorTests
    {
        private const int DwordsCount = 1024 * 1024 * 1024 * 1 / 4; //1Gb
        private const int RepeatCount = 5;
        private const int WarmingSeconds = 3;
        private static readonly Random Rnd = new Random();
        private static readonly int ProcessorCount = Environment.ProcessorCount;
        
        private static int[] _memory;
        private static readonly double[] _results = new double[ProcessorCount];
        private static int _completed;
        private static CancellationTokenSource _cancelSource;

        public static void Perform(double secondsPerTest)
        {
            Reporter.DisplayStartTests("NUMA");

            Reporter.DisplayInitializing();
            _memory = InitializeMemory();

            Reporter.DisplayPerforming();
            Reporter.DisplayProgressBar(ProcessorCount);

            for (int i = 0; i < ProcessorCount; i++)
            {
                Run(i, secondsPerTest);
                Reporter.DisplayOneTestItemDone();
            }

            Reporter.DisplayComplete();

            Reporter.DisplayNumaTestResults(_results);
        }

        public static TimeSpan CalculateTime(double testSeconds)
        {
            return TimeSpan.FromSeconds((testSeconds * RepeatCount + WarmingSeconds) * ProcessorCount);
        }

        private static int[] InitializeMemory()
        {
            var memory = new int[DwordsCount];
            
            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = Rnd.Next(500);
            }

            return memory;
        }

        private static void Run(int coreIndex, double testSeconds)
        {
            GC.Collect(2);

            _completed = 0;
            var runner = new CoreRunner(coreIndex, PerformTest);

            runner.Start();

            var warmFinishSource = new CancellationTokenSource();
            warmFinishSource.Token.Register(() => CompletePhase(testSeconds));
            warmFinishSource.CancelAfter(TimeSpan.FromSeconds(WarmingSeconds));

            _cancelSource = warmFinishSource;

            runner.Join();
        }

        private static void CompletePhase(double testSeconds)
        {
            if (Interlocked.Increment(ref _completed) >= RepeatCount + 1)
            {
                return;
            }

            var cancelSource = new CancellationTokenSource();
            cancelSource.Token.Register(() => CompletePhase(testSeconds));
            cancelSource.CancelAfter(TimeSpan.FromSeconds(testSeconds));

            _cancelSource = cancelSource;
        }

        private static void PerformTest(int coreIndex)
        {
            var next = Rnd.Next(0, _memory.Length);

            var localResult = new double[RepeatCount];

            // Warming
            DoWork(next);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < RepeatCount; i++)
            {
                next = Rnd.Next(0, _memory.Length);

                stopwatch.Restart();

                var count = DoWork(next);

                stopwatch.Stop();

                localResult[i] = (double) count / stopwatch.ElapsedMilliseconds;
            }

            _results[coreIndex] = localResult.Max();
        }

        private static int DoWork(int startIndex)
        {
            var completedOnStart = Volatile.Read(ref _completed);

            var count = 0;
            var sum = 0;
            var next = startIndex;
            var memoryLength = _memory.Length;

            while (Volatile.Read(ref _completed) == completedOnStart)
            {
                next = MemoryStepper.Next(next, true, memoryLength);

                unchecked
                {
                    sum += _memory[next];
                }

                count++;
            }

            return count;
        }
    }
}