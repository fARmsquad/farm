using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TitleScreenSceneConfigurationTests
    {
        private const string TitleScenePath = "Assets/_Project/Scenes/TitleScreen.unity";

        [Test]
        public void TitleScreen_HasStartMyStorySiblingButton_WiredToStartGameStorySlice()
        {
            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

            var btnGo = GameObject.Find("StartMyStoryButton");
            Assert.IsNotNull(btnGo, "StartMyStoryButton GameObject is missing from TitleScreen.unity");

            var rect = btnGo.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(360f, 80f), rect.sizeDelta,
                "StartMyStoryButton must have sizeDelta (360, 80) to match StartGameButton.");
            Assert.AreEqual(new Vector2(200f, 80f), rect.anchoredPosition,
                "StartMyStoryButton must sit at anchoredPosition (200, 80) — right of bottom-center.");

            var image = btnGo.GetComponent<Image>();
            Assert.AreEqual(new Color(0.13f, 0.55f, 0.13f), image.color,
                "StartMyStoryButton must use the dark-green palette of StartGameButton.");

            var label = btnGo.GetComponentInChildren<Text>();
            Assert.IsNotNull(label, "StartMyStoryButton must have a child Text label.");
            Assert.AreEqual("START MY STORY", label.text);
            Assert.AreEqual(36, label.fontSize);

            var button = btnGo.GetComponent<Button>();
            var serialized = new SerializedObject(button);
            var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            Assert.AreEqual(1, calls.arraySize,
                "StartMyStoryButton must have exactly one persistent OnClick listener.");
            var methodName = calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_MethodName");
            Assert.AreEqual("StartGameStorySlice", methodName.stringValue,
                "StartMyStoryButton must target TitleScreenManager.StartGameStorySlice.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        [Test]
        public void TitleScreen_StartGameButton_ShiftedLeftToAccommodateSibling()
        {
            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

            var btnGo = GameObject.Find("StartGameButton");
            Assert.IsNotNull(btnGo, "StartGameButton GameObject is missing from TitleScreen.unity");
            var rect = btnGo.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(-200f, 80f), rect.anchoredPosition,
                "StartGameButton must be shifted to anchoredPosition (-200, 80) to leave room for the sibling.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
