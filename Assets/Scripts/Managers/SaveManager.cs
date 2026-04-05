using System;
using System.IO;
using UnityEngine;
using RPG.Core;

namespace Managers
{
    public class SaveManager : Framework.Base.SingletonMonoBehaviour<SaveManager>
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFolder = "Saves";
        [SerializeField] private int maxAutoSaves = 5;
        [SerializeField] private float autoSaveInterval = 300f;

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFolder);
        private float autoSaveTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }

        private void Update()
        {
            if (GameStateManager.Instance == null ||
                !GameStateManager.Instance.IsInState(GameState.Playing)) return;

            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }

        public void SaveGame(int slot = 0)
        {
            string saveFile = GetSaveFilePath(slot);
            var saveData = new SaveData
            {
                saveTime  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameState = GameStateManager.Instance?.CurrentState.ToString() ?? string.Empty
            };

            try
            {
                File.WriteAllText(saveFile, JsonUtility.ToJson(saveData, true));
                Framework.Events.EventManager.Instance?.TriggerEvent(Framework.Events.GameEvents.SAVE_GAME);
                Debug.Log($"Game saved to slot {slot}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save slot {slot}: {e.Message}");
            }
        }

        public void LoadGame(int slot = 0)
        {
            string saveFile = GetSaveFilePath(slot);

            if (!File.Exists(saveFile))
            {
                Debug.LogWarning($"No save file found at slot {slot}");
                return;
            }

            try
            {
                string json = File.ReadAllText(saveFile);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                ApplySaveData(saveData);
                Framework.Events.EventManager.Instance?.TriggerEvent(Framework.Events.GameEvents.LOAD_GAME);
                Debug.Log($"Game loaded from slot {slot}: {saveData.saveTime}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load slot {slot}: {e.Message}");
            }
        }

        private void ApplySaveData(SaveData saveData)
        {
            if (Enum.TryParse<GameState>(saveData.gameState, out GameState restoredState))
            {
                GameStateManager.Instance?.SetState(restoredState);
            }
        }

        public void AutoSave()
        {
            RotateAutoSaves();
            SaveGame(99);
        }

        public void QuickSave() => SaveGame(98);

        public void DeleteSave(int slot)
        {
            string saveFile = GetSaveFilePath(slot);
            if (File.Exists(saveFile))
            {
                File.Delete(saveFile);
                Debug.Log($"Save slot {slot} deleted");
            }
        }

        public bool HasSaveFile(int slot) => File.Exists(GetSaveFilePath(slot));

        private string GetSaveFilePath(int slot) =>
            Path.Combine(SavePath, $"save_{slot}.json");

        private void RotateAutoSaves()
        {
            for (int i = maxAutoSaves - 1; i > 0; i--)
            {
                string oldFile = GetSaveFilePath(99 + i - 1);
                string newFile = GetSaveFilePath(99 + i);
                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFile)) File.Delete(newFile);
                    File.Move(oldFile, newFile);
                }
            }
        }

        [Serializable]
        private class SaveData
        {
            public string saveTime;
            public string gameState;
        }
    }
}
