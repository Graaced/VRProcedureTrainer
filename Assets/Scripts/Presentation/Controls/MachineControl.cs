using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRPT.Core;
using VRPT.Data;

namespace VRPT.Presentation
{
    /**
     * CLASS: MachineControl
     * DESCRIPTION: Base class for the machine controls the user interacts with directly.
     * It holds what they all share: identity, reference validation and action reporting.
     * Derived classes only decide when an action happened and call Raise.
     *
     */
    public abstract class MachineControl : XRBaseInteractable, IMachineControl
    {
        /** Asset that identifies this control; must be assigned in the Inspector */
        [SerializeField] private ControlId controlId;

        /** Identity of this control, shared with the procedure steps */
        public ControlId ControlId => controlId;

        /** Raised once for each physical action performed on this control */
        public event Action<ControlId, ControlAction> ActionPerformed;

        /** Validates the setup; a control without identity could never be evaluated */
        protected override void Awake()
        {
            // XRI collects the colliders here: without this call the control cannot be selected
            base.Awake();

            if (controlId == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(controlId)}", this);
                enabled = false;
            }
        }

        /** Reports an action to every listener; to be called by derived classes */
        protected void Raise(ControlAction action)
        {
            
            Debug.Log($"Control {controlId.Id} -> {action}", this);
            ActionPerformed?.Invoke(controlId, action);
        }
    }
}

