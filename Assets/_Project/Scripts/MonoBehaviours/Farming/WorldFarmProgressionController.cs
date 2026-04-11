using System.IO;
using FarmSimVR.Core.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class WorldFarmProgressionController : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "world_farming_progress.json";
        [SerializeField] private FarmSimDriver driver;

        private FarmProgressionService _service;
        private string _statusMessage = string.Empty;
        private float _statusUntil;

        public FarmProgressionService Service => _service;
        public string StatusMessage => Time.unscaledTime <= _statusUntil ? _statusMessage : string.Empty;
        public float WateringMultiplier => _service.GetWateringMultiplier();
        public float GrowthMultiplier => _service.GetGrowthMultiplier();
        public float RainMultiplier => _service.GetRainMultiplier();
        public string CurrentExpansionLabel => _service.GetCurrentExpansionLabel();
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            _service = new FarmProgressionService();
            LoadNow();
        }

        private void Start()
        {
            ResolveDriver();
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

        public bool TrySellHarvested(out string message)
        {
            ResolveDriver();
            if (driver == null)
            {
                message = "Farm driver not ready.";
                SetStatus(message);
                return false;
            }

            if (!driver.TrySellAllHarvested(_service, out message))
            {
                SetStatus(message);
                return false;
            }

            SaveNow();
            SetStatus(message);
            return true;
        }

        public bool TryBuyWateringUpgrade(out string message)
        {
            if (_service.TryBuyWateringUpgrade())
            {
                message = $"Watering can upgraded to tier {_service.State.WateringCanTier}.";
                SaveNow();
                SetStatus(message);
                return true;
            }

            var nextCost = _service.GetNextWateringUpgradeCost();
            message = nextCost < 0
                ? "Watering can is already maxed."
                : $"Need {nextCost} coins for the next watering upgrade.";
            SetStatus(message);
            return false;
        }

        public bool TryUnlockNextExpansion(out string message)
        {
            if (_service.TryUnlockNextExpansion())
            {
                message = $"Expansion unlocked: {_service.GetCurrentExpansionLabel()}.";
                SaveNow();
                SetStatus(message);
                return true;
            }

            var nextCost = _service.GetNextExpansionCost();
            message = nextCost < 0
                ? "All current expansion hooks are already unlocked."
                : $"Need {nextCost} coins for the next expansion hook.";
            SetStatus(message);
            return false;
        }

        public bool TrySpendSkill(FarmSkillType skill, out string message)
        {
            if (_service.TrySpendSkillPoint(skill))
            {
                message = $"Spent a point on {SkillLabel(skill)}.";
                SaveNow();
                SetStatus(message);
                return true;
            }

            message = _service.State.SkillPoints <= 0
                ? "No skill points available."
                : $"{SkillLabel(skill)} is already max rank.";
            SetStatus(message);
            return false;
        }

        public void GrantDebugCoins(int amount)
        {
            _service.GrantDebugCoins(amount);
            SetStatus($"+{amount} coins");
        }

        public void GrantDebugExperience(int amount)
        {
            _service.GrantDebugExperience(amount);
            SetStatus($"+{amount} XP");
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
                SetStatus("Failed to save farming progression.");
                return false;
            }
        }

        public bool LoadNow()
        {
            if (!File.Exists(SavePath))
            {
                SetStatus("No farming save found. Starting fresh.");
                return false;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var snapshot = JsonUtility.FromJson<FarmProgressionSnapshot>(json);
                _service.Restore(snapshot);
                SetStatus("Loaded farming progression.");
                return true;
            }
            catch
            {
                SetStatus("Failed to load farming progression.");
                return false;
            }
        }

        private void ResolveDriver()
        {
            if (driver == null)
                driver = GetComponent<FarmSimDriver>() ?? FindAnyObjectByType<FarmSimDriver>();
        }

        private void SetStatus(string message)
        {
            _statusMessage = message ?? string.Empty;
            _statusUntil = Time.unscaledTime + 3f;
        }

        private static string SkillLabel(FarmSkillType skill)
        {
            return skill switch
            {
                FarmSkillType.GreenThumb => "Green Thumb",
                FarmSkillType.Merchant => "Merchant",
                FarmSkillType.RainTender => "Rain Tender",
                _ => "Skill"
            };
        }
    }
}
