using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayCinematicCamera : AutoplayBase
    {
        [SerializeField] private Transform playerTransform;

        private void Awake()
        {
            specId = "INT-004";
            specTitle = "Cinematic Camera";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            var cam = FindAnyObjectByType<CinematicCamera>();
            if (cam == null) { currentLabel = "CinematicCamera not found!"; yield break; }

            Step("Enable cinematic camera");
            cam.EnableCinematicCamera();
            yield return Wait(1f);

            Step("Move to overhead view");
            Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;
            cam.MoveToWaypoint(new CameraWaypoint
            {
                position = origin + new Vector3(0f, 30f, -20f),
                rotation = Quaternion.Euler(55f, 0f, 0f),
                fov = 60f, duration = 2.5f,
                easing = AnimationCurve.EaseInOut(0, 0, 1, 1)
            });
            yield return Wait(3.5f);

            Step("Play 3-shot camera path");
            var path = ScriptableObject.CreateInstance<CameraPath>();
            path.waypoints = new CameraWaypoint[]
            {
                new() { position = origin + new Vector3(20f, 8f, 0f), rotation = Quaternion.Euler(15f, -90f, 0f), fov = 55f, duration = 2.5f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
                new() { position = origin + new Vector3(0f, 12f, 20f), rotation = Quaternion.Euler(20f, 180f, 0f), fov = 50f, duration = 2.5f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
                new() { position = origin + new Vector3(-15f, 5f, -10f), rotation = Quaternion.Euler(10f, 45f, 0f), fov = 65f, duration = 2.5f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
            };
            cam.PlayPath(path);
            yield return Wait(9f);

            Step("Follow player (3 seconds)");
            if (playerTransform != null)
                cam.FollowTarget(playerTransform, new Vector3(0f, 8f, -6f));
            yield return Wait(3f);

            Step("Return to gameplay camera");
            cam.EnableGameplayCamera();
            yield return Wait(1f);
        }
    }
}
