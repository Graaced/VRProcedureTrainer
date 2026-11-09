using System;

namespace VRPT.Core
{
    /**
     * CLASS: ReportEventEntry
     * DESCRIPTION: One use of the emergency stop in the report.
     * It is kept apart from the errors, because using the emergency stop is never penalized.
     *
     */
    [Serializable]
    public sealed class ReportEventEntry
    {
        /** Control that was used */
        public string ControlId;

        /** What was done on it, as a ControlAction name */
        public string Action;

        /** Seconds from the start of the procedure */
        public double AtSeconds;
    }
}
