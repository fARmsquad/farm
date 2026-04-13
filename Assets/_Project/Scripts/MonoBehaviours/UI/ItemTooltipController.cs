using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// A small UGUI panel that follows the mouse and displays item details on hover.
    /// Attached to a TooltipPanel child of InventoryCanvas.
    /// </summary>
    public sealed class ItemTooltipController : MonoBehaviour
    {
        private const float OffsetX = 16f;
        private const float OffsetY = 16f;
        private const float ScreenEdgePadding = 8f;

        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text valueText;

        private RectTransform _canvasRect;
        private Canvas _parentCanvas;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas != null)
                _canvasRect = _parentCanvas.GetComponent<RectTransform>();

            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>
        /// Populates and shows the tooltip panel with item data.
        /// </summary>
        public void Show(ItemData data, Sprite icon, Vector2 screenPos)
        {
            if (panel == null || data == null)
                return;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (descriptionText != null)
                descriptionText.text = data.Description;

            if (valueText != null)
            {
                valueText.text = data.SellValue > 0 ? $"Sell: {data.SellValue}g" : string.Empty;
                valueText.enabled = data.SellValue > 0;
            }

            PositionAtScreen(screenPos);
            panel.SetActive(true);
        }

        /// <summary>
        /// Hides the tooltip panel.
        /// </summary>
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void PositionAtScreen(Vector2 screenPos)
        {
            if (_canvasRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos + new Vector2(OffsetX, -OffsetY),
                _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera,
                out var localPoint);

            var panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null)
                return;

            // Clamp to canvas bounds
            var canvasSize = _canvasRect.rect.size;
            var panelSize = panelRect.rect.size;
            float halfCanvasW = canvasSize.x * 0.5f;
            float halfCanvasH = canvasSize.y * 0.5f;

            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvasW + ScreenEdgePadding,
                halfCanvasW - panelSize.x - ScreenEdgePadding);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvasH + panelSize.y + ScreenEdgePadding,
                halfCanvasH - ScreenEdgePadding);

            panelRect.anchoredPosition = localPoint;
        }
    }
}
