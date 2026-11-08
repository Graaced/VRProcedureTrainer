namespace VRPT.Core
{
    /**
     * CLASS: StepCompletedInfo
     * DESCRIPTION: Details of a step the user has just completed.
     * The UI uses it to move on, the report uses it to record the timing.
     *
     */
    public sealed class StepCompletedInfo
    {
        /** Step that was completed */
        public string StepId { get; }

        /** Position of the step in the procedure */
        public int StepIndex { get; }

        /** Seconds from the start of the procedure */
        public double AtSeconds { get; }

        /** Creates the completion details */
        public StepCompletedInfo(string stepId, int stepIndex, double atSeconds)
        {
            StepId = stepId;
            StepIndex = stepIndex;
            AtSeconds = atSeconds;
        }
    }
}
