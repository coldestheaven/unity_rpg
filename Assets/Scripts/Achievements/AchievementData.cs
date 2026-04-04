using UnityEngine;
using System;
using Framework.Events;

namespace RPG.Achievements
{
    /// <summary>
    /// 成就状态
    /// </summary>
    public enum AchievementState
    {
        Locked,         // 未解锁
        Unlocked,       // 已解锁
        Claimed         // 已领取奖励
    }

    /// <summary>
    /// 成就类型
    /// </summary>
    public enum AchievementType
    {
        Combat,         // 战斗类
        Exploration,    // 探索类
        Quest,          // 任务类
        Collection,     // 收集类
        Social,         // 社交类
        Special         // 特殊类
    }

    /// <summary>
    /// 成就条件类型
    /// </summary>
    public enum AchievementConditionType
    {
        KillEnemy,              // 击杀敌人
        ReachLevel,            // 达到等级
        CompleteQuest,         // 完成任务
        CollectItem,           // 收集物品
        PlayTime,              // 游戏时长
        KillCount,             // 击杀总数
        GoldAmount,            // 金币数量
        LocationVisit          // 到达地点
    }

    /// <summary>
    /// 成就数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Achievement", menuName = "RPG/Achievements/Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Header("基本信息")]
        public string achievementId;
        public string achievementName;
        [TextArea]
        public string description;
        public Sprite icon;
        public AchievementType achievementType;

        [Header("条件")]
        public AchievementConditionType conditionType;
        public int targetValue = 1;
        public string targetId;           // 目标ID(敌人ID、任务ID等)

        [Header("奖励")]
        public int experienceReward = 0;
        public int goldReward = 0;
        public RPG.Items.ItemData[] itemRewards;

        [Header("设置")]
        public bool isHidden = false;     // 是否隐藏成就
        public bool hasNotification = true; // 是否显示通知

        public AchievementState State { get; set; }
        public int CurrentProgress { get; set; }
        public DateTime? UnlockTime { get; set; }
        public DateTime? ClaimTime { get; set; }

        /// <summary>
        /// 检查是否完成条件
        /// </summary>
        public bool CheckCondition(object[] args)
        {
            if (State != AchievementState.Locked)
            {
                return false;
            }

            bool conditionMet = false;

            switch (conditionType)
            {
                case AchievementConditionType.ReachLevel:
                    conditionMet = CheckReachLevel(args);
                    break;
                case AchievementConditionType.CompleteQuest:
                    conditionMet = CheckCompleteQuest(args);
                    break;
                case AchievementConditionType.PlayTime:
                    conditionMet = CheckPlayTime(args);
                    break;
                case AchievementConditionType.GoldAmount:
                    conditionMet = CheckGoldAmount(args);
                    break;
                default:
                    Debug.LogWarning($"Condition type {conditionType} not implemented");
                    break;
            }

            return conditionMet;
        }

        private bool CheckReachLevel(object[] args)
        {
            if (args == null || args.Length == 0 || !(args[0] is int level))
            {
                return false;
            }

            CurrentProgress = level;
            return level >= targetValue;
        }

        private bool CheckCompleteQuest(object[] args)
        {
            if (args == null || args.Length == 0 || !(args[0] is RPG.Quests.QuestEventArgs questArgs))
            {
                return false;
            }

            return questArgs.questId == targetId;
        }

        private bool CheckPlayTime(object[] args)
        {
            if (args == null || args.Length == 0 || !(args[0] is float playTime))
            {
                return false;
            }

            CurrentProgress = Mathf.FloorToInt(playTime / 60f); // 转换为分钟
            return CurrentProgress >= targetValue;
        }

