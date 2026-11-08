using System;
using System.Collections.Generic;

namespace VRPT.Core
{
    /**
     * CLASS: StepDefinition
     * DESCRIPTION: One step of a procedure as plain data, with no Unity types.
     * Immutable, so nothing can alter a procedure while it is being evaluated.
     *
     */
    public sealed class StepDefinition
    {
        /** Unique identifier of the step, used in the report */
        public string Id { get; }

        /** Text shown to the user */
        public string Instruction { get; }

        /** Control the user must operate */
        public string ControlId { get; }

        /** Action the user must perform on that control */
        public ControlAction Action { get; }

        /** Controls that are dangerous to operate during this step */
        public IReadOnlyList<string> CriticalControlIds { get; }

        /** Creates a step; the critical controls are optional */
        public StepDefinition(string id, string instruction, string controlId, ControlAction action,
            IEnumerable<string> criticalControlIds = null)
        {
            Id = id;
            Instruction = instruction;
            ControlId = controlId;
            Action = action;
            CriticalControlIds = new List<string>(criticalControlIds ?? Array.Empty<string>());
        }
    }
}
