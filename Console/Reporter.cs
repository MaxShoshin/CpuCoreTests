using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal static class Reporter
    {
        private const double Tolerance = 0.0000001;

        private static readonly ConsoleColor DefaultColor;

        static Reporter()
        {
            DefaultColor = Console.ForegroundColor;
        }

        public static void DisplayUsage()
        {
            var exeName = Path.GetFileName(typeof(Program).Assembly.Location);

            Console.WriteLine("Usage:");
            Console.WriteLine($"  {exeName} [secondsPerTests] [detectorType1 [detectorType2..]]");
            Console.WriteLine("      where detectorType1, detectorType2 can be one of:");
            Console.WriteLine("         CpuInfo        - display CPU information");
            Console.WriteLine("         Hyperthreading - run tests to detect hyperthreading cores");
            Console.WriteLine("         NUMA           - run tests to detect cores on different NUMA sockets");
            Console.WriteLine();
        }

        public static void DisplayHello()
        {
            Console.WriteLine("Utility to check CPU cores.");
            Console.WriteLine();
        }


        public static void DisplayBeforeTestInfo(double testSeconds, TimeSpan timeToRunTests)
        {
            Console.WriteLine();
            Console.WriteLine("Run tests:");
            Console.WriteLine($"Use {testSeconds}sec per test.");

            Console.WriteLine("It will take around: {0}", timeToRunTests );
            Console.WriteLine();
            Console.WriteLine();
        }

        public static void DisplayBye()
        {
            Console.WriteLine();
            Console.WriteLine("Finished.");
        }

        public static void DisplayError(Exception exception)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception);
            Console.ForegroundColor = DefaultColor;
        }

        public static void DisplayStartTests(string testName)
        {
            Console.WriteLine($"Performing {testName} tests..." );

        }

        public static void DisplayOneTestItemDone()
        {
            Console.Write(".");
        }

        public static void DisplayProgressBar(int count)
        {
            Console.Write(new string('-', count));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        public static void DisplayComplete()
        {
            Console.WriteLine();
            Console.WriteLine();
        }

        public static void DisplayMatrixResults(double[,] matrix, int processorCount)
        {
            if (Console.WindowWidth < 11 + processorCount * 6)
            {
                Console.WriteLine("... matrix will be displayed incorrectly. please increase console width.");
            }

            Console.WriteLine("Performing math operations simultaniously on two nearest hyperthreading cores leads to perf degradation.");
            Console.WriteLine("Performacnce (%) when every 2 CPU cores perform mathematical :.");

            var percentMatrix = ConvertToPercent(matrix, processorCount);

            DisplayTableHeader(processorCount);

            var mins = CalculateRowMins(percentMatrix, processorCount);

            for (var i = 0; i < processorCount; i++)
            {
                Console.Write($"{i,2} | ");

                for (var j = 0; j < processorCount; j++)
                {
                    if (i > j)
                    {
                        Console.Write("   ");
                    }
                    else
                    {
                        if (mins[i] == percentMatrix[i,j])
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                        }

                        if (i == j)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                        }

                        Console.Write($"{percentMatrix[i, j],3:0}");
                        Console.ForegroundColor = DefaultColor;
                    }

                    Console.Write(" | ");
                }

                Console.WriteLine();
            }
        }

        private static void DisplayTableHeader(int processorCount)
        {
            Console.Write("   | ");
            for (var i = 0; i < processorCount; i++)
            {
                Console.Write($"{i,3:##0} | ");
            }

            Console.WriteLine();

            Console.Write("---|");
            for (var i = 0; i < processorCount; i++)
            {
                Console.Write($"-----|");
            }

            Console.WriteLine();
        }

        public static void DisplayTestGroupComplete()
        {
            Console.WriteLine();
            Console.WriteLine();
        }

        private static int[,] ConvertToPercent(double[,] matrix, int processorCount)
        {
            var max = double.MinValue;
            for (var i = 0; i < processorCount; i++)
            {
                for (var j = i; j < processorCount; j++)
                {
                    if (max < matrix[i, j])
                    {
                        max = matrix[i, j];
                    }
                }
            }

            var percents = new int[processorCount, processorCount];
            for (var i = 0; i < processorCount; i++)
            {
                for (var j = i; j < processorCount; j++)
                {
                    percents[i, j] = (int)Math.Round(matrix[i, j] / max * 100);
                }
            }

            return percents;
        }

        private static int[] CalculateRowMins(int[,] matrix, int processorCount)
        {
            var mins = new int[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                var min = int.MaxValue;

                for (int j = 0; j < processorCount; j++)
                {
                    if (i > j)
                    {
                        if (matrix[j, i] < min)
                        {
                            min = matrix[j, i];
                        }
                    }
                    else if (i < j)
                    {
                        if (matrix[i, j] < min)
                        {
                            min = matrix[i, j];
                        }
                    }
                }

                mins[i] = min;
            }

            return mins;
        }

        public static void DisplayInitializing()
        {
            Console.WriteLine("Initialization...");
        }

        public static void DisplayPerforming()
        {
            Console.WriteLine("Performing tests...");
        }

        public static void DisplayNumaTestResults(double[,] allResults)
        {
            var results = MaxResultsPerCore(allResults);

            Console.WriteLine();
            Console.WriteLine("Accessing to the 'local' memory for NUMA socket is faster than accessing to the ");
            Console.WriteLine("other's NUMA socket memory.");
            Console.WriteLine("Performance per CPU core (%) for memory access (allocated once before start):");

            DisplayTableHeader(results.Length);
            Console.Write("   | ");

            var max = results.Max();

            for (int i = 0; i < results.Length; i++)
            {
                var percent = (int)Math.Round(results[i] * 100 / max);
                Console.Write($"{percent,3:0} | ");
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        private static double[] MaxResultsPerCore(double[,] allResults)
        {
            var repeatCount = allResults.GetLength(1);
            var length = allResults.GetLength(0);

            var results = new double[length];

            for (int i = 0; i < length; i++)
            {
                var max = allResults[i, 0];

                for (int j = 1; j < repeatCount; j++)
                {
                    if (allResults[i, j] > max)
                    {
                        max = allResults[i, j];
                    }
                }

                results[i] = max;
            }

            return results;
        }
    }
}