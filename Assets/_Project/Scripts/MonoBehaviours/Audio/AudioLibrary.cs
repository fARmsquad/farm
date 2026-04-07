using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Audio
{
    /// <summary>
    /// ScriptableObject that maps string keys to AudioClips for easy lookup.
    /// Create via Assets > Create > FarmSimVR > Audio Library.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "FarmSimVR/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct AudioEntry
        {
            public string key;
            public AudioClip clip;
        }

        [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

        /// <summary>
        /// Returns the AudioClip associated with the given key, or null if not found.
        /// </summary>
        public AudioClip GetClip(string key)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                    return entries[i].clip;
            }
            return null;
        }
    }
}
