using System.Collections;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Cinematics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.PlayMode
{
    /// <summary>
    /// NPC proximity uses the Player tag or a TownPlayerController fallback; CoreScene rigs are often Untagged.
    /// </summary>
    [TestFixture]
    public class NPCControllerTownPlayerRangeTests
    {
        private GameObject _playerGo;
        private GameObject _npcGo;

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null)
                Object.Destroy(_playerGo);
            if (_npcGo != null)
                Object.Destroy(_npcGo);
        }

        [UnityTest]
        public IEnumerator IsPlayerInRange_WithUntaggedTownPlayer_WhenClose_ReturnsTrue()
        {
            _playerGo = new GameObject("TownPlayer");
            _playerGo.AddComponent<CharacterController>();
            _playerGo.AddComponent<TownPlayerController>();

            _npcGo = new GameObject("Npc");
            _npcGo.AddComponent<NPCController>();
            var npc = _npcGo.GetComponent<NPCController>();

            _playerGo.transform.position = Vector3.zero;
            _npcGo.transform.position = new Vector3(3f, 0f, 0f);

            yield return null; // Start() resolves player via TownPlayerController fallback

            Assert.IsTrue(npc.IsPlayerInRange, "NPC should detect Untagged TownPlayerController within default interaction range.");
        }

        [UnityTest]
        public IEnumerator IsPlayerInRange_WithUntaggedTownPlayer_WhenFar_ReturnsFalse()
        {
            _playerGo = new GameObject("TownPlayer");
            _playerGo.AddComponent<CharacterController>();
            _playerGo.AddComponent<TownPlayerController>();

            _npcGo = new GameObject("Npc");
            _npcGo.AddComponent<NPCController>();
            var npc = _npcGo.GetComponent<NPCController>();

            _playerGo.transform.position = Vector3.zero;
            _npcGo.transform.position = new Vector3(20f, 0f, 0f);

            yield return null;

            Assert.IsFalse(npc.IsPlayerInRange);
        }
    }
}
