using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class MissionManagerDemo : MonoBehaviour
    {
        private MissionManager missionManager;
        private static readonly Key Panel = DebugPanelShortcuts.MissionManager;

        private void Start() => TryFind();

        private void TryFind()
        {
            if (missionManager == null)
                missionManager = FindAnyObjectByType<MissionManager>();
        }

        private void Update()
        {
            DebugPanelShortcuts.UpdateInput();
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;
            TryFind();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) OnStartFarmTour();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) OnUpdateObjective();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) OnCompleteMission();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) OnStartMeetTheMayor();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) OnCompleteAndStartNew();
        }

        private void OnGUI()
        {
            DebugPanelShortcuts.DrawMasterMenu();
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 310f; float h = 240f;
            float x = Screen.width - w - 10f; float y = (Screen.height - h) / 2f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Mission Manager (Tab to close)");
            float cy = y + 22f;

            string state = missionManager != null ? missionManager.CurrentMissionState.ToString() : "No Manager";
            string name = missionManager != null ? (missionManager.CurrentMissionName ?? "—") : "—";
            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"State: {state} | Mission: {name}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Start 'Farm Tour' Mission")) OnStartFarmTour();        cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Update Objective")) OnUpdateObjective();                cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Complete Mission")) OnCompleteMission();                cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Start 'Meet the Mayor'")) OnStartMeetTheMayor();       cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[5] Complete + Start New")) OnCompleteAndStartNew();
        }

        private void OnStartFarmTour() => missionManager?.StartMission("Farm Tour", "Follow the path to the farmhouse");
        private void OnUpdateObjective() => missionManager?.UpdateObjective("Now visit the barn");
        private void OnCompleteMission() => missionManager?.CompleteMission();
        private void OnStartMeetTheMayor() => missionManager?.StartMission("Meet the Mayor", "Find the Mayor in town");
        private void OnCompleteAndStartNew()
        {
            missionManager?.CompleteMission();
            missionManager?.StartMission("Explore", "Look around Willowbrook");
        }
    }
}
