using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    [RequireComponent(typeof(CropPlotController))]
    public class CropVisualUpdater : MonoBehaviour
    {
        private CropPlotController _controller;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private static readonly Color SeedlingColor = new Color(0.2f, 0.8f, 0.2f);
        private static readonly Color MatureColor = new Color(0.9f, 0.1f, 0.1f);

        private void Awake()
        {
            _controller = GetComponent<CropPlotController>();
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_controller.State == null) return;

            float t = _controller.State.GrowthPercent;

            // Scale Y from 0.1 to 1.0
            float scaleY = Mathf.Lerp(0.1f, 1.0f, t);
            transform.localScale = new Vector3(0.8f, scaleY, 0.8f);

            // Offset Y so cube grows upward from ground
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                scaleY * 0.5f,
                transform.localPosition.z);

            // Color lerp green -> red
            Color color = Color.Lerp(SeedlingColor, MatureColor, t);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
