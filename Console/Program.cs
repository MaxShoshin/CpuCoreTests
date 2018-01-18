using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class Program
    {
        private const double DefaultTestSeconds = 1;


        private static int _readyCount;

        public static void Main(string[] args)
        {
            Reporter.DisplayHello();

            try
            {
                var testSeconds = DefaultTestSeconds;
                if (args.Length != 1 || !double.TryParse(args[0], out testSeconds))
                {
                    Reporter.DisplayUsage();
                }

                Reporter.DisplayCpuInfo();

                var timeToRunTests =
                    HyperthreadingDetectorTests.CalculateTime(testSeconds);

                Reporter.DisplayBeforeTestInfo(testSeconds, timeToRunTests);

                HyperthreadingDetectorTests.Perform(testSeconds);

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