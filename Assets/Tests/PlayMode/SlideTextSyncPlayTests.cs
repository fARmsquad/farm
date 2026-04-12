using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Tests.PlayMode
{
    [TestFixture]
    public class SlideTextSyncPlayTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator LateUpdate_HideWhenFadesInActive_HidesTextEvenIfSlideActive()
        {
            _root = new GameObject("SlideTextSyncRoot");
            var sync = _root.AddComponent<SlideTextSync>();

            var slide = new GameObject("Slide");
            slide.transform.SetParent(_root.transform, false);
            slide.SetActive(true);

            var text = new GameObject("OverlayText");
            text.transform.SetParent(_root.transform, false);
            text.SetActive(true);

            var hideGo = new GameObject("Blackout");
            hideGo.transform.SetParent(_root.transform, false);
            var hideGroup = hideGo.AddComponent<CanvasGroup>();
            hideGroup.alpha = 1f;

            var field = typeof(SlideTextSync).GetField("pairs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(sync, new[]
            {
                new SlideTextSync.SlideTextPair
                {
                    slide = slide,
                    text = text,
                    hideWhenThisFadesIn = hideGroup
                }
            });

            yield return null;

            Assert.IsFalse(text.activeSelf);
        }

        [UnityTest]
        public IEnumerator LateUpdate_HideWhenFadesInAtZero_TextFollowsSlide()
        {
            _root = new GameObject("SlideTextSyncRoot");
            var sync = _root.AddComponent<SlideTextSync>();

            var slide = new GameObject("Slide");
            slide.transform.SetParent(_root.transform, false);
            slide.SetActive(true);

            var text = new GameObject("OverlayText");
            text.transform.SetParent(_root.transform, false);
            text.SetActive(false);

            var hideGo = new GameObject("Blackout");
            hideGo.transform.SetParent(_root.transform, false);
            var hideGroup = hideGo.AddComponent<CanvasGroup>();
            hideGroup.alpha = 0f;

            var field = typeof(SlideTextSync).GetField("pairs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(sync, new[]
            {
                new SlideTextSync.SlideTextPair
                {
                    slide = slide,
                    text = text,
                    hideWhenThisFadesIn = hideGroup
                }
            });

            yield return null;

            Assert.IsTrue(text.activeSelf);
        }
    }
}
