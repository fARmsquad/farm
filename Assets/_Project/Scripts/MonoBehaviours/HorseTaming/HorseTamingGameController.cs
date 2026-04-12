using FarmSimVR.Core.HorseTaming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Top-down horse taming session: trust loop, carrot drop, pet + mount win.
    /// </summary>
    public sealed class HorseTamingGameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HorseTamingPlayerController player;
        [SerializeField] private HorseTamingHorse horse;
        [SerializeField] private HorseTamingComfortRing comfortRing;
        [SerializeField] private HorseTamingTopDownCamera topDownCamera;
        [SerializeField] private Image trustFill;
        [SerializeField] private Text trustPercentLabel;
        [SerializeField] private Text hintLabel;

        [Header("Comfort zone")]
        [SerializeField] private float comfortRadius = 4f;
        [Tooltip("Inside the green ring, closer than this (meters) crowds the horse and drains trust.")]
        [SerializeField] private float crowdingRadius = 1.35f;
        [SerializeField] private float crowdingLossPerSecond = 12f;

        [Header("Trust rates (per second)")]
        [SerializeField] private float walkTrustPerSecond = 7f;
        [SerializeField] private float standTrustPerSecond = 2.5f;
        [SerializeField] private float spookPenalty = 18f;
        [SerializeField] private float carrotTrustBonus = 22f;

        [Header("Carrots")]
        [SerializeField] private int startingCarrots = 3;

        [Header("Paddock (XZ) for bolt")]
        [SerializeField] private Vector2 boltMinXZ = new(-9f, -9f);
        [SerializeField] private Vector2 boltMaxXZ = new(9f, 9f);
        [SerializeField] private float boltDuration = 0.12f;

        [Header("Pet / mount")]
        [SerializeField] private float petDistance = 2f;
        [SerializeField] private Vector3 mountLocalOffset = new(0f, 1.15f, 0f);
        [SerializeField] private float mountedMoveSpeed = 5f;

        private float _trust;
        private int _carrotsLeft;
        private HorseTamingCarrot _activeCarrot;
        private bool _mounted;
        private Keyboard _keyboard;

        /// <summary>Used by <see cref="HorseTamingWorldBuilder"/> so the scene can be built entirely at runtime.</summary>
        public void WireRuntime(
            HorseTamingPlayerController playerController,
            HorseTamingHorse horseBehaviour,
            HorseTamingComfortRing ring,
            HorseTamingTopDownCamera cameraRig,
            Image fill,
            Text trustLabel,
            Text hints)
        {
            player = playerController;
            horse = horseBehaviour;
            comfortRing = ring;
            topDownCamera = cameraRig;
            trustFill = fill;
            trustPercentLabel = trustLabel;
            hintLabel = hints;
        }

        private void Start()
        {
            _keyboard = Keyboard.current;
            _trust = 0f;
            _carrotsLeft = startingCarrots;
            if (comfortRing != null)
                comfortRing.SetRadius(comfortRadius);
            if (horse != null)
                horse.ConfigureSeek(3.5f, 0.85f);

            UpdateUi();
        }

        private void OnEnable()
        {
            _keyboard = Keyboard.current;
        }

        private void Update()
        {
            if (player == null || horse == null)
                return;

            if (_mounted)
            {
                TickMountedRide();
                UpdateUi();
                return;
            }

            if (_keyboard == null)
                _keyboard = Keyboard.current;
            if (_keyboard == null)
                return;

            if (_keyboard.cKey.wasPressedThisFrame)
                TryDropCarrot();

            TickCarrotAndHorse();
            TickComfortTrust();

            if (_trust >= 100f - 1e-3f && _keyboard.eKey.wasPressedThisFrame && CanPet())
                Mount();

            UpdateUi();
        }

        /// <summary>After mount, WASD drives the horse transform; rider is parented under the horse so they move as one rig.</summary>
        private void TickMountedRide()
        {
            if (_keyboard == null)
                _keyboard = Keyboard.current;
            if (_keyboard == null)
                return;

            float x = 0f;
            if (_keyboard.aKey.isPressed) x -= 1f;
            if (_keyboard.dKey.isPressed) x += 1f;
            float z = 0f;
            if (_keyboard.sKey.isPressed) z -= 1f;
            if (_keyboard.wKey.isPressed) z += 1f;

            var dir = new Vector3(x, 0f, z);
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();
            if (dir.sqrMagnitude < 0.01f)
                return;

            var p = horse.transform.position;
            p += dir * (mountedMoveSpeed * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, boltMinXZ.x, boltMaxXZ.x);
            p.z = Mathf.Clamp(p.z, boltMinXZ.y, boltMaxXZ.y);
            horse.transform.position = p;
        }

        private void TryDropCarrot()
        {
            if (_activeCarrot != null || _carrotsLeft <= 0 || player == null)
                return;

            var pos = player.transform.position + new Vector3(0.4f, 0.1f, 0.4f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Carrot";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(1f, 0.45f, 0.1f);

            _activeCarrot = go.AddComponent<HorseTamingCarrot>();
            _carrotsLeft--;
        }

        private void TickCarrotAndHorse()
        {
            if (_activeCarrot == null)
                return;

            bool calmEnough = !horse.IsBolting;
            bool eaten = horse.TickSeekCarrot(_activeCarrot, calmEnough);
            if (eaten)
            {
                _trust = HorseTamingTrustMath.ApplyCarrotBonus(_trust, carrotTrustBonus);
                Destroy(_activeCarrot.gameObject);
                _activeCarrot = null;
            }
        }

        private void TickComfortTrust()
        {
            if (_trust >= 100f)
            {
                _trust = 100f;
                return;
            }

            Vector3 pp = player.transform.position;
            Vector3 hp = horse.transform.position;
            float dx = pp.x - hp.x;
            float dz = pp.z - hp.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            bool inComfort = dist <= comfortRadius;
            bool tooClose = inComfort && dist < crowdingRadius;

            var r = HorseTamingTrustMath.ProcessComfortTrust(
                _trust,
                inComfort,
                tooClose,
                player.HasMoveInput,
                player.SprintHeld,
                Time.deltaTime,
                walkTrustPerSecond,
                standTrustPerSecond,
                spookPenalty,
                crowdingLossPerSecond);

            if (r.Spooked)
            {
                horse.BoltToRandom(boltMinXZ, boltMaxXZ, boltDuration);
            }

            _trust = r.Trust;
        }

        private bool CanPet()
        {
            Vector3 pp = player.transform.position;
            Vector3 hp = horse.transform.position;
            float dx = pp.x - hp.x;
            float dz = pp.z - hp.z;
            return Mathf.Sqrt(dx * dx + dz * dz) <= petDistance;
        }

        private void Mount()
        {
            _mounted = true;
            if (player != null)
            {
                player.MovementEnabled = false;
                player.enabled = false;
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                    cc.enabled = false;

                // Parent the full player rig (CharacterController root + mesh) so one transform hierarchy rides the horse.
                player.transform.SetParent(horse.transform, true);
                player.transform.localPosition = mountLocalOffset;
                player.transform.localRotation = Quaternion.identity;
            }

            if (topDownCamera != null)
                topDownCamera.SetTarget(horse.transform);

            if (hintLabel != null)
                hintLabel.text = "Mounted! WASD to ride. (You tamed the horse.)";
        }

        private void UpdateUi()
        {
            if (trustFill != null)
                trustFill.fillAmount = _trust / 100f;
            if (trustPercentLabel != null)
                trustPercentLabel.text = $"Trust {_trust:0}%";

            if (hintLabel == null)
                return;

            if (_mounted)
                return;

            if (_trust >= 100f - 1e-3f)
            {
                hintLabel.text = CanPet()
                    ? "Press E to pet the horse (then auto-mount)."
                    : "Move closer to pet (E).";
            }
            else
            {
                hintLabel.text =
                    "WASD slow walk inside the green ring builds trust — not too close to the horse, and not too far. " +
                    "Stand still for a slower gain. Shift sprints — never sprint inside the ring. " +
                    "C drops a carrot (+trust when eaten). " +
                    $"Carrots left: {_carrotsLeft}";
            }
        }
    }
}
