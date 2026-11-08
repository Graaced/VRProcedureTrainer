using System.Collections.Generic;

namespace VRPT.Core
{
    /**
     * CLASS: ProcedureDefinition
     * DESCRIPTION: A whole procedure as plain data, read by the runner.
     * It keeps the logic independent from the ScriptableObjects that author it.
     *
     */
    public sealed class ProcedureDefinition
    {
        /** Unique identifier of the procedure, used in the report */
        public string Id { get; }

        /** Title shown to the user */
        public string Title { get; }

        /** Ordered steps of the procedure */
        public IReadOnlyList<StepDefinition> Steps { get; }

        /** Control treated as the emergency stop, which is never an error */
        public string EmergencyControlId { get; }

        /** Maximum number of errors for the procedure to count as passed */
        public int MaxAllowedErrors { get; }

        /** When true a critical error ends the procedure immediately */
        public bool FailOnCriticalError { get; }

        /** Creates a procedure definition */
        public ProcedureDefinition(string id, string title, IEnumerable<StepDefinition> steps,
            string emergencyControlId, int maxAllowedErrors, bool failOnCriticalError)
        {
            Id = id;
            Title = title;
            Steps = new List<StepDefinition>(steps);
            EmergencyControlId = emergencyControlId;
            MaxAllowedErrors = maxAllowedErrors;
            FailOnCriticalError = failOnCriticalError;
        }
    }
}
