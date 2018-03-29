using System.Threading;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class SpinBarrier
    {
        private readonly int _phaseCount;
        private readonly int _participantCount;
        private int _readyParticipants;
        private int _phase;

        public SpinBarrier(int phaseCount, int participants)
        {
            _phaseCount = phaseCount;
            _participantCount = participants;
        }

        public bool ParticipantReady()
        {
            return Interlocked.Increment(ref _readyParticipants) == _participantCount;
        }

        public bool AreAllParticipantsReady()
        {
            return _readyParticipants == _participantCount;
        }

        public bool NextPhase()
        {
            Volatile.Write(ref _readyParticipants, 0);
            return Interlocked.Increment(ref _phase) < _phaseCount;
        }

        public bool IsPhaseComplete(int phase)
        {
            return _phase != phase;
        }

        public int GetPhase()
        {
            return _phase;
        }
    }
}