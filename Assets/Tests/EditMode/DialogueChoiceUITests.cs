using System.Reflection;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class DialogueChoiceUITests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void StreamChunkDisplaysDirectDeltaText()
        {
            var ui = CreateUi(out var dialogue);

            InvokePrivateInstance(ui, "HandleStreamStarted", "Old Garrett");
            InvokePrivateInstance(ui, "HandleStreamChunk", "Howdy");
            Assert.That(dialogue.text, Is.EqualTo("Howdy"));

            InvokePrivateInstance(ui, "HandleStreamChunk", " traveler.");
            Assert.That(dialogue.text, Is.EqualTo("Howdy traveler."));
        }

        [Test]
        public void ResponseShowsLastQuestionAboveNpcReply()
        {
            var ui = CreateUi(out var dialogue);
            SetPrivateField(ui, "_lastPlayerPrompt", "Can you share the recipe?");

            InvokePrivateInstance(ui, "HandleStreamStarted", "Old Garrett");
            InvokePrivateInstance(ui, "HandleStreamChunk", "Oh, bless my soul!");

            Assert.That(
                dialogue.text,
                Is.EqualTo("You: Can you share the recipe?\n\nOh, bless my soul!"));

            InvokePrivateInstance(ui, "HandleResponse", "Old Garrett", "Oh, bless my soul!");

            Assert.That(
                dialogue.text,
                Is.EqualTo("You: Can you share the recipe?\n\nOh, bless my soul!"));
        }

        [Test]
        public void PlayerPromptSubmittedShowsPromptAboveNpcReply()
        {
            var ui = CreateUi(out var dialogue);

            InvokePrivateInstance(ui, "HandlePlayerPromptSubmitted", "What do you grow out here?");
            InvokePrivateInstance(ui, "HandleResponse", "Old Garrett", "Tomatoes, mostly.");

            Assert.That(
                dialogue.text,
                Is.EqualTo("You: What do you grow out here?\n\nTomatoes, mostly."));
        }

        [Test]
        public void HandleOptions_LongLabel_CreatesWrappedLayoutDrivenButton()
        {
            var ui = CreateUi(out _);
            Transform choiceContainer = (Transform)GetPrivateField(ui, "choiceContainer");

            InvokePrivateInstance(
                ui,
                "HandleOptions",
                new[]
                {
                    "Tell me the full story about the harvest festival and why everybody in town keeps bringing it up."
                });

            Assert.That(choiceContainer.childCount, Is.EqualTo(1));

            GameObject button = choiceContainer.GetChild(0).gameObject;
            Assert.That(button.GetComponent<LayoutElement>(), Is.Not.Null);
            Assert.That(button.GetComponent<ContentSizeFitter>(), Is.Not.Null);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.enableWordWrapping, Is.True);
        }

        [Test]
        public void RefreshVoiceInputStatus_IdleVoiceHint_UsesHintLabelWithoutShowingStatusBadge()
        {
            var ui = CreateUi(out _, out var loading, out var hint);
            var conversationRoot = new GameObject("Conversation");
            var controller = conversationRoot.AddComponent<TownVoiceInputController>();

            SetPrivateField(ui, "_voiceInputController", controller);
            SetPrivateField(ui, "_choicesVisible", true);
            SetAutoProperty(controller, nameof(TownVoiceInputController.CurrentStatus), "Hold V to speak");
            SetEnumAutoProperty(controller, "CurrentStatusPhase", "Idle");

            InvokePrivateInstance(ui, "RefreshVoiceInputStatus");

            Assert.That(hint.text, Does.Contain("V"));
            Assert.That(hint.text, Does.Contain("reply"));
            Assert.That(loading.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void HandleVoiceInputStatusChanged_RecordingStatus_ShowsReleaseHintAlongsideBadge()
        {
            var ui = CreateUi(out _, out var loading, out var hint);
            var conversationRoot = new GameObject("Conversation");
            var controller = conversationRoot.AddComponent<TownVoiceInputController>();

            SetPrivateField(ui, "_voiceInputController", controller);
            SetPrivateField(ui, "_choicesVisible", true);
            SetAutoProperty(controller, nameof(TownVoiceInputController.CurrentStatus), "Listening...");
            SetEnumAutoProperty(controller, "CurrentStatusPhase", "Recording");

            InvokePrivateInstance(ui, "HandleVoiceInputStatusChanged", "Listening...");

            Assert.That(loading.gameObject.activeSelf, Is.True);
            Assert.That(loading.text, Is.EqualTo("Listening..."));
            Assert.That(hint.text, Does.Contain("Release"));
            Assert.That(hint.text, Does.Contain("V"));
        }

        [Test]
        public void HandleResponse_LongContent_GrowsPanelAndMovesChoiceStack()
        {
            var ui = CreateUi(out _);
            var panel = (GameObject)GetPrivateField(ui, "dialoguePanel");
            var choices = (RectTransform)GetPrivateField(ui, "choiceContainer");

            InvokePrivateInstance(ui, "HandlePlayerPromptSubmitted", "Can you tell me every detail about the market day and what I should do before the sun goes down?");
            InvokePrivateInstance(
                ui,
                "HandleResponse",
                "Old Garrett",
                "The market's lively from first light to dusk, and if you want the good apples, the warm bread, and the freshest gossip, you'll need to make your rounds before noon and keep an eye on who's chatting near the fountain.");
            InvokePrivateInstance(ui, "HandleOptions", new[] { "Where should I go first?" });

            Assert.That(panel.GetComponent<RectTransform>().sizeDelta.y, Is.GreaterThan(180f));
            Assert.That(choices.anchoredPosition.y, Is.GreaterThan(220f));
        }

        private static DialogueChoiceUI CreateUi(out TextMeshProUGUI dialogue)
        {
            return CreateUi(out dialogue, out _, out _);
        }

        private static DialogueChoiceUI CreateUi(
            out TextMeshProUGUI dialogue,
            out TextMeshProUGUI loading,
            out TextMeshProUGUI hint)
        {
            var root = new GameObject("DialogueChoiceUITest");
            var ui = root.AddComponent<DialogueChoiceUI>();

            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0f);
            panelRect.anchorMax = new Vector2(0.9f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 30f);
            panelRect.sizeDelta = new Vector2(0f, 180f);

            var speaker = new GameObject("Speaker").AddComponent<TextMeshProUGUI>();
            speaker.transform.SetParent(panel.transform, false);

            dialogue = new GameObject("Dialogue").AddComponent<TextMeshProUGUI>();
            dialogue.transform.SetParent(panel.transform, false);

            var choices = new GameObject("Choices").AddComponent<RectTransform>();
            choices.SetParent(root.transform, false);
            choices.anchorMin = new Vector2(0.1f, 0f);
            choices.anchorMax = new Vector2(0.9f, 0f);
            choices.pivot = new Vector2(0.5f, 0f);
            choices.anchoredPosition = new Vector2(0f, 220f);
            choices.sizeDelta = new Vector2(0f, 220f);

            loading = new GameObject("Loading").AddComponent<TextMeshProUGUI>();
            loading.transform.SetParent(panel.transform, false);

            hint = new GameObject("Hint").AddComponent<TextMeshProUGUI>();
            hint.transform.SetParent(panel.transform, false);

            SetPrivateField(ui, "dialogueCanvas", canvas);
            SetPrivateField(ui, "dialoguePanel", panel);
            SetPrivateField(ui, "speakerNameText", speaker);
            SetPrivateField(ui, "dialogueText", dialogue);
            SetPrivateField(ui, "choiceContainer", choices);
            SetPrivateField(ui, "loadingText", loading);
            SetPrivateField(ui, "hintText", hint);
            return ui;
        }

        private static void InvokePrivateInstance(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return field.GetValue(target);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var backingField = target.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(backingField, Is.Not.Null, $"Missing backing field for property '{propertyName}'.");
            backingField.SetValue(target, value);
        }

        private static void SetEnumAutoProperty(object target, string propertyName, string enumValue)
        {
            var backingField = target.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(backingField, Is.Not.Null, $"Missing backing field for property '{propertyName}'.");
            object parsedValue = System.Enum.Parse(backingField.FieldType, enumValue);
            backingField.SetValue(target, parsedValue);
        }
    }
}
