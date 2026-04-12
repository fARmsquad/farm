using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>Horse movement: bolt to random paddock position on spook; seek active carrot when calm.</summary>
    public sealed class HorseTamingHorse : MonoBehaviour
    {
        [SerializeField] private float seekSpeed = 3.5f;
        [SerializeField] private float eatRadius = 0.85f;

        private float _boltLockUntil;

        public bool IsBolting => Time.time < _boltLockUntil;

        public void ConfigureSeek(float speed, float eatDist)
        {
            seekSpeed = speed;
            eatRadius = eatDist;
        }

        public void BoltToRandom(Vector2 minXZ, Vector2 maxXZ, float boltDuration)
        {
            float rx = Random.Range(minXZ.x, maxXZ.x);
            float rz = Random.Range(minXZ.y, maxXZ.y);
            float y = transform.position.y;
            transform.position = new Vector3(rx, y, rz);
            _boltLockUntil = Time.time + Mathf.Max(0.05f, boltDuration);
        }

        /// <summary>Moves toward carrot; returns true if eaten this frame.</summary>
        public bool TickSeekCarrot(HorseTamingCarrot carrot, bool allowSeek)
        {
            if (carrot == null || !allowSeek)
                return false;

            var p = transform.position;
            var t = carrot.Position;
            var flat = new Vector3(t.x - p.x, 0f, t.z - p.z);
            float d = flat.magnitude;
            if (d <= eatRadius)
                return true;

            if (IsBolting)
                return false;

            var step = flat.normalized * (seekSpeed * Time.deltaTime);
            if (step.magnitude > d)
                step = flat;
            transform.position = p + step;
            return false;
        }
    }
}
