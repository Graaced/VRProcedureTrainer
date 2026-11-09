using System;

namespace VRPT.Core
{
    /**
     * CLASS: ReportErrorEntry
     * DESCRIPTION: One user mistake in the report.
     * The type is stored as text, because the JSON serializer would write an enum as a number.
     *
     */
    [Serializable]
    public sealed class ReportErrorEntry
    {
        /** Step the user was supposed to perform */
        public string StepId;

        /** Control the user actually operated */
        public string ControlId;

        /** Severity of the mistake, as a ProcedureErrorType name */
        public string Type;

        /** Seconds from the start of the procedure */
        public double AtSeconds;
    }
}
