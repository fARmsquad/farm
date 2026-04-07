using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// A single line of dialogue with speaker info, text, timing, and display options.
    /// </summary>
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)] public string text;
        public float duration;
        public bool autoAdvance;
        public Color speakerColor;
    }

    /// <summary>
    /// ScriptableObject container for a sequence of dialogue lines.
    /// Create via Assets > Create > FarmSimVR > Dialogue Data.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueData", menuName = "FarmSimVR/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
