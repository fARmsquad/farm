using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    [RequireComponent(typeof(Renderer))]
    public class CropVisualUpdater : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private CropPlotController _controller;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _baseLocalPosition;

        private static readonly Color PlantedColor  = new(0.48f, 0.66f, 0.24f);
        private static readonly Color SeedlingColor = new(0.28f, 0.78f, 0.24f);
        private static readonly Color MatureColor   = new(0.92f, 0.2f, 0.15f);

        private void Awake()
        {
            // Controller may live on this GameObject or a parent plot GameObject.
            _controller = GetComponentInParent<CropPlotController>();
            _renderer   = GetComponent<Renderer>();
            _propBlock  = new MaterialPropertyBlock();
            _baseLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (_controller?.State == null) return;

            var phase = _controller.State.Phase;
            float t = _controller.State.GrowthPercent;

            // Show immediately when planted so planting has visible feedback.
            bool visible = phase != FarmSimVR.Core.Farming.PlotPhase.Empty;
            if (_renderer.enabled != visible) _renderer.enabled = visible;
            if (!visible) return;

            float displayT = phase switch
            {
                FarmSimVR.Core.Farming.PlotPhase.Planted => 0.08f,
                FarmSimVR.Core.Farming.PlotPhase.Growing => Mathf.Lerp(0.18f, 0.85f, Mathf.Clamp01(t)),
                FarmSimVR.Core.Farming.PlotPhase.Ready => 1f,
                _ => Mathf.Clamp01(t)
            };

            float scaleY = Mathf.Lerp(0.12f, 1.0f, displayT);
            float width = Mathf.Lerp(0.18f, 0.8f, displayT);
            transform.localScale    = new Vector3(width, scaleY, width);
            transform.localPosition = new Vector3(
                _baseLocalPosition.x,
                _baseLocalPosition.y + scaleY * 0.5f,
                _baseLocalPosition.z);

            var fromColor = phase == FarmSimVR.Core.Farming.PlotPhase.Planted
                ? PlantedColor
                : SeedlingColor;

            _renderer.GetPropertyBlock(_propBlock);
            var color = Color.Lerp(fromColor, MatureColor, Mathf.Clamp01(t));
            _propBlock.SetColor(ColorId, color);
            _propBlock.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
