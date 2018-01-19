using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    public sealed class CpuInfoDetector : IDetector
    {
        private const UInt64 Int64Lastbit = 9223372036854775808;

        public TimeSpan CalculateTime(double secondsPerMeasure, double warmSeconds)
        {
            return TimeSpan.Zero;
        }

        public void Perform(double secondsPerMeasure, double warmSeconds)
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
    }
}