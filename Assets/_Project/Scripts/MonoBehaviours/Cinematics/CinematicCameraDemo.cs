using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class CinematicCameraDemo : MonoBehaviour
    {
        private CinematicCamera cinematicCamera;
        private static readonly Key Panel = DebugPanelShortcuts.CinematicCamera;

        private void Start()
        {
            cinematicCamera = FindAnyObjectByType<CinematicCamera>();
            Debug.Log($"[CameraDemo] Start — cinematicCamera={(cinematicCamera != null ? "found" : "NULL")}");
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;
            if (cinematicCamera == null) cinematicCamera = FindAnyObjectByType<CinematicCamera>();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) { Debug.Log("[Cam] Enable Cinematic"); cinematicCamera?.EnableCinematicCamera(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) { Debug.Log("[Cam] Enable Gameplay"); cinematicCamera?.EnableGameplayCamera(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) { Debug.Log("[Cam] Farm"); OnMoveToFarm(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) { Debug.Log("[Cam] Town"); OnMoveToTown(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) { Debug.Log("[Cam] Path"); OnPlaySamplePath(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit6)) { Debug.Log("[Cam] Follow"); OnFollowPlayer(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit7)) { Debug.Log("[Cam] Hold"); cinematicCamera?.HoldPosition(); }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 300f; float h = 290f;
            float x = Screen.width - w - 10f; float y = Screen.height - h - 10f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Cinematic Camera (Shift+4)");
            float cy = y + 22f;

            string mode = cinematicCamera != null ? cinematicCamera.CurrentMode.ToString() : "NULL";
            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"Mode: {mode}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Enable Cinematic Camera")) cinematicCamera?.EnableCinematicCamera(); cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Enable Gameplay Camera")) cinematicCamera?.EnableGameplayCamera();   cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Move to Farm")) OnMoveToFarm();        cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Move to Town")) OnMoveToTown();        cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[5] Play 3-Shot Path")) OnPlaySamplePath(); cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[6] Follow Player")) OnFollowPlayer();     cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[7] Hold Position")) cinematicCamera?.HoldPosition();
        }

        private void OnMoveToFarm()
        {
            if (cinematicCamera == null) return;
            cinematicCamera.EnableCinematicCamera();
            cinematicCamera.MoveToWaypoint(new CameraWaypoint { position = new Vector3(72f, 25f, 24f), rotation = Quaternion.Euler(60f, 0f, 0f), fov = 60f, duration = 2f, easing = AnimationCurve.EaseInOut(0,0,1,1) });
        }

        private void OnMoveToTown()
        {
            if (cinematicCamera == null) return;
            cinematicCamera.EnableCinematicCamera();
            cinematicCamera.MoveToWaypoint(new CameraWaypoint { position = new Vector3(-72f, 15f, 6f), rotation = Quaternion.Euler(20f, 90f, 0f), fov = 55f, duration = 2f, easing = AnimationCurve.EaseInOut(0,0,1,1) });
        }

        private void OnPlaySamplePath()
        {
            if (cinematicCamera == null) return;
            cinematicCamera.EnableCinematicCamera();
            var path = ScriptableObject.CreateInstance<CameraPath>();
            path.waypoints = new CameraWaypoint[]
            {
                new CameraWaypoint { position = new Vector3(72f, 25f, 24f), rotation = Quaternion.Euler(60f, 0f, 0f), fov = 60f, duration = 3f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
                new CameraWaypoint { position = new Vector3(-72f, 15f, 6f), rotation = Quaternion.Euler(20f, 90f, 0f), fov = 55f, duration = 3f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
                new CameraWaypoint { position = new Vector3(0f, 40f, -20f), rotation = Quaternion.Euler(30f, 0f, 0f), fov = 70f, duration = 3f, easing = AnimationCurve.EaseInOut(0,0,1,1) },
            };
            cinematicCamera.PlayPath(path);
        }

        private void OnFollowPlayer()
        {
            if (cinematicCamera == null) return;
            cinematicCamera.EnableCinematicCamera();
            var player = FindAnyObjectByType<FirstPersonExplorer>();
            if (player != null) cinematicCamera.FollowTarget(player.transform, new Vector3(0f, 10f, -7f));
        }
    }
}
