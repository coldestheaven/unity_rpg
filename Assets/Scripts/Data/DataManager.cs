using UnityEngine;
using RPG.Items;
using RPG.Quests;
using RPG.Achievements;

namespace RPG.Core
{
    /// <summary>
    /// 数据管理器 - 统一管理所有数据库
    /// </summary>
    public class DataManager : Singleton<DataManager>
    {
        [Header("数据库")]
        public ItemDatabase itemDatabase;
        public QuestDatabase questDatabase;
        public AchievementDatabase achievementDatabase;

        public bool IsInitialized { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            // 不自动初始化,等待显式调用
        }

        /// <summary>
        /// 初始化所有数据库
        /// </summary>
        public void InitializeAllDatabases()
        {
            if (IsInitialized) return;

            InitializeDatabase(ref itemDatabase, "ItemDatabase");
            InitializeDatabase(ref questDatabase, "QuestDatabase");
            InitializeDatabase(ref achievementDatabase, "AchievementDatabase");

            IsInitialized = true;
            Debug.Log("All databases initialized");
        }

        /// <summary>
        /// 初始化单个数据库
        /// </summary>
        private void InitializeDatabase<T>(ref T database, string databaseName) where T : ScriptableObject
        {
            if (database == null)
            {
                database = Resources.Load<T>(databaseName);

                if (database == null)
                {
                    Debug.LogWarning($"Database not found: {databaseName}");
                }
            }

            // 尝试调用Initialize方法
            var initializeMethod = database?.GetType().GetMethod("Initialize");
            initializeMethod?.Invoke(database, null);
        }

        #region Database Access

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            return itemDatabase?.GetItem(itemId);
        }

        /// <summary>
        /// 获取任务数据
        /// </summary>
        public QuestData GetQuest(string questId)
        {
            return questDatabase?.GetQuest(questId);
        }

        /// <summary>
        /// 获取成就数据
        /// </summary>
        public AchievementData GetAchievement(string achievementId)
        {
            return achievementDatabase?.GetAchievement(achievementId);
        }

        /// <summary>
        /// 获取所有物品
        /// </summary>
        public ItemData[] GetAllItems()
        {
            return itemDatabase?.GetAllItems() ?? new ItemData[0];
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public QuestData[] GetAllQuests()
        {
            return questDatabase?.GetAllQuests() ?? new QuestData[0];
        }

        /// <summary>
        /// 获取所有成就
        /// </summary>
        public AchievementData[] GetAllAchievements()
        {
            return achievementDatabase?.GetAllAchievements() ?? new AchievementData[0];
        }

        #endregion

        #region Hot Reload (Editor Only)

#if UNITY_EDITOR
        /// <summary>
        /// 热重载所有数据库(仅编辑器)
        /// </summary>
        [UnityEditor.MenuItem("RPG/Data/Reload All Databases")]
        public static void ReloadAllDatabases()
        {
            if (Instance != null)
            {
                Instance.InitializeAllDatabases();
                Debug.Log("Databases reloaded");
            }
        }

        /// <summary>
        /// 导出数据库到JSON(仅编辑器)
        /// </summary>
        [UnityEditor.MenuItem("RPG/Data/Export Databases to JSON")]
        public static void ExportDatabasesToJSON()
        {
            // TODO: 实现数据库导出功能
            Debug.Log("Database export not implemented yet");
        }
#endif

        #endregion

        #region Save/Load

        /// <summary>
        /// 保存所有数据
        /// </summary>
        public void SaveAllData()
        {
            // 保存玩家进度
            PlayerProgressManager.Instance?.SaveProgress();

            // 保存任务进度
            QuestManager.Instance?.SaveQuestData();

            // 保存成就进度
            AchievementManager.Instance?.SaveAchievementProgress();

            Debug.Log("All data saved");
        }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        public void LoadAllData()
        {
            // 加载任务进度
            QuestManager.Instance?.LoadQuestData();

            // 加载成就进度
            AchievementManager.Instance?.LoadAchievementProgress();

            Debug.Log("All data loaded");
        }

        #endregion

        #region Debug

        /// <summary>
        /// 打印数据库统计信息
        /// </summary>
        public void PrintDatabaseStats()
        {
            Debug.Log("=== Database Statistics ===");

            if (itemDatabase != null)
            {
                Debug.Log($"Items: {itemDatabase.GetAllItems().Length}");
            }

            if (questDatabase != null)
            {
                Debug.Log($"Quests: {questDatabase.GetAllQuests().Length}");
            }

            if (achievementDatabase != null)
            {
                Debug.Log($"Achievements: {achievementDatabase.GetAllAchievements().Length}");
            }

            Debug.Log("==========================");
        }

        #endregion
    }
}
