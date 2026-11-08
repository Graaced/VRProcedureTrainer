namespace VRPT.Core.Tests
{
    /**
     * CLASS: FakeClock
     * DESCRIPTION: Clock that moves only when a test tells it to.
     * It makes every timing assertion deterministic, without waiting for real time.
     *
     */
    public sealed class FakeClock : IClock
    {
        /** Current fake time, in seconds */
        public double NowSeconds { get; private set; } = 100.0;

        /** Moves the clock forward */
        public void Advance(double seconds)
        {
            NowSeconds += seconds;
        }
    }
}
