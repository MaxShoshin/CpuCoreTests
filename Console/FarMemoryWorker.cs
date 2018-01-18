using System;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class FarMemoryWorker : IWorker
    {
        private const int DwordsCount = 1024 * 1024 * 1024 * 1 / 4; //1Gb

        private static readonly Random Rnd = new Random();
        private static readonly int[] Memory;
        private static int Index = 0;

        private readonly bool _forward = Index++ % 2 == 0;
        private int _next;
        private long _sum;

        static FarMemoryWorker()
        {
            var memory = new int[DwordsCount];

            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = Rnd.Next(500);
            }

            Memory = memory;
        }

        public FarMemoryWorker()
        {
            _next = Rnd.Next(0, Memory.Length);
        }

        public void DoWork()
        {
            _sum += Memory[_next];

            _next = MemoryStepper.Next(_next, _forward, Memory.Length);
        }

        public void Warm()
        {
        }
    }
}