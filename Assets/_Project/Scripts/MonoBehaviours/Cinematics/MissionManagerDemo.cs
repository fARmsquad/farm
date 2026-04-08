using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class MissionManagerDemo : MonoBehaviour
    {
        private MissionManager missionManager;
        private static readonly Key Panel = DebugPanelShortcuts.MissionManager;

        private void Start()
        {
            missionManager = FindAnyObjectByType<MissionManager>();
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;
            if (missionManager == null) missionManager = FindAnyObjectByType<MissionManager>();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) missionManager?.StartMission("Farm Tour", "Follow the path to the farmhouse");
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) missionManager?.UpdateObjective("Now visit the barn");
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) missionManager?.CompleteMission();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) missionManager?.StartMission("Meet the Mayor", "Find the Mayor in town");
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) { missionManager?.CompleteMission(); missionManager?.StartMission("Explore", "Look around Willowbrook"); }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 310f; float h = 230f;
            float x = Screen.width - w - 10f; float y = (Screen.height - h) / 2f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Mission Manager (Shift+M)");
            float cy = y + 22f;

            string state = missionManager != null ? missionManager.CurrentMissionState.ToString() : "NULL";
            string name = missionManager?.CurrentMissionName ?? "—";
            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"State: {state} | {name}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Start 'Farm Tour'")) missionManager?.StartMission("Farm Tour", "Follow the path"); cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Update Objective")) missionManager?.UpdateObjective("Now visit the barn");         cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Complete Mission")) missionManager?.CompleteMission();                             cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Start 'Meet Mayor'")) missionManager?.StartMission("Meet the Mayor", "Find the Mayor"); cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[5] Complete + Start New")) { missionManager?.CompleteMission(); missionManager?.StartMission("Explore", "Look around"); }
        }
    }
}
