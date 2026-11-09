using System;

namespace VRPT.Core
{
    /**
     * CLASS: ReportBuilder
     * DESCRIPTION: Listens to a runner and turns what happened into a report.
     * Reporting stays out of the runner, which only judges actions, and the verdict rule
     * can be tested without Unity. It lives as long as the runner it listens to.
     *
     */
    public sealed class ReportBuilder
    {
        /** Decimals kept for the times, enough for a person reading the file */
        private const int secondsDecimals = 2;

        /** Procedure being reported */
        private readonly ProcedureDefinition definition;

        /** Runner being listened to */
        private readonly ProcedureRunner runner;

        /** Report filled in during the session */
        private readonly ProcedureReport report;

        /** Raised once, when the procedure ends and the report is complete */
        public event Action<ProcedureReport> ReportReady;

        /** Starts listening to the runner; the start date is a parameter so tests stay deterministic */
        public ReportBuilder(ProcedureDefinition definition, ProcedureRunner runner, DateTime startedAtUtc)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.runner = runner ?? throw new ArgumentNullException(nameof(runner));

            report = new ProcedureReport
            {
                ProcedureId = definition.Id,
                ProcedureTitle = definition.Title,
                StartedAtUtc = startedAtUtc.ToString("o"),
                MaxAllowedErrors = definition.MaxAllowedErrors
            };

            runner.StepCompleted += HandleStepCompleted;
            runner.ErrorOccurred += HandleErrorOccurred;
            runner.EmergencyRecorded += HandleEmergencyRecorded;
            runner.Finished += HandleFinished;
        }

        /** Records a completed step */
        private void HandleStepCompleted(StepCompletedInfo info)
        {
            report.Steps.Add(new ReportStepEntry
            {
                StepId = info.StepId,
                CompletedAtSeconds = RoundSeconds(info.AtSeconds)
            });
        }

        /** Records a user mistake */
        private void HandleErrorOccurred(ProcedureError error)
        {
            report.ErrorCount++;

            if (error.Type == ProcedureErrorType.Critical)
                report.CriticalErrorCount++;

            report.Errors.Add(new ReportErrorEntry
            {
                StepId = error.StepId,
                ControlId = error.ControlId,
                Type = error.Type.ToString(),
                AtSeconds = RoundSeconds(error.AtSeconds)
            });
        }

        /** Records a use of the emergency stop, which never counts as an error */
        private void HandleEmergencyRecorded(ControlAction action, double atSeconds)
        {
            report.Events.Add(new ReportEventEntry
            {
                ControlId = definition.EmergencyControlId,
                Action = action.ToString(),
                AtSeconds = RoundSeconds(atSeconds)
            });
        }

        /** Applies the verdict rule and publishes the report */
        private void HandleFinished(RunnerState finalState)
        {
            report.DurationSeconds = RoundSeconds(runner.ElapsedSeconds);

            bool isPassed = report.CriticalErrorCount == 0 && report.ErrorCount <= definition.MaxAllowedErrors;
            report.Outcome = (isPassed ? ReportOutcome.Passed : ReportOutcome.Failed).ToString();

            ReportReady?.Invoke(report);
        }

        /** Shortens a time for the report */
        private static double RoundSeconds(double seconds)
        {
            return Math.Round(seconds, secondsDecimals);
        }
    }
}
