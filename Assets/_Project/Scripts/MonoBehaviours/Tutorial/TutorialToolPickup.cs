using FarmSimVR.Core.Tutorial;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialToolPickup : MonoBehaviour
    {
        [SerializeField] private TutorialToolId toolId;
        [SerializeField] private string label;

        private Vector3 _basePosition;

        public TutorialToolId ToolId => toolId;
        public string Label => label;

        public void Initialize(TutorialToolId tutorialToolId, string displayLabel, Color color)
        {
            toolId = tutorialToolId;
            label = displayLabel;
            _basePosition = transform.position;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            CreateLabel(displayLabel);
        }

        private void Awake()
        {
            _basePosition = transform.position;
        }

        private void Update()
        {
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * 2f + transform.position.x) * 0.06f);
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        }

        private void CreateLabel(string displayLabel)
        {
            if (transform.Find("Label") != null)
                return;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var text = labelObject.AddComponent<TextMesh>();
            text.text = displayLabel;
            text.characterSize = 0.15f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
        }
    }
}
