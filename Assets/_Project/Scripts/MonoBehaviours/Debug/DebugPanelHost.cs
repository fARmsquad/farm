using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Debugging
{
    /// <summary>
    /// Drives the master debug menu. Attach to any persistent GameObject.
    /// Press Tab to open, pick a number, Tab to go back.
    /// </summary>
    public class DebugPanelHost : MonoBehaviour
    {
        private void Update()
        {
            DebugPanelShortcuts.UpdateInput();
        }

        private void OnGUI()
        {
            DebugPanelShortcuts.DrawMasterMenu();
        }
    }
}
