using System;
using System.Collections.Generic;

namespace VRPT.Core
{
    /**
     * CLASS: ProcedureRunner
     * DESCRIPTION: State machine that judges the steps of a procedure.
     * It receives abstract actions and reports the outcome through events, without moving
     * anything or knowing Unity, so every rule can be tested in milliseconds.
     *
     */
    public sealed class ProcedureRunner
    {
        /** Procedure being evaluated */
        private readonly ProcedureDefinition definition;

        /** Source of time for the timings */
        private readonly IClock clock;

        /** Clock value when the procedure started, in seconds */
        private double startedAtSeconds;

        /** Raised when the current step is completed correctly */
        public event Action<StepCompletedInfo> StepCompleted;

        /** Raised for every wrong action; the current step does not advance */
        public event Action<ProcedureError> ErrorOccurred;

        /** Raised for every use of the emergency stop, with the action and the seconds elapsed */
        public event Action<ControlAction, double> EmergencyRecorded;

        /** Raised once, when the procedure is completed or failed */
        public event Action<RunnerState> Finished;

        /** Current overall state */
        public RunnerState State { get; private set; }

        /** Index of the step the user must perform now; stays on the last step at the end */
        public int CurrentStepIndex { get; private set; }

        /** Steps of the procedure, for the UI */
        public IReadOnlyList<StepDefinition> Steps => definition.Steps;

        /** Seconds elapsed since the procedure started */
        public double ElapsedSeconds => clock.NowSeconds - startedAtSeconds;

        /** Validates the definition; throws ArgumentException if it is not valid */
        public ProcedureRunner(ProcedureDefinition definition, IClock clock)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (definition.Steps.Count == 0)
                throw new ArgumentException($"Procedure '{definition.Id}' has no steps.", nameof(definition));

            HashSet<string> stepIds = new HashSet<string>();
            foreach (StepDefinition step in definition.Steps)
            {
                if (!stepIds.Add(step.Id))
                    throw new ArgumentException($"Procedure '{definition.Id}' has a duplicate step id '{step.Id}'.", nameof(definition));
            }
        }

        /** Starts the timer; calling it twice is a programming error */
        public void Start()
        {
            if (State != RunnerState.NotStarted)
                throw new InvalidOperationException($"Procedure '{definition.Id}' has already been started.");

            startedAtSeconds = clock.NowSeconds;
            State = RunnerState.Running;
        }

        /** Single entry point for the user's actions on the machine */
        public void RegisterAction(string controlId, ControlAction action)
        {
            if (State != RunnerState.Running)
                return;

            // The emergency stop comes first: using it must never be penalized
            if (controlId == definition.EmergencyControlId)
            {
                EmergencyRecorded?.Invoke(action, ElapsedSeconds);
                return;
            }

            StepDefinition currentStep = definition.Steps[CurrentStepIndex];

            if (Matches(currentStep, controlId, action))
            {
                CompleteCurrentStep(currentStep);
                return;
            }

            // A repeated event of a completed step is input noise, not a user mistake
            if (MatchesCompletedStep(controlId, action))
                return;

            if (IsCritical(currentStep, controlId))
                ReportCriticalError(currentStep, controlId);
            else
                ErrorOccurred?.Invoke(new ProcedureError(currentStep.Id, controlId, ProcedureErrorType.WrongOrder, ElapsedSeconds));
        }

        /** Advances to the next step, or ends the procedure after the last one */
        private void CompleteCurrentStep(StepDefinition step)
        {
            int completedIndex = CurrentStepIndex;
            bool wasLastStep = completedIndex == definition.Steps.Count - 1;

            // State changes before the events, so a listener always reads the new situation
            if (wasLastStep)
                State = RunnerState.Completed;
            else
                CurrentStepIndex++;

            StepCompleted?.Invoke(new StepCompletedInfo(step.Id, completedIndex, ElapsedSeconds));

            if (wasLastStep)
                Finished?.Invoke(RunnerState.Completed);
        }

        /** Reports a critical error and ends the procedure if it is configured to */
        private void ReportCriticalError(StepDefinition step, string controlId)
        {
            if (definition.FailOnCriticalError)
                State = RunnerState.Failed;

            ErrorOccurred?.Invoke(new ProcedureError(step.Id, controlId, ProcedureErrorType.Critical, ElapsedSeconds));

            if (definition.FailOnCriticalError)
                Finished?.Invoke(RunnerState.Failed);
        }

        /** True if the action is exactly what the step expects */
        private static bool Matches(StepDefinition step, string controlId, ControlAction action)
        {
            return step.ControlId == controlId && step.Action == action;
        }

        /** True if the action repeats a step that is already completed */
        private bool MatchesCompletedStep(string controlId, ControlAction action)
        {
            for (int i = 0; i < CurrentStepIndex; i++)
            {
                if (Matches(definition.Steps[i], controlId, action))
                    return true;
            }

            return false;
        }

        /** True if the control is listed as dangerous for the step */
        private static bool IsCritical(StepDefinition step, string controlId)
        {
            foreach (string criticalControlId in step.CriticalControlIds)
            {
                if (criticalControlId == controlId)
                    return true;
            }

            return false;
        }
    }
}
