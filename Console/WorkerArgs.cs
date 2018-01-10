using System;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class WorkerArgs
    {
        public WorkerArgs(IWorker worker, Thread thread, int index, int workerCount)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            if (thread == null) throw new ArgumentNullException(nameof(thread));

            Worker = worker;
            Thread = thread;
            Index = index;
            WorkerCount = workerCount;
            Report = new ReportInfo(index);
        }

        public IWorker Worker { get; }
        public Thread Thread { get; }
        public int WorkerCount { get; }
        public int Index { get; }
        public ReportInfo Report { get; }
    }
}