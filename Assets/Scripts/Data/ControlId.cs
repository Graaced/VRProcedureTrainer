using UnityEngine;

namespace VRPT.Data
{
    /**
     * CLASS: ControlId
     * DESCRIPTION: Identifies a machine control through a shared asset.
     * Scene controls and procedure steps reference the same asset, so they stay linked
     * without hand-typed strings and procedures can be edited from the Inspector.
     *
     */
    [CreateAssetMenu(fileName = "ControlId", menuName = "VRPT/Control Id")]
    public sealed class ControlId : ScriptableObject
    {
        /** Stable identifier used by the logic and the report, e.g. "stop" */
        [SerializeField] private string id;

        /** Name shown to the user in panel messages, e.g. "STOP button" */
        [SerializeField] private string displayName;

        /** Read-only identifier: asset data must not change at runtime */
        public string Id => id;

        /** Read-only display name */
        public string DisplayName => displayName;
    }
}