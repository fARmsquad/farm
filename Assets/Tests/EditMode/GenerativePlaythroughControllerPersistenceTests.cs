using System.Reflection;
using FarmSimVR.MonoBehaviours.Cinematics;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class GenerativePlaythroughControllerPersistenceTests
    {
        private const string SessionIdPrefKey = "FarmSimVR.GenerativeRuntime.SessionId";
        private const string BaseUrlPrefKey = "FarmSimVR.GenerativeRuntime.BaseUrl";
        private const string JobIdPrefKey = "FarmSimVR.GenerativeRuntime.JobId";

        [SetUp]
        public void SetUp()
        {
            ClearPrefs();
            DestroyControllers();
        }

        [TearDown]
        public void TearDown()
        {
            ClearPrefs();
            DestroyControllers();
        }

        [Test]
        public void Controller_RehydratesPersistedSessionState_OnFreshInstance()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            SetPrivateField(controller, "_activeSessionId", "session-persisted");
            SetPrivateField(controller, "_activeBaseUrl", "https://story-orchestrator-production.up.railway.app");
            SetPrivateField(controller, "_activeJobId", "job-persisted");
            InvokePrivateMethod(controller, "PersistState");

            Object.DestroyImmediate(controller.gameObject);

            var rehydrated = GenerativePlaythroughController.GetOrCreate();

            Assert.That(rehydrated.ActiveSessionId, Is.EqualTo("session-persisted"));
            Assert.That(ReadPrivateField<string>(rehydrated, "_activeBaseUrl"), Is.EqualTo("https://story-orchestrator-production.up.railway.app"));
            Assert.That(ReadPrivateField<string>(rehydrated, "_activeJobId"), Is.EqualTo("job-persisted"));
        }

        [Test]
        public void ClearSequenceState_RemovesPersistedSessionState()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            SetPrivateField(controller, "_activeSessionId", "session-to-clear");
            SetPrivateField(controller, "_activeBaseUrl", "https://story-orchestrator-production.up.railway.app");
            SetPrivateField(controller, "_activeJobId", "job-to-clear");
            InvokePrivateMethod(controller, "PersistState");

            controller.ClearSequenceState();
            Object.DestroyImmediate(controller.gameObject);

            var rehydrated = GenerativePlaythroughController.GetOrCreate();

            Assert.That(rehydrated.ActiveSessionId, Is.Empty);
            Assert.That(ReadPrivateField<string>(rehydrated, "_activeBaseUrl"), Is.Empty);
            Assert.That(ReadPrivateField<string>(rehydrated, "_activeJobId"), Is.Empty);
            Assert.That(PlayerPrefs.HasKey(SessionIdPrefKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(BaseUrlPrefKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(JobIdPrefKey), Is.False);
        }

        [Test]
        public void Controller_ExposesPreparationEntryPoint_ForHistoricalReadyTurns()
        {
            var method = typeof(GenerativePlaythroughController).GetMethod(
                "PrepareExistingReadyTurn",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
        }

        private static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(SessionIdPrefKey);
            PlayerPrefs.DeleteKey(BaseUrlPrefKey);
            PlayerPrefs.DeleteKey(JobIdPrefKey);
            PlayerPrefs.Save();
        }

        private static void DestroyControllers()
        {
            foreach (var controller in Object.FindObjectsByType<GenerativePlaythroughController>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(instance, null);
        }

        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(instance);
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(instance, value);
        }
    }
}
