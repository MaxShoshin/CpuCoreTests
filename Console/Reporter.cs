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
        private const UInt64 Int64Lastbit = 9223372036854775808;
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
            Console.WriteLine($"  {exeName} [secondsPerTests]");
            Console.WriteLine();
        }

        public static void DisplayHello()
        {
            Console.WriteLine("Utility to check CPU cores.");
            Console.WriteLine();
        }

        public static void DisplayCpuInfo()
        {
            Console.WriteLine("General processor info:");

            Console.WriteLine("  Processor count (WMI): {0}", SystemInfoHelper.WmiPhysicalProcessorCount);
            Console.WriteLine("  Core count/processor (WMI): {0}",SystemInfoHelper.WmiCoreCount );
            Console.WriteLine("  Global logical processors (WMI): {0}", SystemInfoHelper.WmiGlobalLogicalProcessorCount);
            Console.WriteLine("  Global logical processors (Environment.LogicalProcessorCount): {0}", Environment.ProcessorCount);

            var activeProcessorGroupCount = SystemInfoHelper.GetActiveProcessorGroupCount();


            var sb = new StringBuilder();
            for (UInt16 groupIndex = 0; groupIndex < activeProcessorGroupCount; groupIndex++)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.Append("      ");
                }

                sb.AppendFormat("Group '{0}' as ", groupIndex);

                UInt32 processorCount = SystemInfoHelper.GetActiveProcessorCount(groupIndex);
                if (processorCount == 0)
                {
                    sb.AppendFormat("Error reading GetActiveProcessorCount: {0}", Marshal.GetLastWin32Error());
                }
                else
                {
                    sb.AppendFormat("{0} Logical Processors", processorCount);
                }
            }

            Console.WriteLine("  Active Processor group count (pinvoke kernel32): {0}", activeProcessorGroupCount);
            Console.WriteLine("  Logical Processor count per group (pinvoke kernel32): {0}", sb);

            Console.WriteLine("  Maximum Processor group count (pinvoke kernel32): {0}", SystemInfoHelper.GetMaximumProcessorGroupCount());


            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            Console.WriteLine("  Thread pool Max Threads - workerThreads: {0}", workerThreads);
            Console.WriteLine("  Thread pool Max Threads - completionPortThreads: {0}", completionPortThreads);

            UInt64 processAffinityMask;
            UInt64 systemAffinityMask;

            bool isResultOk = SystemInfoHelper.GetProcessAffinityMask(
                System.Diagnostics.Process.GetCurrentProcess().Handle,
                out processAffinityMask,
                out systemAffinityMask);

            if (isResultOk)
            {
                Console.WriteLine("  Process Affinity Mask (pinvoke kernel32): {0:x8} (bit count: {1}, mask: {2})", processAffinityMask, GetBitCount(processAffinityMask), GetBitString(processAffinityMask));
                Console.WriteLine("  System Affinity Mask (pinvoke kernel32): {0:x8} (bit count: {1}, mask: {2})", systemAffinityMask, GetBitCount(systemAffinityMask), GetBitString(systemAffinityMask));
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                Console.WriteLine("  GetProcessAffinityMask() error = {0}", error);
            }


            var numaHighestNodeNumber = SystemInfoHelper.NumaHighestNodeNumber;

            Console.WriteLine("  Highest Numa Node Number (pinvoke kernel32)    0 Based: {0}", numaHighestNodeNumber);

            Console.WriteLine("  NUMA nodes and their associated Processor Mask (pinvoke kernel32):");
            for (int nodeIndex = 0; nodeIndex <= numaHighestNodeNumber; nodeIndex++)
            {
                UInt64 numaNodeProcessorMask;
                isResultOk = SystemInfoHelper.GetNumaNodeProcessorMask((byte) nodeIndex, out numaNodeProcessorMask);
                if (isResultOk)
                {
                    Console.WriteLine("  Node: {0} Processor Mask: {1:x8} (bit count: {2}, mask: {3})", nodeIndex, numaNodeProcessorMask, GetBitCount(numaNodeProcessorMask), GetBitString(numaNodeProcessorMask));
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    Console.WriteLine("  GetNumaNodeProcessorMask({1}) error = {0}", error, nodeIndex);
                }
            }

            Console.WriteLine();
            var structLogProcInfo = SystemInfoHelper.GetLogicalProcessorInformation();
            foreach (var procInfo in structLogProcInfo)
            {
                var processorMask = GetBitString((ulong)procInfo.ProcessorMask);
                Console.Write($"  Processor mask: {processorMask} ");

                switch (procInfo.Relationship)
                {
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore:
                        Console.WriteLine("Core.");
                        break;
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationNumaNode:
                        Console.WriteLine($"NumaNode {procInfo.ProcessorInformation.NumaNode.NodeNumber}");
                        break;
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache:
                        var cache = procInfo.ProcessorInformation.Cache;
                        Console.WriteLine($"Cache level {cache.Level}, size {cache.Size / 1024}Kb, line size: {cache.LineSize} type: {cache.Type}");
                        break;
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorPackage:
                        Console.WriteLine($"Processor package.");
                        break;
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup:
                        Console.WriteLine($"Relation group.");
                        break;
                    case SystemInfoHelper.LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll:
                        Console.WriteLine($"Relation all.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static int GetBitCount(UInt64 number)
        {
            int count = 0;
            while (number != 0)
            {
                if ((number & 1) == 1)
                {
                    count++;
                }
                number = number >> 1;
            }
            return count;
        }

        private static string GetBitString(UInt64 number)
        {
            var sb = new StringBuilder();
            UInt64 bit = Int64Lastbit;

            for(int index = 0; index < 64; index++)
            {
                if ((number & bit) > 0)
                {
                    sb.Append('1');
                }
                else
                {
                    sb.Append('-');
                }
                bit = bit >> 1;
            }

            return sb.ToString();
        }

        public static void DisplayBeforeTestInfo(double testSeconds, TimeSpan timeToRunTests)
        {
            Console.WriteLine();
            Console.WriteLine("Run tests:");
            Console.WriteLine($"Use {testSeconds}sec per test.");

            Console.WriteLine("It will take around: {0}", timeToRunTests );
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("MathWorker tests tries to do some mathematical computation. These tests will detect hyperthreading cores.");
            Console.WriteLine("LocalMemoryWorker tests tries to operate with local to core memory. These tests can be used as baseline for NUMA detection tests.");
            Console.WriteLine("FarMemoryWorker tests operates with the shared between all cores memory. This test can detect NUMA groups of processors.");
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

        public static void DisplayPairTest(string workerName)
        {
            Console.WriteLine($"Testing core pairs {workerName}...");
        }

        public static void DisplayOneMatrixTestDone()
        {
            Console.Write(".");
        }

        public static void DisplayMatrixTestsDone()
        {
            Console.WriteLine();
        }

        public static void DisplayMatrixResults(double[,] matrix, int processorCount)
        {
            Console.Write("    | ");
            for (var i = 0; i < processorCount; i++)
            {
                Console.Write($"{i,12:#########0}  | ");
            }

            Console.WriteLine();

            var mins = CalculateRowMins(matrix, processorCount);

            for (var i = 0; i < processorCount; i++)
            {
                Console.Write($"{i,2}  | ");

                for (var j = 0; j < processorCount; j++)
                {
                    if (i > j)
                    {
                        Console.Write("            ");
                    }
                    else
                    {
                        if (mins[i] == matrix[i,j])
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                        }

                        if (i == j)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                        }

                        Console.Write($"{matrix[i, j],12:0.000}");
                        Console.ForegroundColor = DefaultColor;
                    }

                    Console.Write("  | ");
                }

                Console.WriteLine();
            }
        }

        private static double[] CalculateRowMins(double[,] matrix, int processorCount)
        {
            var mins = new double[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                var min = double.MaxValue;

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



        public static void DisplayTestInfo(string description, string workerName)
        {
            Console.WriteLine();
            Console.WriteLine($"Start {workerName} tests... ({description})");
        }

        public static void DisplayResults(IReadOnlyList<WorkerArgs> workerArgs)
        {
            var badReports = workerArgs.Select(x => x.Report)
                .OrderBy(report => report.OpsPerMs)
                .Take(workerArgs.Count / 2)
                .ToList();

            foreach (var workerArg in workerArgs)
            {
                var report = workerArg.Report;

                Console.ForegroundColor = badReports.Contains(report)
                    ? ConsoleColor.DarkRed
                    : ConsoleColor.Green;

                Console.WriteLine($"{report.CoreIndex:D2}: {report.Elapsed:G} Operations: {report.OperationsCount}; Switches: {report.CoreSwitchCount}. Processor time: {report.ProcessorTime}. Ops/ms: {report.OpsPerMs}");
            }

            Console.ForegroundColor = DefaultColor;
            Console.WriteLine("Done.");
        }

        public static void DisplayTestGroupComplete()
        {
            Console.WriteLine();
            Console.WriteLine();
        }

        public static void DisplayTestRetry(int i, int repeatCount)
        {
            Console.WriteLine("===================================================================");
            Console.WriteLine($"Repeat {i+1} of {repeatCount}");
            Console.WriteLine();
        }
    }
}