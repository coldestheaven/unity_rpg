using UnityEngine;
using System;
using Framework.Events;

namespace RPG.Core
{
    /// <summary>
    /// 玩家进度数据
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int level = 1;
        public float experience = 0f;
        public float experienceToNextLevel = 100f;
        public int gold = 0;

        public float GetExperienceProgress()
        {
            return experience / experienceToNextLevel;
        }

        public bool CanLevelUp()
        {
            return experience >= experienceToNextLevel;
        }

        public void AddExperience(float amount)
        {
            experience += amount;
        }

        public void LevelUp()
        {
            level++;
            experience -= experienceToNextLevel;
            experienceToNextLevel *= 1.5f;
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }
    }

    /// <summary>
    /// 玩家进度管理器
    /// </summary>
    public class PlayerProgressManager : Singleton<PlayerProgressManager>
    {
        public PlayerProgress Progress { get; private set; }

        public event Action<int> OnLevelUp;
        public event Action<float> OnExperienceGained;
        public event Action<int> OnGoldGained;
        public event Action<PlayerProgress> OnProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            Progress = new PlayerProgress();
            NotifyProgressChanged();
        }

        /// <summary>
        /// 添加经验
        /// </summary>
        public void AddExperience(float amount)
        {
            Progress.AddExperience(amount);
            OnExperienceGained?.Invoke(amount);

            // Typed EventBus (new) + legacy string bus (backward compat)
            EventManager.Instance?.TriggerEvent("ExperienceGained", new ExperienceEventArgs
            {
                amount = amount,
                currentExperience = Progress.experience,
                experienceToNextLevel = Progress.experienceToNextLevel
            });

            Framework.Events.EventBus.Publish(new Framework.Events.PlayerXPGainedEvent
            {
                Amount = amount,
                CurrentXP = Progress.experience,
                XPToNextLevel = Progress.experienceToNextLevel
            });

            // 检查是否升级
            while (Progress.CanLevelUp())
            {
                int oldLevel = Progress.level;
                Progress.LevelUp();
                OnLevelUp?.Invoke(Progress.level);

                EventManager.Instance?.TriggerEvent("PlayerLevelUp", new LevelUpEventArgs
                {
                    level = Progress.level,
                    currentExperience = Progress.experience,
                    experienceToNextLevel = Progress.experienceToNextLevel
                });

                Framework.Events.EventBus.Publish(new Framework.Events.PlayerLevelUpEvent
                {
                    OldLevel = oldLevel,
                    NewLevel = Progress.level,
                    NewXPToNextLevel = Progress.experienceToNextLevel
                });

                Debug.Log($"Level up! Current level: {Progress.level}");
            }

            NotifyProgressChanged();
        }

        /// <summary>
        /// 添加金币
        /// </summary>
        public void AddGold(int amount)
        {
            Progress.AddGold(amount);
            OnGoldGained?.Invoke(amount);

            EventManager.Instance?.TriggerEvent("GoldGained", new GoldEventArgs
            {
                currentGold = Progress.gold,
                changeAmount = amount
            });

            Framework.Events.EventBus.Publish(new Framework.Events.GoldChangedEvent
            {
                CurrentGold = Progress.gold,
                Delta = amount
            });

            NotifyProgressChanged();
        }

        /// <summary>
        /// 获取当前等级
        /// </summary>
        public int GetLevel()
        {
            return Progress.level;
        }

        /// <summary>
        /// 获取当前经验
        /// </summary>
        public float GetExperience()
        {
            return Progress.experience;
        }

        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        public float GetExperienceToNextLevel()
        {
            return Progress.experienceToNextLevel;
        }

        /// <summary>
        /// 获取金币数量
        /// </summary>
        public int GetGold()
        {
            return Progress.gold;
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void ResetProgress()
        {
            Progress = new PlayerProgress();
            NotifyProgressChanged();
        }

        public void NotifyProgressChanged()
        {
            OnProgressChanged?.Invoke(Progress);
        }

        public void SaveProgress()
        {
            SaveSystem.Instance?.SaveGame();
        }
    }

    [Serializable]
    public class ExperienceEventArgs
    {
        public float amount;
        public float currentExperience;
        public float experienceToNextLevel;
    }

    [Serializable]
    public class LevelUpEventArgs
    {
        public int level;
        public float currentExperience;
        public float experienceToNextLevel;
    }

    [Serializable]
    public class GoldEventArgs
    {
        public int currentGold;
        public int changeAmount;
    }
}
