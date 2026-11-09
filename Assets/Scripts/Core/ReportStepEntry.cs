using System;

namespace VRPT.Core
{
    /**
     * CLASS: ReportStepEntry
     * DESCRIPTION: One completed step in the report.
     * Public fields, because the JSON serializer ignores properties.
     *
     */
    [Serializable]
    public sealed class ReportStepEntry
    {
        /** Step that was completed */
        public string StepId;

        /** Seconds from the start of the procedure */
        public double CompletedAtSeconds;
    }
}
