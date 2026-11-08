namespace VRPT.Core
{
    /**
     * CLASS: IClock
     * DESCRIPTION: Source of time for the training logic.
     * Tests use a fake clock that advances on command, so timings are deterministic.
     *
     */
    public interface IClock
    {
        /** Seconds elapsed from a fixed origin; only the differences matter */
        double NowSeconds { get; }
    }
}
