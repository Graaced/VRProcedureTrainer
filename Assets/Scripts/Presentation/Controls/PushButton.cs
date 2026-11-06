using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: PushButton
     * DESCRIPTION: Push button operated by selecting it, used for STOP, START and emergency stop.
     * It reports one action per press, so a second hand never counts as an extra press.
     * In latching mode it stays pressed until selected again, like a real emergency stop.
     *
     */
    public sealed class PushButton : MachineControl
    {
        /** Moving part of the button, e.g. Tasto or Fungo */
        [SerializeField] private Transform pressedPart;

        /** Local movement from rest to fully pressed, in meters */
        [SerializeField] private Vector3 pressedOffset = new Vector3(0f, 0f, -0.005f);

        /** Travel speed of the moving part, in meters per second */
        [Min(0.001f)]
        [SerializeField] private float travelSpeed = 0.05f;

        /** When true the button stays pressed until the next select */
        [SerializeField] private bool latching;

        /** Local position of the moving part when the button is up */
        private Vector3 restPosition;

        /** Current latch state; only used in latching mode */
        private bool latched;

        /** Validates the setup and prepares the button for use */
        protected override void Awake()
        {
            base.Awake();

            // Two hands must both count as selecting, otherwise the second one steals the select
            selectMode = InteractableSelectMode.Multiple;

            if (pressedPart == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(pressedPart)}", this);
                enabled = false;
                return;
            }

            restPosition = pressedPart.localPosition;
        }

        /** Turns the first select into a single press action */
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // Only the first hand counts as a press
            if (interactorsSelecting.Count > 1)
                return;

            if (!latching)
            {
                Raise(ControlAction.Pressed);
                return;
            }

            latched = !latched;
            Raise(latched ? ControlAction.Pressed : ControlAction.Released);
        }

        /** Moves the button towards its target position every frame */
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            // This method runs several times per frame: move only once
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            bool isDown = latching ? latched : isSelected;
            Vector3 target = isDown ? restPosition + pressedOffset : restPosition;

            pressedPart.localPosition = Vector3.MoveTowards(
                pressedPart.localPosition, target, travelSpeed * Time.deltaTime);
        }
    }
}

