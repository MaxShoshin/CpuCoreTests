using System;

namespace CpuThreadingTest.ConsoleApp
{
    internal interface IDetector
    {
        TimeSpan CalculateTime(double secondsPerMeasure, double warmingSeconds);

        void Perform(double secondsPerMeasure, double warmingSeconds);
    }
}