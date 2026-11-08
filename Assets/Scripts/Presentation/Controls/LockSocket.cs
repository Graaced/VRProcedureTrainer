using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRPT.Core;
using VRPT.Data;

namespace VRPT.Presentation
{
    /**
     * CLASS: LockSocket
     * DESCRIPTION: Reports when the padlock is inserted into or removed from the isolator.
     * It wraps an XRSocketInteractor, which is not an interactable and cannot inherit from
     * MachineControl, so it implements the shared control contract directly.
     *
     */
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class LockSocket : MonoBehaviour, IMachineControl
    {
        /** Asset that identifies this control; must be assigned in the Inspector */
        [SerializeField] private ControlId controlId;

        /** Socket that physically holds the padlock */
        private XRSocketInteractor socket;

        /** Identity of this control, shared with the procedure steps */
        public ControlId ControlId => controlId;

        /** Raised once for each physical action performed on this control */
        public event Action<ControlId, ControlAction> ActionPerformed;

        /** Finds the socket and validates the setup */
        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();

            if (controlId == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(controlId)}", this);
                enabled = false;
            }
        }

        /** Starts listening to the socket */
        private void OnEnable()
        {
            socket.selectEntered.AddListener(HandlePadlockInserted);
            socket.selectExited.AddListener(HandlePadlockRemoved);
        }

        /** Stops listening to the socket */
        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(HandlePadlockInserted);
            socket.selectExited.RemoveListener(HandlePadlockRemoved);
        }


        /** Enables or disables the socket; a disabled socket also releases the padlock it holds */
        public void SetAvailable(bool available)
        {
            socket.socketActive = available;
        }

        /** Reports that the padlock entered the socket */
        private void HandlePadlockInserted(SelectEnterEventArgs args)
        {
            Raise(ControlAction.Inserted);
        }

        /** Reports that the padlock left the socket because the user took it */
        private void HandlePadlockRemoved(SelectExitEventArgs args)
        {
            // A canceled select comes from a disabled object, not from the user
            if (args.isCanceled)
                return;

            Raise(ControlAction.Removed);
        }

        /** Reports an action to every listener */
        private void Raise(ControlAction action)
        {
            ActionPerformed?.Invoke(controlId, action);
        }
    }
}
