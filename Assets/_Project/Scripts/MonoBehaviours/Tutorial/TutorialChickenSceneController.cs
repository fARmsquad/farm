using FarmSimVR.MonoBehaviours.ChickenGame;
using UnityEngine;
using FarmSimVR.Core.Tutorial;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialChickenSceneController : MonoBehaviour
    {
        private ChickenGameManager _manager;
        private float _advanceAt = -1f;
        private GUIStyle _style;

        private void Start()
        {
            _manager = FindAnyObjectByType<ChickenGameManager>();
        }

        private void Update()
        {
            if (_manager == null || _advanceAt >= 0f)
            {
                if (_advanceAt >= 0f && Time.time >= _advanceAt)
                {
                    _advanceAt = -1f;
                    TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
                }
                return;
            }

            if (_manager.IsGameOver && _manager.IsWon)
                _advanceAt = Time.time + TutorialDevTuning.PostMinigameAdvanceDelay;
        }

        private void OnGUI()
        {
            if (_manager == null)
                return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _style.normal.textColor = Color.white;

            var text = _advanceAt >= 0f
                ? "Chicken secured. Heading to the next tutorial beat..."
                : "Tutorial Goal: Catch the chicken and drop it in the coop.";

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(18f, 120f, 420f, 34f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30f, 126f, 396f, 22f), text, _style);
        }
    }
}
