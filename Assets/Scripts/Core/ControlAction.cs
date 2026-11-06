namespace VRPT.Core 
{
    /**
     * CLASS: ControlAction
     * DESCRIPTION: Lists the physical actions a user can perform on a machine control.
     * The training logic evaluates these actions without depending on XR or Unity.
     * Saved in assets as numbers: add new values only at the end.
     *
     */
    public enum ControlAction
    {
        /** Button pressed; on the emergency stop it also means latched in the pressed position */
        Pressed,

        /** Emergency stop unlatched; this alone never restarts the machine, START is required */
        Released,

        /** Isolator switch turned to position 0 (OFF) */
        SwitchedOff,

        /** Isolator switch turned to position 1 (ON) */
        SwitchedOn,

        /** Padlock inserted into the isolator socket */
        Inserted,

        /** Padlock removed from the isolator socket */
        Removed,

        /** Guard opened past its threshold */
        Opened,

        /** Guard closed again */
        Closed
    }

}



