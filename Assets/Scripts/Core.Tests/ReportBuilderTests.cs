using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VRPT.Core.Tests
{
    /**
     * CLASS: ReportBuilderTests
     * DESCRIPTION: Defines what the report contains and when a session counts as passed.
     * Every test drives a real runner, so the report is checked against the real sequence of events.
     *
     */
    public sealed class ReportBuilderTests
    {
        /** Control treated as the emergency stop in these tests */
        private const string emergencyId = "emergency";

        /** Fixed start date, so the report can be compared exactly */
        private static readonly DateTime startedAtUtc = new DateTime(2026, 11, 10, 9, 12, 3, DateTimeKind.Utc);

        /** Clock driving the timings of the runner under test */
        private FakeClock clock;

        /** Reports published by the builder */
        private List<ProcedureReport> reports;

        /** Resets the shared state before each test */
        [SetUp]
        public void SetUp()
        {
            clock = new FakeClock();
            reports = new List<ProcedureReport>();
        }

        /** Checks the data of the procedure and the verdict of a clean session */
        [Test]
        public void ReportBuilder_CleanSession_OutcomePassedWithProcedureData()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 0);

            PerformAllSteps(runner);

            ProcedureReport report = reports[0];
            Assert.AreEqual(nameof(ReportOutcome.Passed), report.Outcome);
            Assert.IsTrue(report.IsPassed);
            Assert.AreEqual("loto", report.ProcedureId);
            Assert.AreEqual("Lockout", report.ProcedureTitle);
            Assert.AreEqual("2026-11-10T09:12:03.0000000Z", report.StartedAtUtc);
            Assert.AreEqual(0, report.MaxAllowedErrors);
            Assert.IsEmpty(report.Errors);
        }

        /** Checks the verdict rule when there are too many errors */
        [Test]
        public void ReportBuilder_ErrorsExceedMaximum_OutcomeFailed()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 1);

            runner.RegisterAction("start", ControlAction.Pressed);
            runner.RegisterAction("isolator", ControlAction.SwitchedOn);
            PerformAllSteps(runner);

            Assert.AreEqual(RunnerState.Completed, runner.State);
            Assert.AreEqual(2, reports[0].ErrorCount);
            Assert.AreEqual(nameof(ReportOutcome.Failed), reports[0].Outcome);
            Assert.IsFalse(reports[0].IsPassed);
        }

        /** Checks that the maximum itself is still allowed */
        [Test]
        public void ReportBuilder_ErrorsEqualMaximum_OutcomePassed()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 1);

            runner.RegisterAction("start", ControlAction.Pressed);
            PerformAllSteps(runner);

            Assert.AreEqual(1, reports[0].ErrorCount);
            Assert.AreEqual(nameof(ReportOutcome.Passed), reports[0].Outcome);
        }

        /** Checks that a critical error fails the session even if the procedure goes on */
        [Test]
        public void ReportBuilder_CriticalErrorWithinMaximum_OutcomeFailed()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 5, failOnCriticalError: false);

            runner.RegisterAction("guard", ControlAction.Opened);
            PerformAllSteps(runner);

            Assert.AreEqual(RunnerState.Completed, runner.State);
            Assert.AreEqual(1, reports[0].CriticalErrorCount);
            Assert.AreEqual(nameof(ReportOutcome.Failed), reports[0].Outcome);
        }

        /** Checks a critical error that ends the session early */
        [Test]
        public void ReportBuilder_CriticalErrorEndsProcedure_ReportsItWithDuration()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 5, failOnCriticalError: true);
            clock.Advance(7.5);

            runner.RegisterAction("guard", ControlAction.Opened);

            ProcedureReport report = reports[0];
            Assert.AreEqual(nameof(ReportOutcome.Failed), report.Outcome);
            Assert.AreEqual(7.5, report.DurationSeconds, 0.001);
            Assert.AreEqual(1, report.Errors.Count);
            Assert.AreEqual("stop", report.Errors[0].StepId);
            Assert.AreEqual("guard", report.Errors[0].ControlId);
            Assert.AreEqual(nameof(ProcedureErrorType.Critical), report.Errors[0].Type);
            Assert.AreEqual(7.5, report.Errors[0].AtSeconds, 0.001);
        }

        /** Checks the order and the timing of the completed steps, and the total time */
        [Test]
        public void ReportBuilder_CompletedSteps_RecordsTimesFromTheClock()
        {
            ProcedureRunner runner = CreateStartedRunner();

            clock.Advance(8.2);
            runner.RegisterAction("stop", ControlAction.Pressed);
            clock.Advance(1.3);
            runner.RegisterAction("isolator", ControlAction.SwitchedOff);

            ProcedureReport report = reports[0];
            Assert.AreEqual("stop", report.Steps[0].StepId);
            Assert.AreEqual(8.2, report.Steps[0].CompletedAtSeconds, 0.001);
            Assert.AreEqual("isolate", report.Steps[1].StepId);
            Assert.AreEqual(9.5, report.Steps[1].CompletedAtSeconds, 0.001);
            Assert.AreEqual(9.5, report.DurationSeconds, 0.001);
        }

        /** Checks that times are shortened for a person reading the file */
        [Test]
        public void ReportBuilder_LongDecimalTime_RoundsToTwoDecimals()
        {
            ProcedureRunner runner = CreateStartedRunner();
            clock.Advance(3.14159);

            PerformAllSteps(runner);

            Assert.AreEqual(3.14, reports[0].DurationSeconds);
        }

        /** Checks that the emergency stop is recorded as an event and never as an error */
        [Test]
        public void ReportBuilder_EmergencyStop_RecordedAsEventNotError()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 0);
            clock.Advance(2.0);

            runner.RegisterAction(emergencyId, ControlAction.Pressed);
            clock.Advance(1.0);
            runner.RegisterAction(emergencyId, ControlAction.Released);
            PerformAllSteps(runner);

            ProcedureReport report = reports[0];
            Assert.AreEqual(2, report.Events.Count);
            Assert.AreEqual(emergencyId, report.Events[0].ControlId);
            Assert.AreEqual(nameof(ControlAction.Pressed), report.Events[0].Action);
            Assert.AreEqual(2.0, report.Events[0].AtSeconds, 0.001);
            Assert.AreEqual(nameof(ControlAction.Released), report.Events[1].Action);
            Assert.AreEqual(0, report.ErrorCount);
            Assert.AreEqual(nameof(ReportOutcome.Passed), report.Outcome);
        }

        /** Checks that the report is published only at the end, and only once */
        [Test]
        public void ReportReady_DuringAndAfterProcedure_RaisedOnceAtTheEnd()
        {
            ProcedureRunner runner = CreateStartedRunner();

            runner.RegisterAction("stop", ControlAction.Pressed);
            Assert.IsEmpty(reports);

            runner.RegisterAction("isolator", ControlAction.SwitchedOff);
            runner.RegisterAction("stop", ControlAction.Pressed);

            Assert.AreEqual(1, reports.Count);
        }

        /** Checks that the saved text keeps enums as words and includes the nested lists */
        [Test]
        public void ProcedureReport_SerializedToJson_KeepsReadableValues()
        {
            ProcedureRunner runner = CreateStartedRunner(maxAllowedErrors: 0);
            runner.RegisterAction("start", ControlAction.Pressed);
            PerformAllSteps(runner);

            string json = JsonUtility.ToJson(reports[0]);

            StringAssert.Contains("\"Outcome\":\"Failed\"", json);
            StringAssert.Contains("\"StartedAtUtc\":\"2026-11-10T09:12:03.0000000Z\"", json);
            StringAssert.Contains("\"Type\":\"WrongOrder\"", json);
            StringAssert.Contains("\"StepId\":\"isolate\"", json);
            StringAssert.DoesNotContain("IsPassed", json);
        }

        /** Creates a started runner with a builder that stores what it publishes */
        private ProcedureRunner CreateStartedRunner(int maxAllowedErrors = 0, bool failOnCriticalError = true)
        {
            StepDefinition[] steps =
            {
                new StepDefinition("stop", "Press STOP", "stop", ControlAction.Pressed, new[] { "guard" }),
                new StepDefinition("isolate", "Turn off", "isolator", ControlAction.SwitchedOff, new[] { "guard" })
            };

            ProcedureDefinition definition = new ProcedureDefinition(
                "loto", "Lockout", steps, emergencyId, maxAllowedErrors, failOnCriticalError);

            ProcedureRunner runner = new ProcedureRunner(definition, clock);
            ReportBuilder builder = new ReportBuilder(definition, runner, startedAtUtc);
            builder.ReportReady += reports.Add;

            runner.Start();
            return runner;
        }

        /** Performs every step in the right order */
        private static void PerformAllSteps(ProcedureRunner runner)
        {
            runner.RegisterAction("stop", ControlAction.Pressed);
            runner.RegisterAction("isolator", ControlAction.SwitchedOff);
        }
    }
}
