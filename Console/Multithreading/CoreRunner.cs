using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class CoreRunner
    {
        private static readonly ThreadPriority ThreadPriority = ThreadPriority.Normal;


        private readonly int _coreIndex;
        private readonly Action<int> _doWork;

        private Thread _thread;

        public CoreRunner(int coreIndex, Action<int> doWork)
        {
            if (doWork == null) throw new ArgumentNullException(nameof(doWork));

            _coreIndex = coreIndex;
            _doWork = doWork;

            _thread = CreateThread();
        }

        public int CoreIndex => _coreIndex;

        public void Start()
        {
            _thread.Start();
        }

        public void Join()
        {
            _thread.Join();
        }

        private void Runner(object arg)
        {
            var currentThreadId = AppDomain.GetCurrentThreadId();
            var currentThread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>()
                .First(thread => thread.Id == currentThreadId);

            currentThread.ProcessorAffinity = (IntPtr) (1 << _coreIndex);

            _doWork(_coreIndex);
        }

        private Thread CreateThread()
        {
            var thread = new Thread(Runner);
            thread.Priority = ThreadPriority;

            return thread;
        }
    }
}