using System;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class GCInfo : IDisposable
    {
        private readonly int _beforeGc0;
        private readonly int _beforeGc1;
        private readonly int _beforeGc2;

        public GCInfo()
        {
            _beforeGc0 = GC.CollectionCount(0);
            _beforeGc1 = GC.CollectionCount(1);
            _beforeGc2 = GC.CollectionCount(2);
        }

        public void Dispose()
        {
            var afterGc0 = GC.CollectionCount(0);
            var afterGc1 = GC.CollectionCount(1);
            var afterGc2 = GC.CollectionCount(2);

            Console.WriteLine("GCs during test: {0}/{1}/{2}", afterGc0 - _beforeGc0, afterGc1 - _beforeGc1, afterGc2 - _beforeGc2);
        }
    }
}