using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal sealed class GenerativePlaythroughMenuViewRefs
    {
        public Text StatusLabel;
        public Text DetailLabel;
        public Text HistoryEmptyLabel;
        public RectTransform HistoryContent;
        public Button CreateButton;
        public Button RefreshButton;
        public Button PlayButton;
        public Button BackButton;
        public readonly List<Image> StageTileImages = new();
        public readonly List<Text> StageTileLabels = new();
    }

    internal static class GenerativePlaythroughMenuViewBuilder
    {
        public static Canvas EnsureCanvas(string canvasName)
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            var canvasObject = new GameObject(canvasName);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem(string eventSystemName)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject(eventSystemName);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        public static GenerativePlaythroughMenuViewRefs Build(
            Transform parent,
            string rootName,
            Font font,
            Action onCreate,
            Action onRefresh,
            Action onPlay,
            Action onBack)
        {
            var view = new GenerativePlaythroughMenuViewRefs();
            var root = CreatePanel(parent, rootName, new Rect(0f, 0f, 0f, 0f), new Color(0.04f, 0.06f, 0.12f, 0.98f), stretchToParent: true);
            CreateLabel(font, "Header", root.transform, "GENERATIVE PLAYTHROUGHS", 30, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(48f, 36f, 720f, 40f), Color.white);
            CreateLabel(font, "SubHeader", root.transform, "Create new runs, monitor live pipeline progress, and replay earlier service-backed sessions.", 15, FontStyle.Normal, TextAnchor.UpperLeft, new Rect(48f, 80f, 980f, 28f), new Color(0.88f, 0.92f, 1f));

            BuildActionPanel(font, root.transform, view, onCreate, onRefresh, onPlay, onBack);
            BuildHistoryPanel(font, root.transform, view);
            BuildTrackerPanel(font, root.transform, view);
            BuildDetailPanel(font, root.transform, view);
            return view;
        }

        public static Text CreateLabel(
            Font font,
            string name,
            Transform parent,
            string text,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Rect rect,
            Color color,
            bool stretchToParent = false)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            if (stretchToParent)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
            else
            {
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = new Vector2(rect.x, -rect.y);
                labelRect.sizeDelta = new Vector2(rect.width, rect.height);
            }

            var label = labelObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static GameObject CreatePanel(
            Transform parent,
            string name,
            Rect rect,
            Color color,
            bool stretchToParent = false)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            if (stretchToParent)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }
            else
            {
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(rect.x, -rect.y);
                panelRect.sizeDelta = new Vector2(rect.width, rect.height);
            }

            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        public static RectTransform CreateContainer(Transform parent, string name, Rect rect)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.anchoredPosition = new Vector2(rect.x, -rect.y);
            containerRect.sizeDelta = new Vector2(rect.width, rect.height);
            return containerRect;
        }

        private static void BuildActionPanel(
            Font font,
            Transform parent,
            GenerativePlaythroughMenuViewRefs view,
            Action onCreate,
            Action onRefresh,
            Action onPlay,
            Action onBack)
        {
            var panel = CreatePanel(parent, "Actions", new Rect(48f, 132f, 1180f, 88f), new Color(0.09f, 0.12f, 0.2f, 0.94f));
            view.CreateButton = CreateActionButton(font, panel.transform, "CreateButton", "Create New Playthrough", new Vector2(20f, -20f), onCreate);
            view.RefreshButton = CreateActionButton(font, panel.transform, "RefreshButton", "Refresh", new Vector2(320f, -20f), onRefresh);
            view.PlayButton = CreateActionButton(font, panel.transform, "PlayButton", "Play Ready Playthrough", new Vector2(520f, -20f), onPlay);
            view.BackButton = CreateActionButton(font, panel.transform, "BackButton", "Back To Title", new Vector2(900f, -20f), onBack);
        }

        private static void BuildHistoryPanel(Font font, Transform parent, GenerativePlaythroughMenuViewRefs view)
        {
            var panel = CreatePanel(parent, "HistoryPanel", new Rect(48f, 252f, 360f, 708f), new Color(0.08f, 0.11f, 0.2f, 0.94f));
            CreateLabel(font, "HistoryHeader", panel.transform, "Playthrough Library", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(20f, 16f, 280f, 24f), Color.white);
            view.HistoryContent = CreateContainer(panel.transform, "HistoryContent", new Rect(20f, 56f, 320f, 610f));
            view.HistoryEmptyLabel = CreateLabel(font, "Empty", view.HistoryContent, "No generated playthroughs yet.", 14, FontStyle.Normal, TextAnchor.UpperLeft, new Rect(0f, 0f, 320f, 24f), new Color(0.82f, 0.88f, 1f));
        }

        private static void BuildTrackerPanel(Font font, Transform parent, GenerativePlaythroughMenuViewRefs view)
        {
            var panel = CreatePanel(parent, "TrackerPanel", new Rect(440f, 252f, 788f, 272f), new Color(0.08f, 0.11f, 0.2f, 0.94f));
            CreateLabel(font, "TrackerHeader", panel.transform, "Pipeline Tracker", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(20f, 16f, 220f, 24f), Color.white);

            var stages = new[] { "Queued", "Story", "Images", "Audio", "Package", "Validate", "Ready" };
            for (int index = 0; index < stages.Length; index++)
            {
                var tile = CreatePanel(panel.transform, $"Stage_{index + 1}", new Rect(20f + (index * 108f), 70f, 96f, 150f), new Color(0.2f, 0.24f, 0.3f, 0.92f));
                var label = CreateLabel(font, "Label", tile.transform, stages[index], 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Rect(8f, 20f, 80f, 110f), Color.white);
                view.StageTileImages.Add(tile.GetComponent<Image>());
                view.StageTileLabels.Add(label);
            }
        }

        private static void BuildDetailPanel(Font font, Transform parent, GenerativePlaythroughMenuViewRefs view)
        {
            var detailPanel = CreatePanel(parent, "DetailPanel", new Rect(440f, 552f, 788f, 408f), new Color(0.08f, 0.11f, 0.2f, 0.94f));
            CreateLabel(font, "StatusHeader", detailPanel.transform, "Dispatch Board", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(20f, 16f, 220f, 24f), Color.white);
            view.StatusLabel = CreateLabel(font, "Status", detailPanel.transform, GenerativePlaythroughMenuController.DefaultStatus, 15, FontStyle.Normal, TextAnchor.UpperLeft, new Rect(20f, 54f, 748f, 120f), new Color(0.96f, 0.97f, 1f));
            CreateLabel(font, "DetailHeader", detailPanel.transform, "Selected Playthrough", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(20f, 192f, 260f, 22f), Color.white);
            view.DetailLabel = CreateLabel(font, "Detail", detailPanel.transform, "No playthrough selected.", 15, FontStyle.Normal, TextAnchor.UpperLeft, new Rect(20f, 226f, 748f, 160f), new Color(0.9f, 0.94f, 1f));
        }

        private static Button CreateActionButton(
            Font font,
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Action onClick)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(240f, 44f);
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.11f, 0.27f, 0.67f, 0.98f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateLabel(font, "Label", buttonObject.transform, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Rect(0f, 0f, 0f, 0f), Color.white, stretchToParent: true);
            return button;
        }
    }
}
