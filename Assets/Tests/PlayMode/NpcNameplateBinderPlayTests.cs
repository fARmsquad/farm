using System.Collections;
using System.Reflection;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.PlayMode
{
    public sealed class NpcNameplateBinderPlayTests
    {
        private GameObject _npcGo;

        [TearDown]
        public void TearDown()
        {
            if (_npcGo != null)
                Object.Destroy(_npcGo);
        }

        [UnityTest]
        public IEnumerator Start_WithNpcRole_ShowsNameAndRoleOnLabels()
        {
            _npcGo = new GameObject("Npc");
            var npc = _npcGo.AddComponent<NPCController>();
            SetPrivateField(npc, "npcName", "Mira the Baker");
            SetPrivateField(npc, "npcRole", "The baker");

            var plate = NpcNameplateFactory.CreateNameplate(_npcGo.transform, Vector3.up);
            yield return null;

            var texts = plate.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(texts.Length, Is.EqualTo(2), "Expected name + role TMP.");
            Assert.That(texts[0].text, Is.EqualTo("Mira the Baker"));
            Assert.That(texts[1].text, Is.EqualTo("The baker"));
            Assert.That(texts[1].gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator Start_WithoutNpcRole_HidesRoleLabel()
        {
            _npcGo = new GameObject("Npc");
            var npc = _npcGo.AddComponent<NPCController>();
            SetPrivateField(npc, "npcName", "Someone");
            SetPrivateField(npc, "npcRole", string.Empty);

            var plate = NpcNameplateFactory.CreateNameplate(_npcGo.transform, Vector3.up);
            yield return null;

            var texts = plate.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(texts.Length, Is.EqualTo(2));
            Assert.That(texts[0].text, Is.EqualTo("Someone"));
            Assert.That(texts[1].gameObject.activeInHierarchy, Is.False);
        }

        private static void SetPrivateField(object target, string fieldName, string value)
        {
            var f = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"Missing field {fieldName}");
            f.SetValue(target, value);
        }
    }
}
