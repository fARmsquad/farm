using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    internal static class TownDialogueHudLayout
    {
        private const float DefaultPanelHeight = 180f;
        private const float MaxPanelHeight = 320f;
        private const float MinimumDialogueHeight = 64f;
        private const float MinimumChoiceHeight = 56f;
        private const float ChoiceGap = 10f;
        private const float PanelPadding = 48f;

        public static void ConfigureStatusText(TextMeshProUGUI loadingText)
        {
            if (loadingText == null)
                return;

            RectTransform rect = loadingText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 12f);
            rect.sizeDelta = new Vector2(420f, 28f);
            loadingText.fontSize = 16f;
            loadingText.alignment = TextAlignmentOptions.MidlineLeft;
            loadingText.enableWordWrapping = true;
            loadingText.overflowMode = TextOverflowModes.Overflow;
            loadingText.gameObject.SetActive(false);
        }

        public static void ConfigureHintText(TextMeshProUGUI hintText)
        {
            if (hintText == null)
                return;

            hintText.fontSize = 16f;
            hintText.alignment = TextAlignmentOptions.MidlineRight;
            hintText.enableWordWrapping = true;
            hintText.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ConfigureChoiceContainer(Transform choiceContainer)
        {
            if (choiceContainer == null)
                return;

            var layout = choiceContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = choiceContainer.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = choiceContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = choiceContainer.gameObject.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void ConfigureChoiceButton(GameObject buttonObject, string label)
        {
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, MinimumChoiceHeight);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.26f, 0.92f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.45f, 1f);
            colors.pressedColor = new Color(0.08f, 0.12f, 0.20f, 1f);
            button.colors = colors;

            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = MinimumChoiceHeight;
            layout.preferredHeight = 64f;

            var fitter = buttonObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildButtonLabel(buttonObject.transform, label);
        }

        public static void RefreshLayout(
            RectTransform panelRect,
            RectTransform choiceRect,
            TextMeshProUGUI speakerNameText,
            TextMeshProUGUI dialogueText,
            TextMeshProUGUI loadingText,
            TextMeshProUGUI hintText)
        {
            if (panelRect == null)
                return;

            float panelWidth = GetPanelWidth(panelRect);
            float speakerHeight = GetPreferredTextHeight(speakerNameText, panelWidth, 24f);
            float dialogueHeight = GetPreferredTextHeight(dialogueText, panelWidth, MinimumDialogueHeight);
            float footerHeight = GetFooterHeight(panelWidth, loadingText, hintText);
            float targetHeight = Mathf.Clamp(
                speakerHeight + dialogueHeight + footerHeight + 44f,
                DefaultPanelHeight,
                MaxPanelHeight);

            Vector2 panelSize = panelRect.sizeDelta;
            panelRect.sizeDelta = new Vector2(panelSize.x, targetHeight);

            if (choiceRect != null)
            {
                Vector2 choicePosition = choiceRect.anchoredPosition;
                choicePosition.y = panelRect.anchoredPosition.y + targetHeight + ChoiceGap;
                choiceRect.anchoredPosition = choicePosition;
                LayoutRebuilder.ForceRebuildLayoutImmediate(choiceRect);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        private static void BuildButtonLabel(Transform parent, string label)
        {
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(parent, false);

            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 10f);
            textRect.offsetMax = new Vector2(-18f, -10f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        private static float GetPanelWidth(RectTransform panelRect)
        {
            float width = panelRect.rect.width;
            return width > 0f ? width : 560f;
        }

        private static float GetFooterHeight(
            float panelWidth,
            TextMeshProUGUI loadingText,
            TextMeshProUGUI hintText)
        {
            float footerWidth = Mathf.Max(200f, (panelWidth - PanelPadding) * 0.5f);
            float statusHeight = GetPreferredTextHeight(loadingText, footerWidth, 22f);
            float hintHeight = GetPreferredTextHeight(hintText, footerWidth, 22f);
            return Mathf.Max(statusHeight, hintHeight);
        }

        private static float GetPreferredTextHeight(
            TextMeshProUGUI text,
            float width,
            float minimumHeight)
        {
            if (text == null || !text.gameObject.activeSelf || string.IsNullOrWhiteSpace(text.text))
                return 0f;

            float preferredHeight = text.GetPreferredValues(text.text, width, 0f).y;
            return Mathf.Max(minimumHeight, preferredHeight);
        }
    }
}
