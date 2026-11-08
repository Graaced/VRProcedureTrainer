using UnityEngine;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: UnityClock
     * DESCRIPTION: Gives the training logic the real time of the running application.
     * It ignores Time.timeScale, so a paused or slowed game cannot alter the report timings.
     *
     */
    public sealed class UnityClock : IClock
    {
        /** Seconds elapsed since the application started */
        public double NowSeconds => Time.realtimeSinceStartupAsDouble;
    }
}
