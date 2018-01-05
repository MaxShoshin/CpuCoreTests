using System;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class WorkerArgs
    {
        public WorkerArgs(IWorker worker, Thread thread, int index, CancellationToken cancellationToken)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            if (thread == null) throw new ArgumentNullException(nameof(thread));

            Worker = worker;
            Thread = thread;
            Index = index;
            CancellationToken = cancellationToken;
            Report = new ReportInfo(index);
        }

        public IWorker Worker { get; }
        public Thread Thread { get; }
        public int Index { get; }
        public CancellationToken CancellationToken { get; }
        public ReportInfo Report { get; }
    }
}