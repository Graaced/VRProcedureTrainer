using System;
using System.Collections.Generic;
using UnityEngine;
using VRPT.Core;
using VRPT.Data;

namespace VRPT.Presentation
{
    /**
     * CLASS: ProcedureController
     * DESCRIPTION: The only bridge between the Unity scene and the procedure logic.
     * It forwards what the user does on the machine to the runner, which stays free of Unity.
     * It must sit on a parent of all the machine controls.
     *
     */
    public sealed class ProcedureController : MonoBehaviour
    {
        /** Procedure the user must perform */
        [SerializeField] private TrainingProcedure procedure;

        /** Judges the actions of the user */
        private ProcedureRunner runner;

        /** Every machine control found under this object */
        private IMachineControl[] controls;

        /** Validates the setup and creates the runner */
        private void Awake()
        {
            if (procedure == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(procedure)}", this);
                enabled = false;
                return;
            }

            // Includes inactive objects: a control hidden at the start must still be listened to
            controls = GetComponentsInChildren<IMachineControl>(true);

            ProcedureDefinition definition;
            try
            {
                definition = procedure.ToDefinition();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"[{name}] {exception.Message}", this);
                enabled = false;
                return;
            }

            if (!HasEveryStepControl(definition))
            {
                enabled = false;
                return;
            }

            runner = new ProcedureRunner(definition, new UnityClock());
        }

        /** Starts listening to the controls and to the runner */
        private void OnEnable()
        {
            foreach (IMachineControl control in controls)
                control.ActionPerformed += HandleControlAction;

            runner.StepCompleted += HandleStepCompleted;
            runner.ErrorOccurred += HandleErrorOccurred;
            runner.EmergencyRecorded += HandleEmergencyRecorded;
            runner.Finished += HandleFinished;
        }

        /** Stops listening to the controls and to the runner */
        private void OnDisable()
        {
            foreach (IMachineControl control in controls)
                control.ActionPerformed -= HandleControlAction;

            runner.StepCompleted -= HandleStepCompleted;
            runner.ErrorOccurred -= HandleErrorOccurred;
            runner.EmergencyRecorded -= HandleEmergencyRecorded;
            runner.Finished -= HandleFinished;
        }

        /** Starts the timer of the procedure */
        private void Start()
        {
            runner.Start();
            Debug.Log($"[{name}] Procedure '{procedure.name}' started", this);
        }

        /** Passes an action of the user to the runner */
        private void HandleControlAction(ControlId control, ControlAction action)
        {
            runner.RegisterAction(control.Id, action);
        }

        /** Logs a completed step */
        private void HandleStepCompleted(StepCompletedInfo info)
        {
            Debug.Log($"[{name}] Step '{info.StepId}' completed at {info.AtSeconds:F1}s", this);
        }

        /** Logs a mistake of the user */
        private void HandleErrorOccurred(ProcedureError error)
        {
            Debug.Log($"[{name}] {error.Type} error on step '{error.StepId}': control '{error.ControlId}' at {error.AtSeconds:F1}s", this);
        }

        /** Logs a use of the emergency stop */
        private void HandleEmergencyRecorded(ControlAction action, double atSeconds)
        {
            Debug.Log($"[{name}] Emergency stop {action} at {atSeconds:F1}s", this);
        }

        /** Logs the end of the procedure */
        private void HandleFinished(RunnerState state)
        {
            Debug.Log($"[{name}] Procedure finished: {state}", this);
        }

        /** Checks the scene has the control of every step, otherwise the procedure could never end */
        private bool HasEveryStepControl(ProcedureDefinition definition)
        {
            HashSet<string> sceneControlIds = new HashSet<string>();
            foreach (IMachineControl control in controls)
            {
                if (control.ControlId != null)
                    sceneControlIds.Add(control.ControlId.Id);
            }

            bool isValid = true;
            foreach (StepDefinition step in definition.Steps)
            {
                if (sceneControlIds.Contains(step.ControlId))
                    continue;

                Debug.LogError($"[{name}] No control '{step.ControlId}' under this object, needed by step '{step.Id}'", this);
                isValid = false;
            }

            return isValid;
        }
    }
}
