using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: RotarySwitch
     * DESCRIPTION: Isolator switch: first position is ON, second is OFF.
     * While the padlock is inserted it cannot be moved, which is the point of the lockout.
     *
     */
    public sealed class RotarySwitch : RotaryControl
    {
        /** True while the padlock prevents the switch from moving */
        private bool isLocked;

        /** A locked switch cannot change position */
        protected override bool CanChangePosition => !isLocked;

        /** Blocks or frees the switch; called when the padlock is inserted or removed */
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        /** A locked switch refuses every new select */
        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            return !isLocked && base.IsSelectableBy(interactor);
        }

        /** Reports the new position of the switch */
        protected override void ReportPosition(bool atSecondPosition)
        {
            Raise(atSecondPosition ? ControlAction.SwitchedOff : ControlAction.SwitchedOn);
        }
    }
}
