using FarmSimVR.Core.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    [RequireComponent(typeof(Renderer))]
    public sealed class PlotVisualUpdater : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color EmptySoilColor = new(0.42f, 0.3f, 0.16f);
        private static readonly Color PlantedSoilColor = new(0.33f, 0.24f, 0.12f);
        private static readonly Color GrowingSoilColor = new(0.30f, 0.33f, 0.15f);
        private static readonly Color HarvestableSoilColor = new(0.48f, 0.38f, 0.17f);
        private static readonly Color DepletedSoilColor = new(0.38f, 0.35f, 0.31f);

        private CropPlotController _controller;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _controller = GetComponent<CropPlotController>();
            if (_controller == null)
                _controller = GetComponentInParent<CropPlotController>();
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (_controller == null)
                _controller = GetComponentInParent<CropPlotController>();

            if (_controller == null || _renderer == null)
                return;

            var targetColor = ResolveSoilColor(
                _controller.SoilState,
                _controller.State?.Phase ?? PlotPhase.Empty);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorId, targetColor);
            _propBlock.SetColor(BaseColorId, targetColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        internal static Color ResolveSoilColor(SoilState soil, PlotPhase phase)
        {
            if (soil == null)
                return ColorForPhase(phase);

            var baseColor = soil.Status switch
            {
                PlotStatus.Planted => PlantedSoilColor,
                PlotStatus.Growing => GrowingSoilColor,
                PlotStatus.Harvestable => HarvestableSoilColor,
                PlotStatus.Depleted => DepletedSoilColor,
                _ => EmptySoilColor
            };

            // Wetter soil reads darker; richer soil gets a subtle healthier tint.
            var wetColor = Color.Lerp(baseColor, baseColor * 0.72f, Mathf.Clamp01(soil.Moisture) * 0.55f);
            float nutrientLift = Mathf.Lerp(-0.02f, 0.08f, Mathf.Clamp01(soil.Nutrients));

            return new Color(
                Mathf.Clamp01(wetColor.r + nutrientLift * 0.2f),
                Mathf.Clamp01(wetColor.g + nutrientLift),
                Mathf.Clamp01(wetColor.b + nutrientLift * 0.1f),
                1f);
        }

        private static Color ColorForPhase(PlotPhase phase)
        {
            return phase switch
            {
                PlotPhase.Planted    => PlantedSoilColor,
                PlotPhase.Sprout     => GrowingSoilColor,
                PlotPhase.YoungPlant => GrowingSoilColor,
                PlotPhase.Budding    => GrowingSoilColor,
                PlotPhase.Fruiting   => GrowingSoilColor,
                PlotPhase.Ready      => HarvestableSoilColor,
                PlotPhase.Wilting    => PlantedSoilColor,
                PlotPhase.Dead       => DepletedSoilColor,
                _                    => EmptySoilColor
            };
        }
    }
}
