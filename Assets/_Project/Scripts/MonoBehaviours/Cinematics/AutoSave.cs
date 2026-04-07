using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Pure static auto-save utility for intro completion and game clock state.
    /// Persists to JSON in Application.persistentDataPath.
    /// </summary>
    public static class AutoSave
    {
        [System.Serializable]
        private class SaveData
        {
            public bool introComplete;
            public float gameClockTime;
            public string timestamp;
            public int version = 1;
        }

        private const int CurrentVersion = 1;

        private static string SavePath =>
            System.IO.Path.Combine(Application.persistentDataPath, "farm_save.json");

        /// <summary>
        /// Writes a save file marking the intro as complete.
        /// </summary>
        public static void SaveIntroComplete(float gameClockTime = 6.25f)
        {
            var data = new SaveData
            {
                introComplete = true,
                gameClockTime = gameClockTime,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                version = CurrentVersion
            };

            try
            {
                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(SavePath, json);
                Debug.Log($"[AutoSave] Saved intro complete (clock={gameClockTime:F2}) to {SavePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoSave] Failed to write save file: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns true if a valid save exists with introComplete and matching version.
        /// </summary>
        public static bool HasCompletedIntro()
        {
            var data = GetSaveDataInternal();
            return data != null && data.introComplete && data.version == CurrentVersion;
        }

        /// <summary>
        /// Returns the parsed SaveData, or null if the file is missing, corrupt, or version-mismatched.
        /// </summary>
        public static object GetSaveData()
        {
            return GetSaveDataInternal();
        }

        private static SaveData GetSaveDataInternal()
        {
            string path = SavePath;

            if (!System.IO.File.Exists(path))
            {
                Debug.Log("[AutoSave] No save file found.");
                return null;
            }

            try
            {
                string json = System.IO.File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[AutoSave] Save file parsed as null.");
                    return null;
                }

                if (data.version != CurrentVersion)
                {
                    Debug.LogWarning($"[AutoSave] Version mismatch: save={data.version}, expected={CurrentVersion}");
                    return null;
                }

                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoSave] Corrupted save file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the save file if it exists.
        /// </summary>
        public static void ClearSave()
        {
            string path = SavePath;
            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                    Debug.Log("[AutoSave] Save file deleted.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AutoSave] Failed to delete save file: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[AutoSave] No save file to delete.");
            }
        }
    }
}
