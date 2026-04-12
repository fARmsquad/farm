using UnityEngine;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    /// <summary>
    /// Random 3D clucks on the chicken; volume follows listener distance via <see cref="AudioSource"/>.
    /// </summary>
    public class ChickenCluckAudio : MonoBehaviour
    {
        [SerializeField] private ChickenAI _chicken;
        [SerializeField] private AudioClip[] _cluckClips;
        [SerializeField] private float _minInterval = 2.2f;
        [SerializeField] private float _maxInterval = 5.5f;

        private AudioSource _source;
        private float _nextCluckTime;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Logarithmic;
            _source.minDistance = 1f;
            _source.maxDistance = 28f;

            if (_chicken == null)
                TryGetComponent(out _chicken);
        }

        private void OnEnable()
        {
            ScheduleNextCluck(initialDelay: true);
        }

        private void Update()
        {
            if (_chicken == null || _cluckClips == null || _cluckClips.Length == 0)
                return;

            if (!_chicken.isActiveAndEnabled || _chicken.IsCaught)
                return;

            if (Time.time >= _nextCluckTime)
            {
                var clip = _cluckClips[Random.Range(0, _cluckClips.Length)];
                if (clip != null)
                    _source.PlayOneShot(clip);
                ScheduleNextCluck(initialDelay: false);
            }
        }

        private void ScheduleNextCluck(bool initialDelay)
        {
            float delay = initialDelay
                ? Random.Range(_minInterval, _maxInterval)
                : Random.Range(_minInterval, _maxInterval);
            _nextCluckTime = Time.time + delay;
        }
    }
}
