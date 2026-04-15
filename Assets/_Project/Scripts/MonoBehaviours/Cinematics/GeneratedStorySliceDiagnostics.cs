using System.Diagnostics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal static class GeneratedStorySliceDiagnostics
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string source, string message)
        {
            UnityEngine.Debug.Log($"[GeneratedStorySlice][{source}] {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string source, string message)
        {
            UnityEngine.Debug.LogWarning($"[GeneratedStorySlice][{source}] {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string source, string message)
        {
            UnityEngine.Debug.LogError($"[GeneratedStorySlice][{source}] {message}");
        }
    }
}
