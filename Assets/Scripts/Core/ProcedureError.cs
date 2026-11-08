namespace VRPT.Core
{
    /**
     * CLASS: ProcedureError
     * DESCRIPTION: A mistake made by the user during a procedure.
     * User mistakes are data, not exceptions, because they are a normal part of training.
     *
     */
    public sealed class ProcedureError
    {
        /** Step the user was supposed to perform */
        public string StepId { get; }

        /** Control the user actually operated */
        public string ControlId { get; }

        /** Severity of the mistake */
        public ProcedureErrorType Type { get; }

        /** Seconds from the start of the procedure */
        public double AtSeconds { get; }

        /** Creates the error details */
        public ProcedureError(string stepId, string controlId, ProcedureErrorType type, double atSeconds)
        {
            StepId = stepId;
            ControlId = controlId;
            Type = type;
            AtSeconds = atSeconds;
        }
    }
}
