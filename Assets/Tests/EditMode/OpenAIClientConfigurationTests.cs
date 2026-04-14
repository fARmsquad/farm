using System;
using System.IO;
using System.Reflection;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class OpenAIClientConfigurationTests
    {
        private const string EnvironmentVariableName = "OPENAI_API_KEY";

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
        public void ResolveApiKey_PrefersEnvironmentVariableOverSerializedOverride()
        {
            string original = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            Environment.SetEnvironmentVariable(EnvironmentVariableName, "env-key");

            try
            {
                var gameObject = new GameObject("OpenAIClient");
                var client = gameObject.AddComponent<OpenAIClient>();
                SetSerializedString(client, "apiKey", "stale-key");

                string resolved = InvokeResolveApiKey(client);

                Assert.That(resolved, Is.EqualTo("env-key"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
            }
        }

        [Test]
        public void ResolveApiKey_FallsBackToGauntletEnvFileWhenEnvironmentMissing()
        {
            string original = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            Environment.SetEnvironmentVariable(EnvironmentVariableName, null);

            string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                string envPath = Path.Combine(tempRoot, ".env.local");
                File.WriteAllText(envPath, "OPENAI_API_KEY=gauntlet-key\n", System.Text.Encoding.UTF8);

                var gameObject = new GameObject("OpenAIClient");
                var client = gameObject.AddComponent<OpenAIClient>();
                SetSerializedString(client, "apiKey", "stale-key");
                SetPrivateString(client, "_gauntletEnvSearchRootOverride", tempRoot);

                string resolved = InvokeResolveApiKey(client);

                Assert.That(resolved, Is.EqualTo("gauntlet-key"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void TownScene_OpenAIClient_DoesNotSerializeApiKey()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Town.unity", OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            var client = UnityEngine.Object.FindAnyObjectByType<OpenAIClient>();
            Assert.That(client, Is.Not.Null);

            var serialized = new SerializedObject(client);
            Assert.That(serialized.FindProperty("apiKey").stringValue, Is.Empty);
        }

        private static string InvokeResolveApiKey(OpenAIClient client)
        {
            MethodInfo method = typeof(OpenAIClient).GetMethod(
                "ResolveApiKey",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(client, null);
        }

        private static void SetSerializedString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateString(object target, string fieldName, string value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
