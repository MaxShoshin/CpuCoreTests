using System;
using System.Runtime.CompilerServices;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class MemoryStepper
    {
        private const int Step = 16777216;
        private static readonly Random Rnd = new Random();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int next, bool forward, int memoryLength)
        {
            var addToNext = Rnd.Next(Step / 2, Step);
            if (!forward)
            {
                addToNext = -addToNext;
            }

            next += addToNext;

            if (next >= memoryLength)
            {
                next -= memoryLength;
            }

            if (next < 0)
            {
                next += memoryLength;
            }

            return next;
        }
    }
}