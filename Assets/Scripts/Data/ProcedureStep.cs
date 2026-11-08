using System.Collections.Generic;
using UnityEngine;
using VRPT.Core;

namespace VRPT.Data
{
    /**
     * CLASS: ProcedureStep
     * DESCRIPTION: One step of a procedure, authored as an asset from the Inspector.
     * Being data, a new step can be added to a procedure without touching any code.
     *
     */
    [CreateAssetMenu(fileName = "Step", menuName = "VRPT/Procedure Step")]
    public sealed class ProcedureStep : ScriptableObject
    {
        /** Stable identifier used by the logic and the report, e.g. "press-stop" */
        [SerializeField] private string id;

        /** Text shown to the user on the instruction panel */
        [SerializeField, TextArea] private string instruction;

        /** Control the user must operate */
        [SerializeField] private ControlId expectedControl;

        /** Action the user must perform on that control */
        [SerializeField] private ControlAction expectedAction;

        /** Controls that are dangerous to operate during this step */
        [SerializeField] private List<ControlId> criticalControls = new List<ControlId>();

        /** Read-only identifier: asset data must not change at runtime */
        public string Id => id;

        /** Read-only expected control */
        public ControlId ExpectedControl => expectedControl;

        /** Converts the asset to plain data; call it only when FindProblems returns nothing */
        public StepDefinition ToDefinition()
        {
            List<string> criticalControlIds = new List<string>(criticalControls.Count);
            foreach (ControlId criticalControl in criticalControls)
                criticalControlIds.Add(criticalControl.Id);

            return new StepDefinition(id, instruction, expectedControl.Id, expectedAction, criticalControlIds);
        }

        /** Lists what is wrong with this step; empty when the step is valid */
        public List<string> FindProblems()
        {
            List<string> problems = new List<string>();

            if (string.IsNullOrWhiteSpace(id))
                problems.Add("Id is empty.");

            if (string.IsNullOrWhiteSpace(instruction))
                problems.Add("Instruction is empty.");

            if (expectedControl == null)
                problems.Add("Expected control is missing.");

            for (int i = 0; i < criticalControls.Count; i++)
            {
                if (criticalControls[i] == null)
                    problems.Add($"Critical control slot {i + 1} is empty.");
            }

            return problems;
        }

        /** Reports problems as soon as the asset is edited in the Inspector */
        private void OnValidate()
        {
            // Unity can also validate a temporary instance: only a saved asset always has a name
            if (string.IsNullOrEmpty(name))
                return;

            foreach (string problem in FindProblems())
                Debug.LogError($"[{name}] {problem}", this);
        }
    }
}
