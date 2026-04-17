using UnityEngine;
using UnityEditor;

namespace FarmSimVR.Editor
{
    public static class BarnDiagnostic
    {
        [MenuItem("fARm/Debug/Log Barn Bounds")]
        public static void LogBarnBounds()
        {
            var barn = GameObject.Find("SM_Bld_Barn_02");
            if (barn == null) { Debug.LogWarning("SM_Bld_Barn_02 not found"); return; }

            var renderers = barn.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Debug.LogWarning("No renderers on barn"); return; }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            Debug.Log($"Barn world pos: {barn.transform.position}");
            Debug.Log($"Barn bounds center: {bounds.center}  size: {bounds.size}");
            Debug.Log($"Barn bounds min: {bounds.min}  max: {bounds.max}");

            var door = GameObject.Find("BarnEntranceDoor");
            if (door != null) Debug.Log($"BarnEntranceDoor pos: {door.transform.position}");
            else Debug.LogWarning("BarnEntranceDoor not found in active scene");
        }
    }
}
