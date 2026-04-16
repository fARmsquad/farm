using UnityEngine;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// Keeps this transform's forward aligned with a camera (default <see cref="Camera.main"/>),
    /// matching the speech-bubble behavior in <c>ComicTextManager</c>.
    /// </summary>
    public sealed class BillboardFaceMainCamera : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;

        private void LateUpdate()
        {
            Camera cam = _targetCamera != null ? _targetCamera : Camera.main;
            if (cam == null)
                return;

            transform.forward = cam.transform.forward;
        }
    }
}
