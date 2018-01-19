using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Reporter.DisplayHello();

            try
            {
                var runConfig = RunConfiguration.ParseFromCommandLine(args);

                if (runConfig.DisplayUsage)
                {
                    Reporter.DisplayUsage();
                }

                if (runConfig.Exit)
                {
                    return;
                }

                var detectors = runConfig.InstantiateDetectors();

                var timeToRunTests = TimeSpan.Zero;

                foreach (var detector in detectors)
                {
                    timeToRunTests += detector.CalculateTime(runConfig.SecondsPerMeasure, runConfig.WarmingSeconds);
                }

                if (timeToRunTests > TimeSpan.Zero)
                {
                    Reporter.DisplayBeforeTestInfo(runConfig.SecondsPerMeasure, timeToRunTests);
                }

                foreach (var detector in detectors)
                {
                    detector.Perform(runConfig.SecondsPerMeasure, runConfig.WarmingSeconds);
                }

                Reporter.DisplayBye();
            }
            catch (Exception ex)
            {
                Reporter.DisplayError(ex);
            }

            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
        }
    }
}