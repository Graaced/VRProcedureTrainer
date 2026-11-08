using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: RotarySwitch
     * DESCRIPTION: Two-position rotary isolator switch, operated by dragging or clicking.
     * The state changes only on release and the handle follows it, so a half-finished
     * movement never reaches the machine.
     *
     */
    public sealed class RotarySwitch : MachineControl
    {
        /** Pointer closer than this to the rotation axis gives an unstable angle, in meters */
        private const float minPointerRadius = 0.01f;

        /** Rotating part of the switch, e.g. Maniglia */
        [SerializeField] private Transform handle;

        /** Local Z angle of the handle in the ON position, in degrees */
        [SerializeField] private float onAngle = 0f;

        /** Local Z angle of the handle in the OFF position, in degrees */
        [SerializeField] private float offAngle = -90f;

        /** Rotation speed of the handle when snapping, in degrees per second */
        [Min(1f)]
        [SerializeField] private float rotationSpeed = 360f;

        /** Pointer rotation needed before a select counts as a drag, in degrees */
        [Min(0f)]
        [SerializeField] private float dragThreshold = 8f;

        /** When true a select without dragging toggles the switch */
        [SerializeField] private bool allowClickToggle = true;

        /** Current position of the switch; the machine starts powered */
        private bool isOn = true;

        /** Current local Z angle of the handle, in degrees */
        private float currentAngle;

        /** True once the current select has moved past the drag threshold */
        private bool isDragging;

        /** True once the starting pointer angle of the current select is known */
        private bool hasGrabAngle;

        /** Pointer angle when the current select started, in degrees */
        private float grabPointerAngle;

        /** Handle angle when the current select started, in degrees */
        private float grabHandleAngle;

        /** Validates the setup and places the handle in the ON position */
        protected override void Awake()
        {
            base.Awake();

            if (handle == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(handle)}", this);
                enabled = false;
                return;
            }

            currentAngle = onAngle;
            ApplyAngle();
        }

        /** Prepares a new drag */
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            isDragging = false;
            hasGrabAngle = false;
        }

        /** Decides the new switch position when the user lets go */
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            bool wasDragging = isDragging;
            isDragging = false;

            // A canceled select is not a user decision: the handle returns to its position
            if (args.isCanceled)
                return;

            if (wasDragging)
                SetState(Mathf.Abs(currentAngle - onAngle) < Mathf.Abs(currentAngle - offAngle));
            else if (allowClickToggle)
                SetState(!isOn);
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
                float targetAngle = isOn ? onAngle : offAngle;
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            }

            ApplyAngle();
        }

        /** Applies the pointer rotation to the handle, within its two end positions */
        private void UpdateDrag()
        {
            if (!TryGetPointerAngle(out float pointerAngle))
                return;

            if (!hasGrabAngle)
            {
                grabPointerAngle = pointerAngle;
                grabHandleAngle = currentAngle;
                hasGrabAngle = true;
                return;
            }

            // DeltaAngle avoids a jump when the angle wraps around at 180 degrees
            float delta = Mathf.DeltaAngle(grabPointerAngle, pointerAngle);

            if (!isDragging && Mathf.Abs(delta) < dragThreshold)
                return;

            isDragging = true;
            currentAngle = Mathf.Clamp(grabHandleAngle + delta,
                Mathf.Min(onAngle, offAngle), Mathf.Max(onAngle, offAngle));
        }

        /** Computes the pointer angle around the handle axis; false if too close to the axis */
        private bool TryGetPointerAngle(out float angle)
        {
            Transform attach = interactorsSelecting[0].GetAttachTransform(this);
            Vector3 offset = transform.InverseTransformDirection(attach.position - handle.position);

            if (new Vector2(offset.x, offset.y).sqrMagnitude < minPointerRadius * minPointerRadius)
            {
                angle = 0f;
                return false;
            }

            // Same convention as the Z rotation: 0 points up, positive turns towards -X
            angle = Mathf.Atan2(-offset.x, offset.y) * Mathf.Rad2Deg;
            return true;
        }

        /** Changes the switch position and reports it, only if it actually changed */
        private void SetState(bool newIsOn)
        {
            if (newIsOn == isOn)
                return;

            isOn = newIsOn;
            Raise(isOn ? ControlAction.SwitchedOn : ControlAction.SwitchedOff);
        }

        /** Writes the current angle to the handle */
        private void ApplyAngle()
        {
            handle.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }
}


