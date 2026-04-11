using System;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Placed on an always-active parent (e.g. the overlay text canvas),
    /// this component keeps each text GameObject's active state in sync
    /// with its corresponding slide GameObject.
    /// </summary>
    public class SlideTextSync : MonoBehaviour
    {
        [Serializable]
        public struct SlideTextPair
        {
            [Tooltip("The slide whose active state drives visibility.")]
            public GameObject slide;

            [Tooltip("The text GameObject to show/hide in sync.")]
            public GameObject text;
        }

        [SerializeField] private SlideTextPair[] pairs;

        private void LateUpdate()
        {
            if (pairs == null) return;

            for (int i = 0; i < pairs.Length; i++)
            {
                var slide = pairs[i].slide;
                var text = pairs[i].text;

                if (slide == null || text == null) continue;

                bool slideActive = slide.activeInHierarchy;
                if (text.activeSelf != slideActive)
                {
                    text.SetActive(slideActive);
                }
            }
        }
    }
}
