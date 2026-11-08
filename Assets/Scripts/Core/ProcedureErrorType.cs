namespace VRPT.Core
{
    /**
     * CLASS: ProcedureErrorType
     * DESCRIPTION: Severity of a user mistake.
     * A critical error can end the procedure, while a wrong order only gets recorded.
     *
     */
    public enum ProcedureErrorType
    {
        /** An action on a control that is not the expected one */
        WrongOrder,

        /** An action listed as dangerous for the current step */
        Critical
    }
}
