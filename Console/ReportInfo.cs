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
    }
}