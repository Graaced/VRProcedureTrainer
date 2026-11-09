using System;
using System.Collections.Generic;

namespace VRPT.Core
{
    /**
     * CLASS: ProcedureReport
     * DESCRIPTION: Result of a training session, shown to the user and saved as JSON.
     * Dates and enums are stored as text, so the saved file stays readable by a person.
     *
     */
    [Serializable]
    public sealed class ProcedureReport
    {
        /** Identifier of the procedure */
        public string ProcedureId;

        /** Title of the procedure */
        public string ProcedureTitle;

        /** When the session started, as an ISO 8601 UTC date */
        public string StartedAtUtc;

        /** Length of the session, in seconds */
        public double DurationSeconds;

        /** Final verdict, as a ReportOutcome name */
        public string Outcome;

        /** Errors of any type, critical ones included */
        public int ErrorCount;

        /** Errors of the critical type only */
        public int CriticalErrorCount;

        /** Maximum number of errors the procedure allows to pass */
        public int MaxAllowedErrors;

        /** Completed steps, in order */
        public List<ReportStepEntry> Steps = new List<ReportStepEntry>();

        /** User mistakes, in order */
        public List<ReportErrorEntry> Errors = new List<ReportErrorEntry>();

        /** Uses of the emergency stop, in order */
        public List<ReportEventEntry> Events = new List<ReportEventEntry>();

        /** True if the session was passed; being a property, it is not saved */
        public bool IsPassed => Outcome == nameof(ReportOutcome.Passed);
    }
}
