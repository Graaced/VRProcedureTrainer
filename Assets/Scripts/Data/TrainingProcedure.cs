using System;
using System.Collections.Generic;
using UnityEngine;
using VRPT.Core;

namespace VRPT.Data
{
    /**
     * CLASS: TrainingProcedure
     * DESCRIPTION: A complete procedure authored as an ordered list of step assets.
     * It converts itself to a plain ProcedureDefinition, so the runner never depends on Unity.
     *
     */
    [CreateAssetMenu(fileName = "TrainingProcedure", menuName = "VRPT/Training Procedure")]
    public sealed class TrainingProcedure : ScriptableObject
    {
        /** Stable identifier used in the report, e.g. "loto-press" */
        [SerializeField] private string id;

        /** Title shown to the user */
        [SerializeField] private string title;

        /** Ordered steps of the procedure */
        [SerializeField] private List<ProcedureStep> steps = new List<ProcedureStep>();

        /** Control treated as the emergency stop, which is never an error */
        [SerializeField] private ControlId emergencyControl;

        /** Maximum number of errors for the procedure to count as passed */
        [SerializeField, Min(0)] private int maxAllowedErrors;

        /** When true a critical error ends the procedure immediately */
        [SerializeField] private bool failOnCriticalError = true;

        /** Converts the asset to plain data; throws InvalidOperationException if it is not valid */
        public ProcedureDefinition ToDefinition()
        {
            // OnValidate runs only in the editor, so a build needs this check too
            List<string> problems = FindProblems();
            if (problems.Count > 0)
                throw new InvalidOperationException($"Procedure '{name}' is not valid: {string.Join(" ", problems)}");

            List<StepDefinition> stepDefinitions = new List<StepDefinition>(steps.Count);
            foreach (ProcedureStep step in steps)
                stepDefinitions.Add(step.ToDefinition());

            return new ProcedureDefinition(id, title, stepDefinitions, emergencyControl.Id, maxAllowedErrors, failOnCriticalError);
        }

        /** Lists what is wrong with the procedure and its steps; empty when it is valid */
        public List<string> FindProblems()
        {
            List<string> problems = new List<string>();

            if (string.IsNullOrWhiteSpace(id))
                problems.Add("Id is empty.");

            if (string.IsNullOrWhiteSpace(title))
                problems.Add("Title is empty.");

            if (emergencyControl == null)
                problems.Add("Emergency control is missing.");

            if (steps.Count == 0)
                problems.Add("The procedure has no steps.");

            HashSet<string> stepIds = new HashSet<string>();
            for (int i = 0; i < steps.Count; i++)
            {
                ProcedureStep step = steps[i];
                if (step == null)
                {
                    problems.Add($"Step slot {i + 1} is empty.");
                    continue;
                }

                foreach (string stepProblem in step.FindProblems())
                    problems.Add($"Step '{step.name}': {stepProblem}");

                if (!string.IsNullOrWhiteSpace(step.Id) && !stepIds.Add(step.Id))
                    problems.Add($"Step id '{step.Id}' is used more than once.");

                // The runner handles the emergency control first, so this step could never complete
                if (emergencyControl != null && step.ExpectedControl == emergencyControl)
                    problems.Add($"Step '{step.name}' expects the emergency control and could never be completed.");
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
