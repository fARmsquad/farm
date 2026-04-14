using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Audio
{
    /// <summary>
    /// Plays a footstep AudioClip at regular intervals while the
    /// CharacterController is grounded and moving.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudio : MonoBehaviour
    {
        private const float WALK_STEP_INTERVAL = 0.45f;
        private const float RUN_STEP_INTERVAL  = 0.3f;
        private const float MIN_VELOCITY_SQR   = 0.1f * 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioClip footstepClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

        [Header("Variation")]
        [Tooltip("Random pitch range applied to each step for natural variation.")]
        [SerializeField] private float minPitch = 0.9f;
        [SerializeField] private float maxPitch = 1.1f;

        private CharacterController _cc;
        private AudioSource _audioSource;
        private float _stepTimer;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _audioSource = GetComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            Vector3 horizontalVelocity = _cc.velocity;
            horizontalVelocity.y = 0f;

            bool isMoving = horizontalVelocity.sqrMagnitude > MIN_VELOCITY_SQR;
            bool isGrounded = _cc.isGrounded;

            if (!isMoving || !isGrounded)
            {
                _stepTimer = 0f;
                return;
            }

            float interval = IsRunning() ? RUN_STEP_INTERVAL : WALK_STEP_INTERVAL;

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= interval)
            {
                PlayFootstep();
                _stepTimer -= interval;
            }
        }

        /// <summary>
        /// Plays a single footstep with slight pitch randomisation.
        /// </summary>
        private void PlayFootstep()
        {
            if (footstepClip == null) return;

            _audioSource.pitch = Random.Range(minPitch, maxPitch);
            _audioSource.PlayOneShot(footstepClip, volume);
        }

        /// <summary>
        /// Checks whether the player is holding the run key (left shift).
        /// </summary>
        private static bool IsRunning()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.leftShiftKey.isPressed;
        }
    }
}
