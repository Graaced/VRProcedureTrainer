using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPT.Presentation
{
    /**
     * CLASS: RotaryControl
     * DESCRIPTION: Base class for controls that rotate between two positions by dragging or clicking.
     * The position changes only on release and the moving part follows it, so a half-finished
     * movement never reaches the machine. Derived classes only say what each position means.
     *
     */
    public abstract class RotaryControl : MachineControl
    {
        /** Pointer closer than this to the rotation axis gives an unstable direction, in meters */
        private const float minPointerRadius = 0.01f;

        /** Part that rotates, e.g. Maniglia; must be a child of another object */
        [FormerlySerializedAs("handle")]
        [SerializeField] private Transform rotatingPart;

        /** Local axis of the rotation */
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

        /** Angle of the first position, the starting one, in degrees */
        [FormerlySerializedAs("onAngle")]
        [SerializeField] private float firstAngle = 0f;

        /** Angle of the second position, in degrees */
        [FormerlySerializedAs("offAngle")]
        [SerializeField] private float secondAngle = -90f;

        /** Fraction of the travel past which a drag ends in the second position */
        [Range(0.05f, 0.95f)]
        [SerializeField] private float positionThreshold = 0.5f;

        /** Rotation speed when snapping, in degrees per second */
        [Min(1f)]
        [SerializeField] private float rotationSpeed = 360f;

        /** Pointer rotation needed before a select counts as a drag, in degrees */
        [Min(0f)]
        [SerializeField] private float dragThreshold = 8f;

        /** When true a select without dragging toggles the position */
        [SerializeField] private bool allowClickToggle = true;

        /** Current logical position of the control */
        private bool isAtSecondPosition;

        /** Current angle of the rotating part, in degrees */
        private float currentAngle;

        /** True once the current select has moved past the drag threshold */
        private bool isDragging;

        /** True once the starting pointer direction of the current select is known */
        private bool hasGrabDirection;

        /** Pointer direction when the current select started */
        private Vector3 grabDirection;

        /** Angle of the rotating part when the current select started, in degrees */
        private float grabAngle;

        /** Whether the user is currently allowed to change the position */
        protected virtual bool CanChangePosition => true;

        /** Reports the new position to the machine using the action that fits this control */
        protected abstract void ReportPosition(bool atSecondPosition);

        /** Validates the setup and places the part in the first position */
        protected override void Awake()
        {
            base.Awake();

            if (rotatingPart == null || rotatingPart.parent == null)
            {
                Debug.LogError($"[{name}] {nameof(rotatingPart)} is missing or has no parent", this);
                enabled = false;
                return;
            }

            currentAngle = firstAngle;
            ApplyAngle();
        }

        /** Prepares a new drag */
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            isDragging = false;
            hasGrabDirection = false;
        }

        /** Decides the new position when the user lets go */
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            bool wasDragging = isDragging;
            isDragging = false;

            // A canceled or blocked select is not a user decision: the part returns to its position
            if (args.isCanceled || !CanChangePosition)
                return;

            if (wasDragging)
                SetPosition(Mathf.InverseLerp(firstAngle, secondAngle, currentAngle) >= positionThreshold);
            else if (allowClickToggle)
                SetPosition(!isAtSecondPosition);
        }

        /** Follows the pointer while dragging, otherwise rotates towards the current position */
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            // This method runs several times per frame: move only once
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            if (isSelected)
                UpdateDrag();

            if (!isDragging)
            {
                float targetAngle = isAtSecondPosition ? secondAngle : firstAngle;
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            }

            ApplyAngle();
        }

        /** Applies the pointer rotation to the part, within its two end positions */
        private void UpdateDrag()
        {
            if (!TryGetPointerDirection(out Vector3 direction))
                return;

            if (!hasGrabDirection)
            {
                grabDirection = direction;
                grabAngle = currentAngle;
                hasGrabDirection = true;
                return;
            }

            float delta = Vector3.SignedAngle(grabDirection, direction, rotationAxis);

            if (!isDragging && Mathf.Abs(delta) < dragThreshold)
                return;

            isDragging = true;
            currentAngle = Mathf.Clamp(grabAngle + delta,
                Mathf.Min(firstAngle, secondAngle), Mathf.Max(firstAngle, secondAngle));
        }

        /** Computes the pointer direction around the axis; false if too close to the axis */
        private bool TryGetPointerDirection(out Vector3 direction)
        {
            Transform attach = interactorsSelecting[0].GetAttachTransform(this);

            // Uses the parent space: the rotating part's own space turns together with it
            Vector3 offset = rotatingPart.parent.InverseTransformDirection(attach.position - rotatingPart.position);

            direction = Vector3.ProjectOnPlane(offset, rotationAxis);
            return direction.sqrMagnitude >= minPointerRadius * minPointerRadius;
        }

        /** Changes the position and reports it, only if it actually changed */
        private void SetPosition(bool atSecondPosition)
        {
            if (atSecondPosition == isAtSecondPosition)
                return;

            isAtSecondPosition = atSecondPosition;
            ReportPosition(isAtSecondPosition);
        }

        /** Writes the current angle to the rotating part */
        private void ApplyAngle()
        {
            rotatingPart.localRotation = Quaternion.AngleAxis(currentAngle, rotationAxis);
        }
    }
}

