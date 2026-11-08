using UnityEngine;

namespace VRPT.Presentation
{
    /**
     * CLASS: IndicatorLamp
     * DESCRIPTION: Panel lamp that lights up by changing its emission color.
     * It uses a MaterialPropertyBlock, so each lamp changes only itself and Unity
     * does not create a copy of the material.
     *
     */
    [RequireComponent(typeof(Renderer))]
    public sealed class IndicatorLamp : MonoBehaviour
    {
        /** Shader property of the URP Lit emission color; a constant, not runtime state */
        private static readonly int emissionColorId = Shader.PropertyToID("_EmissionColor");

        /** Emission color when the lamp is on; HDR so it can exceed normal brightness */
        [ColorUsage(false, true)]
        [SerializeField] private Color litColor = Color.white;

        /** Renderer of the lamp lens */
        private Renderer lensRenderer;

        /** Per-renderer overrides applied on top of the shared material */
        private MaterialPropertyBlock propertyBlock;

        /** Prepares the renderer and the property block */
        private void Awake()
        {
            lensRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        /** Turns the lamp on or off */
        public void SetLit(bool lit)
        {
            lensRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(emissionColorId, lit ? litColor : Color.black);
            lensRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}

