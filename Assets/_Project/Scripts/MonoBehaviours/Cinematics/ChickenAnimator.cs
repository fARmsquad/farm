using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Procedurally animates the intro chicken: idle bobbing, periodic pecking, and
    /// short strut walks around its spawn point. Runs entirely in world space using
    /// unscaled time so it stays active regardless of Time.timeScale.
    /// </summary>
    public class ChickenAnimator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Bob")]
        [SerializeField] private float bobAmplitude = 0.04f;
        [SerializeField] private float bobFrequency = 3.5f;

        [Header("Head Peck")]
        [SerializeField] private Transform headBone;
        [SerializeField] private float peckInterval = 2.2f;
        [SerializeField] private float peckDuration = 0.35f;
        [SerializeField] private float peckAngle = 28f;

        [Header("Strut Walk")]
        [SerializeField] private float strutRadius = 0.8f;
        [SerializeField] private float strutSpeed = 0.55f;
        [SerializeField] private float strutPauseMin = 1.5f;
        [SerializeField] private float strutPauseMax = 3f;

        #endregion

        #region Private State

        private Vector3 _spawnPosition;
        private float _bobTime;
        private float _peckTimer;
        private float _strutAngle;
        private float _strutPauseTimer;
        private bool _isStrutting;
        private float _strutTargetAngle;
        private Quaternion _headRestRotation;

        // Peck
        private float _peckProgress;
        private bool _isPecking;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            _spawnPosition = transform.position;
            _strutAngle = Random.Range(0f, 360f);
            _strutPauseTimer = Random.Range(strutPauseMin, strutPauseMax);

            if (headBone != null)
                _headRestRotation = headBone.localRotation;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            UpdateBob(dt);
            UpdatePeck(dt);
            UpdateStrut(dt);
        }

        #endregion

        #region Bob

        private void UpdateBob(float dt)
        {
            _bobTime += dt;
            float bobOffset = Mathf.Sin(_bobTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            Vector3 pos = transform.position;
            pos.y = _spawnPosition.y + bobOffset;
            transform.position = pos;
        }

        #endregion

        #region Peck

        private void UpdatePeck(float dt)
        {
            if (_isPecking)
            {
                _peckProgress += dt / peckDuration;

                if (_peckProgress >= 1f)
                {
                    _peckProgress = 0f;
                    _isPecking = false;
                    if (headBone != null)
                        headBone.localRotation = _headRestRotation;
                    _peckTimer = peckInterval + Random.Range(-0.4f, 0.4f);
                }
                else
                {
                    // Triangle wave: peck down then snap back
                    float t = _peckProgress < 0.5f
                        ? _peckProgress * 2f
                        : (1f - _peckProgress) * 2f;
                    t = Mathf.SmoothStep(0f, 1f, t);

                    if (headBone != null)
                        headBone.localRotation = _headRestRotation * Quaternion.Euler(peckAngle * t, 0f, 0f);
                }
            }
            else
            {
                _peckTimer -= dt;
                if (_peckTimer <= 0f)
                {
                    _isPecking = true;
                    _peckProgress = 0f;
                }
            }
        }

        #endregion

        #region Strut Walk

        private void UpdateStrut(float dt)
        {
            if (_isStrutting)
            {
                float step = strutSpeed * dt * 90f; // degrees per second
                float diff = Mathf.DeltaAngle(_strutAngle, _strutTargetAngle);

                if (Mathf.Abs(diff) <= step)
                {
                    _strutAngle = _strutTargetAngle;
                    _isStrutting = false;
                    _strutPauseTimer = Random.Range(strutPauseMin, strutPauseMax);
                }
                else
                {
                    _strutAngle += Mathf.Sign(diff) * step;
                }

                ApplyStrutPosition();
            }
            else
            {
                _strutPauseTimer -= dt;
                if (_strutPauseTimer <= 0f)
                {
                    _isStrutting = true;
                    _strutTargetAngle = _strutAngle + Random.Range(-80f, 80f);
                    ApplyStrutPosition();
                }
            }
        }

        private void ApplyStrutPosition()
        {
            float rad = _strutAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad) * strutRadius, 0f, Mathf.Cos(rad) * strutRadius);
            Vector3 targetXZ = _spawnPosition + offset;

            // Update spawn y reference so bob stays relative
            Vector3 pos = transform.position;
            pos.x = targetXZ.x;
            pos.z = targetXZ.z;
            transform.position = pos;
            _spawnPosition = new Vector3(targetXZ.x, _spawnPosition.y, targetXZ.z);

            // Face direction of travel
            Vector3 dir = offset.normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        #endregion
    }
}
