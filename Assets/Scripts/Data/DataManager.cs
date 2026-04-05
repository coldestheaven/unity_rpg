using System;
using RPG.Achievements;
using RPG.Items;
using RPG.Quests;
using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// [已废弃] 数据管理器兼容层 — 委托给 <see cref="RPG.Data.GameDataService"/>。
    ///
    /// 新代码请直接使用:
    ///   <code>RPG.Data.GameDataService.Instance.Items.GetById("item_sword")</code>
    ///
    /// 此类保留仅为向后兼容，后续版本将移除。
    /// </summary>
    [Obsolete("使用 RPG.Data.GameDataService.Instance 替代。此类将在未来版本移除。")]
    public class DataManager : Singleton<DataManager>
    {
        [Header("数据库（已废弃，请改用 GameDataService）")]
        [Tooltip("留空后将自动从 GameDataService 获取")]
        public ItemDatabase        itemDatabase;
        public QuestDatabase       questDatabase;
        public AchievementDatabase achievementDatabase;

        public bool IsInitialized { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>初始化所有数据库（委托给 GameDataService）。</summary>
        public void InitializeAllDatabases()
        {
            if (IsInitialized) return;

            // Prefer local references; fall back to GameDataService
            var svc = RPG.Data.GameDataService.Instance;
            if (itemDatabase == null && svc?.Items != null)
                itemDatabase = svc.Items as ItemDatabase;
            if (questDatabase == null && svc?.Quests != null)
                questDatabase = svc.Quests as QuestDatabase;
            if (achievementDatabase == null && svc?.Achievements != null)
                achievementDatabase = svc.Achievements as AchievementDatabase;

            IsInitialized = true;
        }

        // ── Facade methods (backward compat) ─────────────────────────────────

        public ItemData        GetItem(string id)        => RPG.Data.GameDataService.Instance?.GetItem(id)
                                                          ?? itemDatabase?.GetItem(id);
        public QuestData       GetQuest(string id)       => RPG.Data.GameDataService.Instance?.GetQuest(id)
                                                          ?? questDatabase?.GetQuest(id);
        public AchievementData GetAchievement(string id) => RPG.Data.GameDataService.Instance?.Achievements?.GetById(id)
                                                          ?? achievementDatabase?.GetAchievement(id);

        public ItemData[]        GetAllItems()       => itemDatabase?.GetAllItems()        ?? Array.Empty<ItemData>();
        public QuestData[]       GetAllQuests()      => questDatabase?.GetAllQuests()      ?? Array.Empty<QuestData>();
        public AchievementData[] GetAllAchievements()=> achievementDatabase?.GetAllAchievements() ?? Array.Empty<AchievementData>();
    }
}
