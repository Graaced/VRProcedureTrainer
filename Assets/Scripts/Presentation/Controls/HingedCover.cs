using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: HingedCover
     * DESCRIPTION: Hinged protective guard: first position is closed, second is open.
     * Opening it is the last step of the lockout, so it counts as open past a threshold
     * set on the component, without requiring the user to swing it all the way.
     *
     */
    public sealed class HingedCover : RotaryControl
    {
        /** Reports the new position of the guard */
        protected override void ReportPosition(bool atSecondPosition)
        {
            Raise(atSecondPosition ? ControlAction.Opened : ControlAction.Closed);
        }
    }
}

