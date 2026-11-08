using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VRPT.Core.Tests
{
    /**
     * CLASS: ProcedureRunnerTests
     * DESCRIPTION: Defines the evaluation rules of a procedure, using the lockout sequence.
     * They run in EditMode without Unity scenes, so every rule is checked in milliseconds.
     *
     */
    public sealed class ProcedureRunnerTests
    {
        /** Control treated as the emergency stop in these tests */
        private const string emergencyId = "emergency";

        /** Clock driving the timings of the runner under test */
        private FakeClock clock;

        /** Steps reported as completed */
        private List<StepCompletedInfo> completedSteps;

        /** Errors reported by the runner */
        private List<ProcedureError> errors;

        /** Emergency stop uses reported by the runner */
        private List<ControlAction> emergencies;

        /** Final states reported by the runner */
        private List<RunnerState> finishedStates;

        /** Resets the shared state before each test */
        [SetUp]
        public void SetUp()
        {
            clock = new FakeClock();
            completedSteps = new List<StepCompletedInfo>();
            errors = new List<ProcedureError>();
            emergencies = new List<ControlAction>();
            finishedStates = new List<RunnerState>();
        }

        /** Checks the happy path from the first step to the last */
        [Test]
        public void RegisterAction_CorrectSequence_CompletesProcedure()
        {
            ProcedureRunner runner = CreateStartedRunner();

            PerformAllSteps(runner);

            Assert.AreEqual(RunnerState.Completed, runner.State);
            Assert.AreEqual(5, completedSteps.Count);
            Assert.IsEmpty(errors);
        }

        /** Checks that an unexpected control is recorded and the step stays the same */
        [Test]
        public void RegisterAction_WrongControl_RaisesErrorAndDoesNotAdvance()
        {
            ProcedureRunner runner = CreateStartedRunner();

            runner.RegisterAction("start", ControlAction.Pressed);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ProcedureErrorType.WrongOrder, errors[0].Type);
            Assert.AreEqual("stop", errors[0].StepId);
            Assert.AreEqual("start", errors[0].ControlId);
            Assert.AreEqual(0, runner.CurrentStepIndex);
        }

        /** Checks that a critical control ends the procedure when configured to */
        [Test]
        public void RegisterAction_CriticalControl_FailsWhenFailOnCriticalError()
        {
            ProcedureRunner runner = CreateStartedRunner(failOnCriticalError: true);

            runner.RegisterAction("guard", ControlAction.Opened);

            Assert.AreEqual(ProcedureErrorType.Critical, errors[0].Type);
            Assert.AreEqual(RunnerState.Failed, runner.State);
            CollectionAssert.AreEqual(new[] { RunnerState.Failed }, finishedStates);
        }

        /** Checks that a critical control only records the error when configured to continue */
        [Test]
        public void RegisterAction_CriticalControl_ContinuesWhenFailOnCriticalErrorIsFalse()
        {
            ProcedureRunner runner = CreateStartedRunner(failOnCriticalError: false);

            runner.RegisterAction("guard", ControlAction.Opened);

            Assert.AreEqual(ProcedureErrorType.Critical, errors[0].Type);
            Assert.AreEqual(RunnerState.Running, runner.State);
            Assert.AreEqual(0, runner.CurrentStepIndex);
            Assert.IsEmpty(finishedStates);
        }

        /** Checks that a repeated event of a completed step is input noise, not a mistake */
        [Test]
        public void RegisterAction_RepeatedPreviousStep_IsIgnored()
        {
            ProcedureRunner runner = CreateStartedRunner();
            runner.RegisterAction("stop", ControlAction.Pressed);

            runner.RegisterAction("stop", ControlAction.Pressed);

            Assert.IsEmpty(errors);
            Assert.AreEqual(1, runner.CurrentStepIndex);
            Assert.AreEqual(1, completedSteps.Count);
        }

        /** Checks that using the emergency stop is recorded and never penalized */
        [Test]
        public void RegisterAction_EmergencyStop_IsRecordedAndIsNotAnError()
        {
            ProcedureRunner runner = CreateStartedRunner();

            runner.RegisterAction(emergencyId, ControlAction.Pressed);

            CollectionAssert.AreEqual(new[] { ControlAction.Pressed }, emergencies);
            Assert.IsEmpty(errors);
            Assert.AreEqual(0, runner.CurrentStepIndex);
            Assert.AreEqual(RunnerState.Running, runner.State);
        }

        /** Checks that nothing is evaluated once the procedure has ended */
        [Test]
        public void RegisterAction_AfterFinish_IsIgnored()
        {
            ProcedureRunner runner = CreateStartedRunner();
            PerformAllSteps(runner);

            runner.RegisterAction("start", ControlAction.Pressed);

            Assert.IsEmpty(errors);
            Assert.AreEqual(RunnerState.Completed, runner.State);
        }

        /** Checks that nothing is evaluated before the procedure starts */
        [Test]
        public void RegisterAction_BeforeStart_IsIgnored()
        {
            ProcedureRunner runner = CreateRunner();

            runner.RegisterAction("stop", ControlAction.Pressed);

            Assert.IsEmpty(errors);
            Assert.IsEmpty(completedSteps);
            Assert.AreEqual(RunnerState.NotStarted, runner.State);
        }

        /** Checks that the right control with the wrong action is still a mistake */
        [Test]
        public void RegisterAction_SameControlDifferentAction_CountsAsError()
        {
            ProcedureRunner runner = CreateStartedRunner();
            runner.RegisterAction("stop", ControlAction.Pressed);

            runner.RegisterAction("isolator", ControlAction.SwitchedOn);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ProcedureErrorType.WrongOrder, errors[0].Type);
            Assert.AreEqual(1, runner.CurrentStepIndex);
        }

        /** Checks that a procedure without steps is rejected */
        [Test]
        public void Constructor_NoSteps_ThrowsArgumentException()
        {
            ProcedureDefinition definition = new ProcedureDefinition(
                "empty", "Empty", new StepDefinition[0], emergencyId, 0, true);

            Assert.Throws<ArgumentException>(() => new ProcedureRunner(definition, clock));
        }

        /** Checks that two steps cannot share the same identifier */
        [Test]
        public void Constructor_DuplicateStepIds_ThrowsArgumentException()
        {
            StepDefinition[] steps =
            {
                new StepDefinition("same", "First", "stop", ControlAction.Pressed),
                new StepDefinition("same", "Second", "start", ControlAction.Pressed)
            };
            ProcedureDefinition definition = new ProcedureDefinition(
                "duplicate", "Duplicate", steps, emergencyId, 0, true);

            Assert.Throws<ArgumentException>(() => new ProcedureRunner(definition, clock));
        }

        /** Checks that starting twice is reported as a programming error */
        [Test]
        public void Start_CalledTwice_ThrowsInvalidOperationException()
        {
            ProcedureRunner runner = CreateStartedRunner();

            Assert.Throws<InvalidOperationException>(() => runner.Start());
        }

        /** Checks that the end of the procedure is announced exactly once */
        [Test]
        public void Finished_IsRaisedOnlyOnce()
        {
            ProcedureRunner runner = CreateStartedRunner();
            PerformAllSteps(runner);

            runner.RegisterAction("guard", ControlAction.Opened);

            CollectionAssert.AreEqual(new[] { RunnerState.Completed }, finishedStates);
        }

        /** Checks that timings are measured from the start of the procedure */
        [Test]
        public void StepCompleted_ReportsTimeFromFakeClock()
        {
            ProcedureRunner runner = CreateStartedRunner();
            clock.Advance(8.5);

            runner.RegisterAction("stop", ControlAction.Pressed);

            Assert.AreEqual(8.5, completedSteps[0].AtSeconds, 0.0001);
        }

        /** Creates a runner for the lockout sequence, listening to all its events */
        private ProcedureRunner CreateRunner(bool failOnCriticalError = true)
        {
            string[] guardIsCritical = { "guard" };
            StepDefinition[] steps =
            {
                new StepDefinition("stop", "Stop the machine", "stop", ControlAction.Pressed, guardIsCritical),
                new StepDefinition("isolate", "Turn the isolator off", "isolator", ControlAction.SwitchedOff, guardIsCritical),
                new StepDefinition("lock", "Fit your padlock", "lock", ControlAction.Inserted, guardIsCritical),
                new StepDefinition("verify", "Try to start the machine", "start", ControlAction.Pressed, guardIsCritical),
                new StepDefinition("open", "Open the guard", "guard", ControlAction.Opened)
            };
            ProcedureDefinition definition = new ProcedureDefinition(
                "loto", "Lockout", steps, emergencyId, 0, failOnCriticalError);

            ProcedureRunner runner = new ProcedureRunner(definition, clock);
            runner.StepCompleted += completedSteps.Add;
            runner.ErrorOccurred += errors.Add;
            runner.EmergencyRecorded += (action, atSeconds) => emergencies.Add(action);
            runner.Finished += finishedStates.Add;
            return runner;
        }

        /** Creates a runner that is already running */
        private ProcedureRunner CreateStartedRunner(bool failOnCriticalError = true)
        {
            ProcedureRunner runner = CreateRunner(failOnCriticalError);
            runner.Start();
            return runner;
        }

        /** Performs every step of the lockout sequence in the right order */
        private static void PerformAllSteps(ProcedureRunner runner)
        {
            runner.RegisterAction("stop", ControlAction.Pressed);
            runner.RegisterAction("isolator", ControlAction.SwitchedOff);
            runner.RegisterAction("lock", ControlAction.Inserted);
            runner.RegisterAction("start", ControlAction.Pressed);
            runner.RegisterAction("guard", ControlAction.Opened);
        }
    }
}
