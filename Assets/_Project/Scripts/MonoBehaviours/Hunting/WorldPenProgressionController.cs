using System.IO;
using FarmSimVR.Core.Hunting;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public sealed class WorldPenProgressionController : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "world_pen_progress.json";

        private PenGameProgressionService _service;
        private string _statusMessage = string.Empty;
        private float _statusUntil;

        public PenGameProgressionService Service => _service;
        public string StatusMessage => Time.unscaledTime <= _statusUntil ? _statusMessage : string.Empty;
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            _service = new PenGameProgressionService();
            LoadNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        public PenDepositReward ApplyDeposits(int animalCount)
        {
            var reward = _service.ApplyDeposits(animalCount);
            if (reward.ExperienceEarned > 0)
            {
                SaveNow();
                SetStatus($"+{reward.ExperienceEarned} pen XP");
            }

            return reward;
        }

        public bool TrySpendHandlingPoint(out string message)
        {
            if (_service.TrySpendAnimalHandlingPoint())
            {
                message = $"Animal Handling increased to rank {_service.State.AnimalHandlingRank}.";
                SaveNow();
                SetStatus(message);
                return true;
            }

            message = _service.State.SkillPoints <= 0
                ? "No pen skill points available."
                : "Animal Handling is already maxed.";
            SetStatus(message);
            return false;
        }

        public void GrantDebugExperience(int amount)
        {
            _service.GrantDebugExperience(amount);
            SetStatus($"+{amount} pen XP");
        }

        public bool SaveNow()
        {
            try
            {
                var json = JsonUtility.ToJson(_service.CreateSnapshot(), true);
                File.WriteAllText(SavePath, json);
                return true;
            }
            catch
            {
                SetStatus("Failed to save pen progression.");
                return false;
            }
        }

        public bool LoadNow()
        {
            if (!File.Exists(SavePath))
                return false;

            try
            {
                var json = File.ReadAllText(SavePath);
                var snapshot = JsonUtility.FromJson<PenGameProgressionSnapshot>(json);
                _service.Restore(snapshot);
                SetStatus("Loaded pen progression.");
                return true;
            }
            catch
            {
                SetStatus("Failed to load pen progression.");
                return false;
            }
        }

        private void SetStatus(string message)
        {
            _statusMessage = message ?? string.Empty;
            _statusUntil = Time.unscaledTime + 3f;
        }
    }
}
