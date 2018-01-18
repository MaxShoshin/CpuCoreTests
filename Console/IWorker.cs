namespace CpuThreadingTest.ConsoleApp
{
    internal interface IWorker
    {
        void DoWork();

        void Warm();
    }
}