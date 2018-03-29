using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CpuThreadingTest.ConsoleApp.Multithreading;

namespace CpuThreadingTest.ConsoleApp.Detectors
{
    internal sealed class NumaNodeDetector : IDetector
    {
        private const int DwordsCount = 1024 * 1024 * 1024 * 1 / 4; //1Gb
        private const int RepeatCount = 5;
        private static readonly Random Rnd = new Random();
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        private static int[] _memory;
        private static int[] _indexes;

        public void Perform(double secondsPerTest, double warmSeconds)
        {
            Reporter.DisplayStartTests("NUMA");

            Reporter.DisplayInitializing();
            _memory = InitializeMemory();

            Reporter.DisplayPerforming();
            Reporter.DisplayProgressBar(ProcessorCount);

            var multicoreRunner = new MultiCoreRunner(RepeatCount, secondsPerTest, warmSeconds, ReadMemory);

            _indexes = new int[ProcessorCount];

            for (int i = 0; i < ProcessorCount; i++)
            {
                _indexes[i] = Rnd.Next(0, _memory.Length);
                multicoreRunner.ScheduleCore(i);
            }

            multicoreRunner.Run();

            Reporter.DisplayComplete();

            Reporter.DisplayNumaTestResults(multicoreRunner.GetResults());
        }

        private int ReadMemory(int coreIndex)
        {
            var next = _indexes[coreIndex];
            next = MemoryStepper.Next(next, true, _memory.Length);
            _indexes[coreIndex] = next;

            return _memory[next];
        }

        public TimeSpan CalculateTime(double testSeconds, double warmSeconds)
        {
            return TimeSpan.FromSeconds(testSeconds * RepeatCount + warmSeconds);
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
    }
}