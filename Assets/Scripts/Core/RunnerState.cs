namespace VRPT.Core
{
    /**
     * CLASS: RunnerState
     * DESCRIPTION: Overall state of a procedure.
     * Completed and Failed are final: from there the runner ignores every action.
     *
     */
    public enum RunnerState
    {
        /** Created but not started yet */
        NotStarted,

        /** The user is performing the steps */
        Running,

        /** Every step was completed */
        Completed,

        /** Stopped by a critical error */
        Failed
    }
}
