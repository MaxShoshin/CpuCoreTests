using System;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class ReportInfo
    {
        public ReportInfo(int coreIndex)
        {
            CoreIndex = coreIndex;
        }

        public int CoreIndex { get; }
        public int CoreSwitchCount { get; set; }
        public int OperationsCount { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan ProcessorTime { get; set; }
        public double OpsPerMs => OperationsCount / ProcessorTime.TotalMilliseconds;

        public int GC0Count { get; set; }
        public int GC1Count { get; set; }
        public int GC2Count { get; set; }
    }
}