        private bool CheckGoldAmount(object[] args)
        {
            if (args == null || args.Length == 0 || !(args[0] is int gold))
            {
                return false;
            }

            CurrentProgress = gold;
            return gold >= targetValue;
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public float GetProgressPercentage()
        {
            return targetValue > 0 ? (float)CurrentProgress / targetValue : 0f;
        }

        /// <summary>
        /// 解锁成就
        /// </summary>
        public void Unlock()
        {
            if (State != AchievementState.Locked)
            {
                return;
            }

            State = AchievementState.Unlocked;
            UnlockTime = DateTime.Now;

            Debug.Log($"Achievement unlocked: {achievementName}");
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        public void ClaimRewards()
        {
            if (State != AchievementState.Unlocked)
            {
                return;
            }

            State = AchievementState.Claimed;
            ClaimTime = DateTime.Now;

            Debug.Log($"Achievement rewards claimed: {achievementName}");
        }
    }

    /// <summary>
    /// 成就实例 - 运行时成就状态
    /// </summary>
    public class AchievementInstance
    {
        public AchievementData Data { get; private set; }
        public AchievementState State => Data.State;
        public int CurrentProgress => Data.CurrentProgress;

        public event Action<AchievementInstance> OnAchievementUnlocked;
        public event Action<AchievementInstance> OnProgressChanged;

        public AchievementInstance(AchievementData data)
        {
            Data = data;
            Data.State = AchievementState.Locked;
            Data.CurrentProgress = 0;
        }

        /// <summary>
        /// 检查条件
        /// </summary>
        public bool CheckCondition(object[] args)
        {
            if (Data.State != AchievementState.Locked)
            {
                return false;
            }

            bool conditionMet = Data.CheckCondition(args);

            if (conditionMet)
            {
                Unlock();
            }
            else
            {
                OnProgressChanged?.Invoke(this);
            }

            return conditionMet;
        }

        /// <summary>
        /// 解锁成就
        /// </summary>
        public void Unlock()
        {
            if (Data.State != AchievementState.Locked)
            {
                return;
            }

            Data.Unlock();
            OnAchievementUnlocked?.Invoke(this);

            EventManager.Instance?.TriggerEvent("AchievementUnlocked", new AchievementEventArgs
            {
                achievementId = Data.achievementId,
                achievementName = Data.achievementName,
                achievementType = Data.achievementType
            });
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        public bool ClaimRewards()
        {
            if (Data.State != AchievementState.Unlocked)
            {
                return false;
            }

            Data.ClaimRewards();
            GiveRewards();

            EventManager.Instance?.TriggerEvent("AchievementRewardsClaimed", new AchievementEventArgs
            {
                achievementId = Data.achievementId,
                achievementName = Data.achievementName,
                achievementType = Data.achievementType
            });

            return true;
        }

        /// <summary>
        /// 给予奖励
        /// </summary>
        private void GiveRewards()
        {
            // 经验奖励
            if (Data.experienceReward > 0)
            {
                RPG.Core.PlayerProgressManager.Instance?.AddExperience(Data.experienceReward);
            }

            // 金币奖励
            if (Data.goldReward > 0)
            {
                RPG.Core.PlayerProgressManager.Instance?.AddGold(Data.goldReward);
            }

            // 物品奖励
            if (Data.itemRewards != null)
            {
                foreach (var item in Data.itemRewards)
                {
                    if (item != null)
                    {
                        RPG.Items.ItemSystem.Instance?.AddItem(item, 1);
                    }
                }
            }
        }

        /// <summary>
        /// 加载保存的进度
        /// </summary>
        public void LoadProgress(AchievementProgress progress)
        {
            Data.State = progress.state;
            Data.CurrentProgress = progress.currentProgress;
            Data.UnlockTime = progress.unlockTime;
            Data.ClaimTime = progress.claimTime;
        }

        /// <summary>
        /// 保存进度
        /// </summary>
        public AchievementProgress SaveProgress()
        {
            return new AchievementProgress
            {
                achievementId = Data.achievementId,
                state = Data.State,
                currentProgress = Data.CurrentProgress,
                unlockTime = Data.UnlockTime,
                claimTime = Data.ClaimTime
            };
        }
    }

    [System.Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public AchievementState state;
        public int currentProgress;
        public DateTime? unlockTime;
        public DateTime? claimTime;
    }

    [System.Serializable]
    public class AchievementEventArgs
    {
        public string achievementId;
        public string achievementName;
        public AchievementType achievementType;
    }
}
