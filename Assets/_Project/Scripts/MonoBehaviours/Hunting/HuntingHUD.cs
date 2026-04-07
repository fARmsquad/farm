using UnityEngine;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class HuntingHUD : MonoBehaviour
    {
        private CaughtAnimalTracker _tracker;
        private WildAnimalSpawner _spawner;

        public void Initialize(CaughtAnimalTracker tracker, WildAnimalSpawner spawner)
        {
            _tracker = tracker;
            _spawner = spawner;
        }

        private void OnGUI()
        {
            if (_tracker == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            GUIStyle smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16
            };

            float y = 10;
            GUI.Label(new Rect(10, y, 400, 30), "=== HUNTING CHORE ===", style);
            y += 30;
            GUI.Label(new Rect(10, y, 400, 25), $"Carrying: {_tracker.CarriedCount} animals", smallStyle);
            y += 25;
            GUI.Label(new Rect(10, y, 400, 25), $"In Barn: {_tracker.DepositedCount} animals", smallStyle);
            y += 25;
            if (_spawner != null)
                GUI.Label(new Rect(10, y, 400, 25), $"Wild Animals: {_spawner.ActiveCount}", smallStyle);
            y += 35;
            GUI.Label(new Rect(10, y, 400, 25), "WASD = Move | E = Catch | Walk to Barn to deposit", smallStyle);
        }
    }
}
