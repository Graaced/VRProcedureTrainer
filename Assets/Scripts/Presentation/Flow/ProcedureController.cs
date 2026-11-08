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
     * It forwards what the user does on the machine to the runner, which stays free of Unity,
     * and shows the result through the highlight and the panel.
     * It must sit on a parent of all the machine controls.
     *
     */
    public sealed class ProcedureController : MonoBehaviour
    {
        /** Procedure the user must perform */
        [SerializeField] private TrainingProcedure procedure;

        /** Lights up the control of the current step */
        [SerializeField] private HighlightService highlightService;

        /** Shows the current step and the mistakes */
        [SerializeField] private InstructionPanel instructionPanel;

        /** Confirms outcomes with sound and vibration */
        [SerializeField] private FeedbackPlayer feedbackPlayer;

        /** Judges the actions of the user */
        private ProcedureRunner runner;

        /** Every machine control found under this object */
        private IMachineControl[] controls;

        /** Name shown to the user for each control, by control id */
        private Dictionary<string, string> controlNames;

        /** Validates the setup and creates the runner */
        private void Awake()
        {
            // Single & on purpose: every missing reference gets logged, not just the first
            bool isValid = IsAssigned(procedure, nameof(procedure))
                & IsAssigned(highlightService, nameof(highlightService))
                & IsAssigned(instructionPanel, nameof(instructionPanel))
                & IsAssigned(feedbackPlayer, nameof(feedbackPlayer));

            if (!isValid)
            {
                enabled = false;
                return;
            }

            // Includes inactive objects: a control hidden at the start must still be listened to
            controls = GetComponentsInChildren<IMachineControl>(true);

            controlNames = new Dictionary<string, string>();
            foreach (IMachineControl control in controls)
            {
                if (control.ControlId != null)
                    controlNames[control.ControlId.Id] = control.ControlId.DisplayName;
            }

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
            if (runner == null)
                return;

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
            // A failed Awake disables the component before any runner exists
            if (runner == null)
                return;

            foreach (IMachineControl control in controls)
                control.ActionPerformed -= HandleControlAction;

            runner.StepCompleted -= HandleStepCompleted;
            runner.ErrorOccurred -= HandleErrorOccurred;
            runner.EmergencyRecorded -= HandleEmergencyRecorded;
            runner.Finished -= HandleFinished;
        }

        /** Starts the timer and guides the user to the first step */
        private void Start()
        {
            runner.Start();
            ShowCurrentStep();
        }

        /** Passes an action of the user to the runner */
        private void HandleControlAction(ControlId control, ControlAction action)
        {
            runner.RegisterAction(control.Id, action);
        }

        /** Confirms the step and moves the guidance to the next one, if there is one */
        private void HandleStepCompleted(StepCompletedInfo info)
        {
            feedbackPlayer.PlaySuccess();

            if (runner.State == RunnerState.Running)
                ShowCurrentStep();
        }

        /** Signals a mistake of the user */
        private void HandleErrorOccurred(ProcedureError error)
        {
            feedbackPlayer.PlayError();

            string controlName = controlNames[error.ControlId];

            if (error.Type == ProcedureErrorType.Critical)
                instructionPanel.ShowCriticalError(controlName);
            else
                instructionPanel.ShowWrongOrder(controlName);
        }

        /** Logs a use of the emergency stop, which is never shown as a mistake */
        private void HandleEmergencyRecorded(ControlAction action, double atSeconds)
        {
            Debug.Log($"[{name}] Emergency stop {action} at {atSeconds:F1}s", this);
        }

        /** Removes the guidance and shows the outcome */
        private void HandleFinished(RunnerState state)
        {
            highlightService.Clear();
            instructionPanel.ShowFinished(state == RunnerState.Completed);
        }

        /** Highlights the control of the current step and shows its instruction */
        private void ShowCurrentStep()
        {
            StepDefinition step = runner.Steps[runner.CurrentStepIndex];

            highlightService.Highlight(step.ControlId);
            instructionPanel.ShowStep(runner.CurrentStepIndex + 1, runner.Steps.Count, step.Instruction);
        }

        /** Checks the scene has the control of every step, otherwise the procedure could never end */
        private bool HasEveryStepControl(ProcedureDefinition definition)
        {
            bool isValid = true;
            foreach (StepDefinition step in definition.Steps)
            {
                if (controlNames.ContainsKey(step.ControlId))
                    continue;

                Debug.LogError($"[{name}] No control '{step.ControlId}' under this object, needed by step '{step.Id}'", this);
                isValid = false;
            }

            return isValid;
        }

        /** Logs an error when a required reference is missing */
        private bool IsAssigned(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
                return true;

            Debug.LogError($"[{name}] Missing reference to {fieldName}", this);
            return false;
        }
    }
}
