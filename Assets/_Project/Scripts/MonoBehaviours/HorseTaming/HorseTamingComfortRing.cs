using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Draws a flat XZ circle around the horse for the comfort zone (green ring).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class HorseTamingComfortRing : MonoBehaviour
    {
        [SerializeField] private float radius = 4f;
        [SerializeField] private int segments = 64;
        [SerializeField] private float yOffset = 0.04f;
        [SerializeField] private Color color = new(0.35f, 0.85f, 0.4f, 0.95f);

        private LineRenderer _lr;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.loop = true;
            _lr.useWorldSpace = false;
            _lr.widthMultiplier = 0.08f;
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _lr.material = new Material(shader);
            if (_lr.material.HasProperty("_Color"))
                _lr.material.SetColor("_Color", color);
            _lr.startColor = color;
            _lr.endColor = color;
            Rebuild();
        }

        public void SetRadius(float r)
        {
            radius = Mathf.Max(0.1f, r);
            Rebuild();
        }

        private void Rebuild()
        {
            if (_lr == null)
                _lr = GetComponent<LineRenderer>();

            _lr.positionCount = segments;
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * step;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                _lr.SetPosition(i, new Vector3(x, yOffset, z));
            }
        }
    }
}
