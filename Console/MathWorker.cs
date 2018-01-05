using System;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class MathWorker : IWorker
    {
        private double _x;
        private int _i = 1;

        public void DoWork()
        {
            _i++;

            PrimeTool.IsPrime(_i);
            if (_i == int.MaxValue)
            {
                _i = 0;
                _x= Math.Sqrt(_i);
                _x += _x;
            }
        }

        public static class PrimeTool
        {
            public static bool IsPrime(int candidate)
            {
                // Test whether the parameter is a prime number.
                if ((candidate & 1) == 0)
                {
                    if (candidate == 2)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                // Note:
                // ... This version was changed to test the square.
                // ... Original version tested against the square root.
                // ... Also we exclude 1 at the end.
                for (int i = 3; (i * i) <= candidate; i += 2)
                {
                    if ((candidate % i) == 0)
                    {
                        return false;
                    }
                }
                return candidate != 1;
            }
        }
    }
}