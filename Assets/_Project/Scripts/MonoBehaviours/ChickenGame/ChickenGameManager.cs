using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    public class ChickenGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] public ChickenAI chicken;
        [SerializeField] public Transform player;
        [SerializeField] public TextMeshProUGUI timerText;
        [SerializeField] public TextMeshProUGUI resultText;
        [SerializeField] public TextMeshProUGUI instructionText;

        [Header("Settings")]
        [SerializeField] public float timeLimit = 45f;

        private float _timeRemaining;
        private bool _gameOver;
        private Vector3 _playerStartPos;
        private Quaternion _playerStartRot;
        private Vector3 _chickenStartPos;

        private void Start()
        {
            _timeRemaining = timeLimit;

            if (player != null)
            {
                _playerStartPos = player.position;
                _playerStartRot = player.rotation;
            }
            if (chicken != null)
                _chickenStartPos = chicken.transform.position;

            if (resultText != null) resultText.gameObject.SetActive(false);
            RefreshInstruction();
        }

        private void Update()
        {
            if (_gameOver)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                    RestartGame();
                return;
            }

            _timeRemaining -= Time.deltaTime;
            UpdateTimerText();

            if (chicken != null && player != null)
            {
                float dx = player.position.x - chicken.transform.position.x;
                float dz = player.position.z - chicken.transform.position.z;
                float sqDist = dx * dx + dz * dz;
                if (sqDist <= chicken.catchRadius * chicken.catchRadius)
                {
                    EndGame(won: true);
                    return;
                }
            }

            if (_timeRemaining <= 0f)
                EndGame(won: false);
        }

        private void UpdateTimerText()
        {
            if (timerText == null) return;
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining));
            timerText.text = $"Time: {secs}s";
            timerText.color = _timeRemaining <= 10f ? Color.red : Color.white;
        }

        private void EndGame(bool won)
        {
            _gameOver = true;
            if (chicken != null) chicken.enabled = false;
            if (timerText != null) timerText.gameObject.SetActive(false);

            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                if (won)
                {
                    int elapsed = Mathf.CeilToInt(timeLimit - _timeRemaining);
                    resultText.text = $"CAUGHT!\nGot it in {elapsed}s!";
                    resultText.color = new Color(0.2f, 0.9f, 0.2f);
                }
                else
                {
                    resultText.text = "TIME'S UP!\nThe chicken escaped!";
                    resultText.color = Color.red;
                }
            }

            if (instructionText != null)
                instructionText.text = "Press SPACE to try again";
        }

        private void RestartGame()
        {
            _gameOver = false;
            _timeRemaining = timeLimit;
            if (timerText != null) timerText.gameObject.SetActive(true);

            if (player != null)
            {
                player.position = _playerStartPos;
                player.rotation = _playerStartRot;
            }

            if (chicken != null)
            {
                chicken.transform.position = _chickenStartPos;
                chicken.enabled = true;
            }

            if (resultText != null) resultText.gameObject.SetActive(false);
            RefreshInstruction();
        }

        private void RefreshInstruction()
        {
            if (instructionText != null)
                instructionText.text = "WASD to move  ·  Mouse to aim  ·  Get close to catch!";
        }
    }
}
