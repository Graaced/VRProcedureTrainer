using System;
using System.Collections.Generic;
using UnityEngine;
using VRPT.Data;

namespace VRPT.Presentation
{
    /**
     * CLASS: HighlightService
     * DESCRIPTION: Makes the control the user must operate next pulse with light.
     * It uses a MaterialPropertyBlock, so no material gets copied, and the parts to light
     * are listed in the Inspector, so a new control needs no code.
     *
     */
    public sealed class HighlightService : MonoBehaviour
    {
        /**
         * CLASS: Target
         * DESCRIPTION: The parts that light up for one control.
         * They are listed by hand because the part to see is not always on the control:
         * the padlock is what the user must find, not its empty socket.
         *
         */
        [Serializable]
        private sealed class Target
        {
            /** Control these parts belong to */
            public ControlId control;

            /** Parts to light up; only URP Lit slots with emission enabled react */
            public Renderer[] renderers;
        }

        /** Shader property of the URP Lit emission color; a constant, not runtime state */
        private static readonly int emissionColorId = Shader.PropertyToID("_EmissionColor");

        /** Parts to light up for each control */
        [SerializeField] private List<Target> targets = new List<Target>();

        /** Emission color of the pulse; HDR so it can exceed normal brightness */
        [ColorUsage(false, true)]
        [SerializeField] private Color highlightColor = new Color(1f, 0.7f, 0.1f);

        /** Brightness at the darkest point of the pulse */
        [Min(0f)]
        [SerializeField] private float minIntensity = 0.1f;

        /** Brightness at the brightest point of the pulse */
        [Min(0f)]
        [SerializeField] private float maxIntensity = 1.5f;

        /** Speed of the pulse, in radians per second */
        [Min(0.1f)]
        [SerializeField] private float pulseSpeed = 4f;

        /** Parts of each control, by control id */
        private Dictionary<string, Renderer[]> renderersByControlId;

        /** Per-renderer overrides applied on top of the shared materials */
        private MaterialPropertyBlock propertyBlock;

        /** Parts pulsing now; null when nothing is highlighted */
        private Renderer[] activeRenderers;

        /** Validates the setup and indexes the parts by control */
        private void Awake()
        {
            renderersByControlId = new Dictionary<string, Renderer[]>();
            propertyBlock = new MaterialPropertyBlock();

            foreach (Target target in targets)
            {
                if (!IsValid(target))
                {
                    Debug.LogError($"[{name}] A highlight target has a missing control or renderer", this);
                    enabled = false;
                    return;
                }

                renderersByControlId[target.control.Id] = target.renderers;
            }
        }

        /** Makes a control pulse, in place of the one highlighted before */
        public void Highlight(string controlId)
        {
            Clear();

            if (!renderersByControlId.TryGetValue(controlId, out activeRenderers))
                Debug.LogWarning($"[{name}] No highlight target for control '{controlId}'", this);
        }

        /** Turns off the current highlight */
        public void Clear()
        {
            if (activeRenderers == null)
                return;

            SetEmission(Color.black);
            activeRenderers = null;
        }

        /** Advances the pulse */
        private void Update()
        {
            if (activeRenderers == null)
                return;

            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            SetEmission(highlightColor * Mathf.Lerp(minIntensity, maxIntensity, wave));
        }

        /** Leaves no part lit when the component stops */
        private void OnDisable()
        {
            Clear();
        }

        /** Applies an emission color to the active parts */
        private void SetEmission(Color color)
        {
            propertyBlock.SetColor(emissionColorId, color);

            foreach (Renderer activeRenderer in activeRenderers)
                activeRenderer.SetPropertyBlock(propertyBlock);
        }

        /** True if the target has a control and every renderer slot is filled */
        private static bool IsValid(Target target)
        {
            if (target.control == null || target.renderers == null || target.renderers.Length == 0)
                return false;

            foreach (Renderer targetRenderer in target.renderers)
            {
                if (targetRenderer == null)
                    return false;
            }

            return true;
        }
    }
}
