using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
using Framework.Events;
using RPG.Items;
using Gameplay.Player;

namespace RPG.Core
{
    /// <summary>
    /// 保存数据
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 玩家进度
        public int playerLevel = 1;
        public float playerExperience = 0f;
        public float experienceToNextLevel = 100f;
        public int gold = 0;

        // 玩家属性
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int attackPower = 10;
        public int defense = 0;
        public float moveSpeed = 5f;

        // 场景信息
        public string currentScene = "";
        public Vector3 playerPosition;

        // 游戏设置
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public int graphicsQuality = 2;
    }

    /// <summary>
    /// 保存系统
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        private const string SAVE_FILE_EXTENSION = ".save";
        private const string AUTO_SAVE_FILE = "AutoSave";
        private const string QUICK_SAVE_FILE = "QuickSave";
        private const int MAX_SAVE_SLOTS = 10;

        private string saveDirectory;
        private SaveData currentSaveData;

        public string[] SaveSlots { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            InitializeSaveDirectory();
            LoadSaveSlots();
        }

        private void InitializeSaveDirectory()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            Debug.Log($"Save directory: {saveDirectory}");
        }

        private void LoadSaveSlots()
        {
            List<string> saveFiles = new List<string>();

            if (Directory.Exists(saveDirectory))
            {
                string[] files = Directory.GetFiles(saveDirectory, "*" + SAVE_FILE_EXTENSION);
                foreach (string file in files)
                {
                    saveFiles.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            SaveSlots = saveFiles.ToArray();
            Debug.Log($"Found {saveFiles.Count} save files");
        }

        #region Save Methods

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame(string saveName = null)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

            SaveData data = CreateSaveData();
            string saveFilePath = GetSaveFilePath(saveName);

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(saveFilePath, json);

                currentSaveData = data;
                RefreshSaveSlots();

                Framework.Events.EventBus.Publish(new Framework.Events.GameSavedEvent(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                Debug.Log($"Game saved to: {saveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        /// <summary>
        /// 快速保存
        /// </summary>
        public void QuickSave()
        {
            SaveGame(QUICK_SAVE_FILE);
        }

        /// <summary>
        /// 自动保存
        /// </summary>
        public void AutoSave()
        {
            SaveGame(AUTO_SAVE_FILE);
        }

        /// <summary>
        /// 创建保存数据
        /// </summary>
        private SaveData CreateSaveData()
        {
            SaveData data = new SaveData();

            // 玩家进度
            var progressManager = PlayerProgressManager.Instance;
            if (progressManager != null)
            {
                data.playerLevel = progressManager.GetLevel();
                data.playerExperience = progressManager.GetExperience();
                data.experienceToNextLevel = progressManager.GetExperienceToNextLevel();
                data.gold = progressManager.GetGold();
            }

            // 玩家属性
            if (PlayerController.Instance != null)
            {
                data.maxHealth = Mathf.RoundToInt(PlayerController.Instance.Health.MaxHealth);
                data.currentHealth = Mathf.RoundToInt(PlayerController.Instance.Health.CurrentHealth);
                data.attackPower = Mathf.RoundToInt(PlayerController.Instance.AttackDamage);
                data.defense = Mathf.RoundToInt(PlayerController.Instance.Defense);
                data.moveSpeed = PlayerController.Instance.MoveSpeed;
                data.playerPosition = PlayerController.Instance.transform.position;
            }

            // 场景信息
            data.currentScene = SceneManager.GetActiveScene().name;

            return data;
        }

        #endregion

        #region Load Methods

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame(string saveName = null)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                // 加载最近的存档
                saveName = QUICK_SAVE_FILE;
            }

            string saveFilePath = GetSaveFilePath(saveName);

            if (!File.Exists(saveFilePath))
            {
                Debug.LogWarning($"Save file not found: {saveFilePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(saveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                ApplySaveData(data);
                currentSaveData = data;

                Framework.Events.EventBus.Publish(new Framework.Events.GameLoadedEvent(0));

                Debug.Log($"Game loaded from: {saveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
            }
        }

        /// <summary>
        /// 快速加载
        /// </summary>
        public void QuickLoad()
        {
            LoadGame(QUICK_SAVE_FILE);
        }

        /// <summary>
        /// 应用保存数据
        /// </summary>
        private void ApplySaveData(SaveData data)
        {
            // 玩家进度
            var progressManager = PlayerProgressManager.Instance;
            if (progressManager != null && progressManager.Progress != null)
            {
                progressManager.Progress.level = data.playerLevel;
                progressManager.Progress.experience = data.playerExperience;
                progressManager.Progress.experienceToNextLevel = data.experienceToNextLevel;
                progressManager.Progress.gold = data.gold;
                progressManager.NotifyProgressChanged();
            }

            // 玩家属性
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetMaxHealth(data.maxHealth);
                PlayerController.Instance.Health.Revive(data.currentHealth);
                PlayerController.Instance.SetAttackDamage(data.attackPower);
                PlayerController.Instance.SetDefense(data.defense);
                PlayerController.Instance.SetMoveSpeed(data.moveSpeed);
            }

            // 场景加载
            if (!string.IsNullOrEmpty(data.currentScene) && data.currentScene != SceneManager.GetActiveScene().name)
            {
                SceneManager.LoadScene(data.currentScene);
            }

            // 玩家位置
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.transform.position = data.playerPosition;
            }
        }

        #endregion

        #region Delete Methods

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSave(string saveName)
        {
            string saveFilePath = GetSaveFilePath(saveName);

            if (File.Exists(saveFilePath))
            {
                try
                {
                    File.Delete(saveFilePath);
                    RefreshSaveSlots();

                    Framework.Events.EventBus.Publish(new Framework.Events.SaveDeletedEvent(0));

                    Debug.Log($"Save deleted: {saveName}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete save: {e.Message}");
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 获取存档文件路径
        /// </summary>
        private string GetSaveFilePath(string saveName)
        {
            return Path.Combine(saveDirectory, saveName + SAVE_FILE_EXTENSION);
        }

        /// <summary>
        /// 刷新存档列表
        /// </summary>
        private void RefreshSaveSlots()
        {
            LoadSaveSlots();
        }

        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        public bool SaveExists(string saveName)
        {
            string saveFilePath = GetSaveFilePath(saveName);
            return File.Exists(saveFilePath);
        }

        /// <summary>
        /// 获取存档信息
        /// </summary>
        public SaveData GetSaveData(string saveName)
        {
            string saveFilePath = GetSaveFilePath(saveName);

            if (!File.Exists(saveFilePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(saveFilePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read save data: {e.Message}");
                return null;
            }
        }

        #endregion
    }

}
