namespace VRPT.Core
{
    /**
     * CLASS: ReportOutcome
     * DESCRIPTION: Final verdict on a training session.
     * It differs from RunnerState: a procedure can reach the end and still be failed for too many errors.
     *
     */
    public enum ReportOutcome
    {
        /** No critical errors and no more errors than allowed */
        Passed,

        /** A critical error, or more errors than allowed */
        Failed
    }
}
