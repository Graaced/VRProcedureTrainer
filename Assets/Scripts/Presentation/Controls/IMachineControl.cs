using System;
using VRPT.Core;
using VRPT.Data;

namespace VRPT.Presentation
{
    /**
     * CLASS: IMachineControl
     * DESCRIPTION: Common contract for everything the user can operate on the machine.
     * Buttons, switches and the lock socket have no shared base class, so this interface
     * lets the simulation and the procedure logic listen to all of them the same way.
     *
     */
    public interface IMachineControl
    {
        /** Identity of this control, shared with the procedure steps */
        ControlId ControlId { get; }

        /** Raised once for each physical action performed on this control */
        event Action<ControlId, ControlAction> ActionPerformed;
    }
}
