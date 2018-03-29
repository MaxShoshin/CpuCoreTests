using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp.Multithreading
{
    public sealed class MultiCoreRunner
    {
        private readonly int _repeatCount;
        private readonly double _testSeconds;
        private readonly double _warmSeconds;
        private readonly List<CoreRunner> _runners = new List<CoreRunner>();
        private readonly Func<int, int> _payload;

        private CancellationTokenSource _cancelSource;
        private double[,] _results;
        private SpinBarrier _barrier;

        public MultiCoreRunner(int repeatCount, double testSeconds, double warmSeconds, Func<int, int> payload)
        {
            _repeatCount = repeatCount;
            _testSeconds = testSeconds;
            _warmSeconds = warmSeconds;
            _payload = payload;
        }

        public void ScheduleCore(int coreIndex)
        {
            var runner = new CoreRunner(coreIndex, RunOnSingleCore);
            _runners.Add(runner);
        }

        public void Run()
        {
            var maxIndex = _runners.Select(runner => runner.CoreIndex).Max();
            _results = new double[maxIndex + 1, _repeatCount];

            _barrier = new SpinBarrier(_repeatCount + 1, _runners.Count);

            foreach (var coreRunner in _runners)
            {
                coreRunner.Start();
            }

            var warmFinishSource = new CancellationTokenSource();
            warmFinishSource.Token.Register(() => CompletePhase(_testSeconds));
            warmFinishSource.CancelAfter(TimeSpan.FromSeconds(_warmSeconds));

            _cancelSource = warmFinishSource;

            foreach (var coreRunner in _runners)
            {
                coreRunner.Join();
            }
        }

        public double[,] GetResults()
        {
            return _results;
        }

        private void RunOnSingleCore(int coreIndex)
        {
            var stopwatch = Stopwatch.StartNew();

            // Warm
            DoWork(stopwatch, coreIndex);

            for (int i = 0; i < _repeatCount; i++)
            {
                var operationCount = DoWork(stopwatch, coreIndex);

                _results[coreIndex, i] = (double)operationCount / stopwatch.ElapsedMilliseconds;
            }
        }

        private int DoWork(Stopwatch stopwatch, int coreIndex)
        {
            var phase = _barrier.GetPhase();

            var count = 0;
            long sum = 0;

            var ready = _barrier.ParticipantReady();
            stopwatch.Restart();

            while (!_barrier.IsPhaseComplete(phase))
            {
                unchecked
                {
                    sum +=_payload(coreIndex);
                }

                if (ready)
                {
                    count++;
                }
                else
                {
                    if (_barrier.AreAllParticipantsReady())
                    {
                        stopwatch.Restart();
                        ready = true;
                    }
                }
            }

            stopwatch.Stop();

            return count;
        }

        private void CompletePhase(double testSeconds)
        {
            if (!_barrier.NextPhase())
            {
                return;
            }

            Trace.WriteLine(DateTime.UtcNow);

            var cancelSource = new CancellationTokenSource();
            cancelSource.Token.Register(() => CompletePhase(testSeconds));
            cancelSource.CancelAfter(TimeSpan.FromSeconds(testSeconds));

            _cancelSource = cancelSource;
        }
    }
}
