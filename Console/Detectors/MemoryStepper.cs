using System;
using System.Runtime.CompilerServices;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class MemoryStepper
    {
        private const int Step = 17 * 1024 * 1024 / 4; // 17Mb

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int next, bool forward, int memoryLength)
        {
            var addToNext = Step;
            if (!forward)
            {
                addToNext = -addToNext;
            }

            next += addToNext;

            while (next >= memoryLength)
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