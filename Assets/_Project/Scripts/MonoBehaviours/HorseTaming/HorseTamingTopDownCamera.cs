using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Perspective follow: sits above and behind the target with a slight tilt so the paddock reads as 3D (not flat ortho top-down).
    /// </summary>
    public sealed class HorseTamingTopDownCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Framing")]
        [Tooltip("World-space offset from the focus point (typically up + back along -Z).")]
        [SerializeField] private Vector3 offsetFromFocus = new Vector3(0f, 14f, -13f);

        [Tooltip("Height added to the target position for the look-at point (chest / center mass).")]
        [SerializeField] private float focusHeight = 1.2f;

        [SerializeField] private float fieldOfView = 50f;

        [Header("Optional orthographic (debug)")]
        [SerializeField] private bool useOrthographic;
        [SerializeField] private float orthographicSize = 11f;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null)
                return;

            _cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                _cam.orthographicSize = orthographicSize;
            }
            else
            {
                _cam.fieldOfView = fieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            var focus = target.position + Vector3.up * focusHeight;

            if (useOrthographic)
            {
                transform.position = new Vector3(focus.x, offsetFromFocus.y, focus.z);
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                return;
            }

            transform.position = focus + offsetFromFocus;
            transform.LookAt(focus);
        }

        public void SetTarget(Transform t) => target = t;
    }
}
