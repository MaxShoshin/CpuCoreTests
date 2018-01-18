using System;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class LocalMemoryWorker : IWorker
    {
        private const int DwordsCount = 200 * 1024 * 1024 * 1 / 4; //200Mb

        private static readonly Random Rnd = new Random();
        private static int Index;

        private readonly int[] _memory;
        private readonly bool _forward = Index++ % 2 == 0;
        private int _next;
        private long _sum;

        public LocalMemoryWorker()
        {
            var memory = new int[DwordsCount];

            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = Rnd.Next(500);
            }

            _memory = memory;
            _next = Rnd.Next(0, memory.Length);
        }

        public void DoWork()
        {
            _sum += _memory[_next];

            _next = MemoryStepper.Next(_next, _forward, _memory.Length);
        }

        public void Warm()
        {
        }
    }
}