using UnityEngine;
using VRPT.Core;
using VRPT.Data;
using TMPro;

namespace VRPT.Presentation
{
    /**
     * CLASS: MachineSimulation
     * DESCRIPTION: Physical state of the press: power, motion and lockout rules.
     * It reacts to the controls whether or not a procedure is running, because the machine
     * is the world being simulated, while judging the user belongs to the ProcedureRunner.
     *
     */
    public sealed class MachineSimulation : MonoBehaviour
    {
        /** Stops the machine */
        [SerializeField] private PushButton stopButton;

        /** Starts the machine, only if power is present and the emergency stop is released */
        [SerializeField] private PushButton startButton;

        /** Stops the machine and prevents any start while latched */
        [SerializeField] private PushButton emergencyButton;

        /** Connects the machine to the power supply */
        [SerializeField] private RotarySwitch isolator;

        /** Holds the padlock on the isolator */
        [SerializeField] private LockSocket lockSocket;

        /** Rotating flywheel of the press */
        [SerializeField] private Transform flywheel;

        /** Flywheel speed while running, in degrees per second */
        [Min(0f)]
        [SerializeField] private float flywheelMaxSpeed = 360f;

        /** Flywheel speed change, in degrees per second squared */
        [Min(1f)]
        [SerializeField] private float flywheelAcceleration = 180f;

        /** Lit while the machine has power */
        [SerializeField] private IndicatorLamp powerLamp;

        /** Lit while the machine is running */
        [SerializeField] private IndicatorLamp runningLamp;

        /** Shows the voltage reaching the machine */
        [SerializeField] private TMP_Text voltageDisplay;

        /** Text shown on the display when the machine has power */
        [SerializeField] private string poweredVoltageText = "400 V";

        /** Text shown on the display when the machine is isolated */
        [SerializeField] private string isolatedVoltageText = "0 V";


        /** True while the isolator connects the machine to the power supply */
        private bool hasPower = true;

        /** True while the motor is driving the machine */
        private bool isRunning = true;

        /** True while the emergency stop is pressed in */
        private bool isEmergencyLatched;

        /** Current flywheel speed, in degrees per second */
        private float flywheelSpeed;

        /** Validates the setup; the machine is already running when the scene starts */
        private void Awake()
        {
            // Single & on purpose: every missing reference gets logged, not just the first
            bool isValid = IsAssigned(stopButton, nameof(stopButton))
                & IsAssigned(startButton, nameof(startButton))
                & IsAssigned(emergencyButton, nameof(emergencyButton))
                & IsAssigned(isolator, nameof(isolator))
                & IsAssigned(lockSocket, nameof(lockSocket))
                & IsAssigned(flywheel, nameof(flywheel))
                & IsAssigned(powerLamp, nameof(powerLamp))
                & IsAssigned(runningLamp, nameof(runningLamp))
                & IsAssigned(voltageDisplay, nameof(voltageDisplay));


            if (!isValid)
            {
                enabled = false;
                return;
            }

            flywheelSpeed = flywheelMaxSpeed;
        }

        /** Applies the initial rules once every control has finished its own Awake */
        private void Start()
        {
            // The padlock can only be fitted with the isolator in OFF, and it starts in ON
            lockSocket.SetAvailable(false);
            UpdateIndicators();

        }

        /** Starts listening to the controls */
        private void OnEnable()
        {
            stopButton.ActionPerformed += HandleStopButton;
            startButton.ActionPerformed += HandleStartButton;
            emergencyButton.ActionPerformed += HandleEmergencyButton;
            isolator.ActionPerformed += HandleIsolator;
            lockSocket.ActionPerformed += HandleLockSocket;
        }

        /** Stops listening to the controls */
        private void OnDisable()
        {
            stopButton.ActionPerformed -= HandleStopButton;
            startButton.ActionPerformed -= HandleStartButton;
            emergencyButton.ActionPerformed -= HandleEmergencyButton;
            isolator.ActionPerformed -= HandleIsolator;
            lockSocket.ActionPerformed -= HandleLockSocket;
        }

        /** Spins the flywheel up or down towards the speed of the current state */
        private void Update()
        {
            float targetSpeed = isRunning ? flywheelMaxSpeed : 0f;
            flywheelSpeed = Mathf.MoveTowards(flywheelSpeed, targetSpeed, flywheelAcceleration * Time.deltaTime);
            flywheel.Rotate(Vector3.right, flywheelSpeed * Time.deltaTime, Space.Self);
        }

        /** STOP always stops the machine */
        private void HandleStopButton(ControlId control, ControlAction action)
        {
            isRunning = false;
            UpdateIndicators();
        }

        /** START works only with power present and the emergency stop released */
        private void HandleStartButton(ControlId control, ControlAction action)
        {
            if (hasPower && !isEmergencyLatched)
                isRunning = true;

            UpdateIndicators();
        }

        /** The emergency stop halts the machine; releasing it never restarts it */
        private void HandleEmergencyButton(ControlId control, ControlAction action)
        {
            isEmergencyLatched = action == ControlAction.Pressed;

            if (isEmergencyLatched)
                isRunning = false;

            UpdateIndicators();
        }

        /** The isolator cuts the power and decides whether the padlock can be fitted */
        private void HandleIsolator(ControlId control, ControlAction action)
        {
            hasPower = action == ControlAction.SwitchedOn;
            lockSocket.SetAvailable(!hasPower);

            if (!hasPower)
                isRunning = false;

            UpdateIndicators();
        }

        private void UpdateIndicators()
        {
            powerLamp.SetLit(hasPower);
            runningLamp.SetLit(isRunning);
            voltageDisplay.text = hasPower ? poweredVoltageText : isolatedVoltageText;
        }

        /** A fitted padlock keeps the isolator from being turned back on */
        private void HandleLockSocket(ControlId control, ControlAction action)
        {
            isolator.SetLocked(action == ControlAction.Inserted);
        }

        /** Logs an error when a required reference is missing */
        private bool IsAssigned(Object reference, string fieldName)
        {
            if (reference != null)
                return true;

            Debug.LogError($"[{name}] Missing reference to {fieldName}", this);
            return false;
        }
    }
}

