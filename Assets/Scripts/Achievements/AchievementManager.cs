using UnityEngine;
using System;
using System.Collections.Generic;
using Framework.Assets;
using Framework.Events;
using RPG.Core;

namespace RPG.Achievements
{
    /// <summary>
    /// 成就管理器
    /// </summary>
    public class AchievementManager : Singleton<AchievementManager>
    {
        [Header("成就数据库")]
        public AchievementDatabase achievementDatabase;

        private Dictionary<string, AchievementInstance> achievements;

        public int TotalAchievements => achievements.Count;
        public int UnlockedAchievements => GetUnlockedCount();
        public int ClaimedAchievements => GetClaimedCount();

        public event Action<AchievementInstance> OnAchievementUnlocked;
        public event Action<AchievementInstance> OnAchievementClaimed;
        public event Action<AchievementInstance> OnProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            InitializeAchievementSystem();
            SubscribeToEvents();
        }

        private void InitializeAchievementSystem()
        {
            achievements = new Dictionary<string, AchievementInstance>();

            if (achievementDatabase == null)
            {
                achievementDatabase = AssetService.Load<AchievementDatabase>(AssetPaths.Data.AchievementDatabase);
            }

            if (achievementDatabase != null)
            {
                AchievementData[] allAchievements = achievementDatabase.GetAllAchievements();

                foreach (var achievementData in allAchievements)
                {
                    AchievementInstance instance = new AchievementInstance(achievementData);
                    instance.OnAchievementUnlocked += OnAchievementUnlockedHandler;
                    instance.OnProgressChanged += OnProgressChangedHandler;
                    achievements[achievementData.achievementId] = instance;
                }

                Debug.Log($"Loaded {achievements.Count} achievements");
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        #region Achievement Management

        /// <summary>
        /// 获取成就实例
        /// </summary>
        public AchievementInstance GetAchievement(string achievementId)
        {
            achievements.TryGetValue(achievementId, out AchievementInstance instance);
            return instance;
        }

        /// <summary>
        /// 获取所有成就
        /// </summary>
        public AchievementInstance[] GetAllAchievements()
        {
            return new List<AchievementInstance>(achievements.Values).ToArray();
        }

        /// <summary>
        /// 获取已解锁的成就
        /// </summary>
        public AchievementInstance[] GetUnlockedAchievements()
        {
            List<AchievementInstance> unlocked = new List<AchievementInstance>();

            foreach (var achievement in achievements.Values)
            {
                if (achievement.State == AchievementState.Unlocked ||
                    achievement.State == AchievementState.Claimed)
                {
                    unlocked.Add(achievement);
                }
            }

            return unlocked.ToArray();
        }

        /// <summary>
        /// 获取可领取奖励的成就
        /// </summary>
        public AchievementInstance[] GetClaimableAchievements()
        {
            List<AchievementInstance> claimable = new List<AchievementInstance>();

            foreach (var achievement in achievements.Values)
            {
                if (achievement.State == AchievementState.Unlocked)
                {
                    claimable.Add(achievement);
                }
            }

            return claimable.ToArray();
        }

        /// <summary>
        /// 领取成就奖励
        /// </summary>
        public bool ClaimAchievement(string achievementId)
        {
            if (!achievements.TryGetValue(achievementId, out AchievementInstance instance))
            {
                Debug.LogWarning($"Achievement not found: {achievementId}");
                return false;
            }

            bool success = instance.ClaimRewards();

            if (success)
            {
                OnAchievementClaimed?.Invoke(instance);
            }

            return success;
        }

        /// <summary>
        /// 检查成就条件
        /// </summary>
        public void CheckAchievements(AchievementConditionType conditionType, object[] args)
        {
            foreach (var achievement in achievements.Values)
            {
                if (achievement.Data.conditionType == conditionType)
                {
                    achievement.CheckCondition(args);
                }
            }
        }

        /// <summary>
        /// 获取解锁进度
        /// </summary>
        public float GetUnlockProgress()
        {
            if (TotalAchievements == 0) return 0f;
            return (float)UnlockedAchievements / TotalAchievements;
        }

        /// <summary>
        /// 获取已解锁数量
        /// </summary>
        private int GetUnlockedCount()
        {
            int count = 0;
            foreach (var achievement in achievements.Values)
            {
                if (achievement.State == AchievementState.Unlocked ||
                    achievement.State == AchievementState.Claimed)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取已领取数量
        /// </summary>
        private int GetClaimedCount()
        {
            int count = 0;
            foreach (var achievement in achievements.Values)
            {
                if (achievement.State == AchievementState.Claimed)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Event Handlers

        private void OnPlayerLevelUp(PlayerLevelUpEvent e)
        {
            CheckAchievements(AchievementConditionType.ReachLevel, new object[] { e.NewLevel });
        }

        private void OnQuestCompleted(QuestCompletedEvent e)
        {
            CheckAchievements(AchievementConditionType.CompleteQuest, new object[] { e.QuestId });
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            CheckAchievements(AchievementConditionType.GoldAmount, new object[] { e.CurrentGold });
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            // 记录游戏开始时间,用于游戏时长成就
        }

        #endregion

        #region Callbacks

        private void OnAchievementUnlockedHandler(AchievementInstance achievement)
        {
            OnAchievementUnlocked?.Invoke(achievement);

            if (achievement.Data.hasNotification)
            {
                ShowAchievementNotification(achievement);
            }

            Debug.Log($"Achievement unlocked: {achievement.Data.achievementName}");
        }

        private void OnProgressChangedHandler(AchievementInstance achievement)
        {
            OnProgressChanged?.Invoke(achievement);
        }

        #endregion

        #region Notification

        private void ShowAchievementNotification(AchievementInstance achievement)
        {
            // TODO: 显示成就通知UI
            Debug.Log($"Achievement notification: {achievement.Data.achievementName}");
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// 保存成就进度
        /// </summary>
        public void SaveAchievementProgress()
        {
            List<AchievementProgress> progressList = new List<AchievementProgress>();

            foreach (var achievement in achievements.Values)
            {
                progressList.Add(achievement.SaveProgress());
            }

            string jsonData = JsonUtility.ToJson(new AchievementSaveData
            {
                achievements = progressList.ToArray()
            }, true);

            PlayerPrefs.SetString("AchievementProgress", jsonData);
            PlayerPrefs.Save();

            Debug.Log("Achievement progress saved");
        }

        /// <summary>
        /// 加载成就进度
        /// </summary>
        public void LoadAchievementProgress()
        {
            string jsonData = PlayerPrefs.GetString("AchievementProgress", "");

            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("No achievement progress found");
                return;
            }

            try
            {
                AchievementSaveData saveData = JsonUtility.FromJson<AchievementSaveData>(jsonData);

                foreach (var progress in saveData.achievements)
                {
                    if (achievements.TryGetValue(progress.achievementId, out AchievementInstance instance))
                    {
                        instance.LoadProgress(progress);
                    }
                }

                Debug.Log("Achievement progress loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load achievement progress: {e.Message}");
            }
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
    }

    [System.Serializable]
    public class AchievementSaveData
    {
        public AchievementProgress[] achievements;
    }
}
