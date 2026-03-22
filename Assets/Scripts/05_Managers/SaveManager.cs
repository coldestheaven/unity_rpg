using System;
using System.IO;
using UnityEngine;

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

        private void Update()
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }

        private void Awake()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }

        public void SaveGame(int slot = 0)
        {
            string saveFile = GetSaveFilePath(slot);
            SaveData saveData = new SaveData
            {
                saveTime = DateTime.Now.ToString(),
                gameState = GameStateManager.Instance.CurrentState.ToString()
            };

            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFile, jsonData);

            Debug.Log($"Game saved to slot {slot} at {saveFile}");
        }

        public void LoadGame(int slot = 0)
        {
            string saveFile = GetSaveFilePath(slot);

            if (!File.Exists(saveFile))
            {
                Debug.LogWarning($"No save file found at slot {slot}");
                return;
            }

            string jsonData = File.ReadAllText(saveFile);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            Debug.Log($"Game loaded from slot {slot}: {saveData.saveTime}");

            // Trigger load event
            Framework.Events.EventManager.Instance.TriggerEvent(Framework.Events.GameEvents.LOAD_GAME);
        }

        public void AutoSave()
        {
            RotateAutoSaves();
            SaveGame(99);
        }

        public void QuickSave()
        {
            SaveGame(98);
        }

        public void DeleteSave(int slot)
        {
            string saveFile = GetSaveFilePath(slot);
            if (File.Exists(saveFile))
            {
                File.Delete(saveFile);
                Debug.Log($"Save slot {slot} deleted");
            }
        }

        public bool HasSaveFile(int slot)
        {
            return File.Exists(GetSaveFilePath(slot));
        }

        private string GetSaveFilePath(int slot)
        {
            return Path.Combine(SavePath, $"save_{slot}.json");
        }

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
