using UnityEngine;
using System;
using Framework.Events;
using Framework.Presentation;
using RPG.Simulation;

namespace RPG.Core
{
    /// <summary>
    /// 玩家进度数据快照（主线程侧，仅由 PresentationDispatcher 的 Apply* 方法写入）。
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int   level                 = 1;
        public float experience            = 0f;
        public float experienceToNextLevel = 100f;
        public int   gold                  = 0;

        public float GetExperienceProgress() =>
            experienceToNextLevel > 0f ? experience / experienceToNextLevel : 0f;

        public bool CanLevelUp() => experience >= experienceToNextLevel;

        public void AddExperience(float amount) => experience += amount;

        public void LevelUp()
        {
            level++;
            experience            -= experienceToNextLevel;
            experienceToNextLevel *= 1.5f;
        }

        public void AddGold(int amount) => gold += amount;
    }

    /// <summary>
    /// 玩家进度管理器 — 表现层所有者
    ///
    /// 职责分离（Command 模式）：
    ///   逻辑层 (<see cref="ProgressSimulation"/>): 在后台逻辑线程执行 XP/等级/金币运算，
    ///     将结果以 <see cref="Framework.Presentation.PresentationCommand"/> 结构体入队到
    ///     <see cref="Framework.Presentation.PresentationCommandQueue"/>（零 GC）。
    ///
    ///   表现层 (本类 + <see cref="Framework.Presentation.PresentationDispatcher"/>):
    ///     Dispatcher 在每帧 Update 中消费命令，调用本类的 Apply* 方法来更新快照、
    ///     触发 UI 事件和 EventBus 通知。逻辑层不持有任何表现层引用。
    ///
    /// 调用方通过 <see cref="AddExperience"/> / <see cref="AddGold"/> 向逻辑线程提交工作；
    /// 结果由 Dispatcher 在下一帧异步回写，主线程无阻塞等待。
    /// </summary>
    public class PlayerProgressManager : Singleton<PlayerProgressManager>, IPresentationProgressReceiver
    {
        // 主线程只读快照 — 仅在 Apply* 方法中写入，无需加锁
        public PlayerProgress Progress { get; private set; }

        public event Action<int>            OnLevelUp;
        public event Action<float>          OnExperienceGained;
        public event Action<int>            OnGoldGained;
        public event Action<PlayerProgress> OnProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            Progress = new PlayerProgress();
        }

        private void Start()
        {
            NotifyProgressChanged();
        }

        // ── Command handlers (called by PresentationDispatcher on main thread) ──

        /// <summary>
        /// 应用 XP 变化命令。由 <see cref="Framework.Presentation.PresentationDispatcher"/> 调用。
        /// </summary>
        public void ApplyXPGained(float amount, float currentXP, float xpToNext)
        {
            Progress.experience            = currentXP;
            Progress.experienceToNextLevel = xpToNext;

            OnExperienceGained?.Invoke(amount);
            EventBus.Publish(new PlayerXPGainedEvent(amount, currentXP, xpToNext));
            NotifyProgressChanged();
        }

        /// <summary>
        /// 应用升级命令。由 <see cref="Framework.Presentation.PresentationDispatcher"/> 调用。
        /// </summary>
        public void ApplyLevelUp(int oldLevel, int newLevel, float xpToNext)
        {
            Progress.level                 = newLevel;
            Progress.experienceToNextLevel = xpToNext;

            OnLevelUp?.Invoke(newLevel);
            EventBus.Publish(new PlayerLevelUpEvent(oldLevel, newLevel, xpToNext));
            Debug.Log($"[PlayerProgressManager] Level up: {oldLevel} → {newLevel}");
            NotifyProgressChanged();
        }

        /// <summary>
        /// 应用金币变化命令。由 <see cref="Framework.Presentation.PresentationDispatcher"/> 调用。
        /// </summary>
        public void ApplyGoldChanged(int newTotal, int delta)
        {
            Progress.gold = newTotal;

            OnGoldGained?.Invoke(delta);
            EventBus.Publish(new GoldChangedEvent(newTotal, delta));
            NotifyProgressChanged();
        }

        // ── Public API — submissions go to the logic thread ───────────────────

        /// <summary>提交经验值到逻辑线程；结果经由 PresentationDispatcher 异步回写。</summary>
        public void AddExperience(float amount)
        {
            var sim = GameSimulation.Instance;
            if (sim != null)
            {
                sim.EnqueueWork(() => sim.Progress.AddExperience(amount));
            }
            else
            {
                // 降级：无逻辑线程时在主线程直接计算
                Progress.AddExperience(amount);
                OnExperienceGained?.Invoke(amount);

                while (Progress.CanLevelUp())
                {
                    int old = Progress.level;
                    Progress.LevelUp();
                    OnLevelUp?.Invoke(Progress.level);
                    EventBus.Publish(new PlayerLevelUpEvent(old, Progress.level,
                        Progress.experienceToNextLevel));
                }

                NotifyProgressChanged();
            }
        }

        /// <summary>提交金币增量到逻辑线程；结果经由 PresentationDispatcher 异步回写。</summary>
        public void AddGold(int amount)
        {
            var sim = GameSimulation.Instance;
            if (sim != null)
            {
                sim.EnqueueWork(() => sim.Progress.AddGold(amount));
            }
            else
            {
                Progress.AddGold(amount);
                OnGoldGained?.Invoke(amount);
                EventBus.Publish(new GoldChangedEvent(Progress.gold, amount));
                NotifyProgressChanged();
            }
        }

        // ── Read accessors (main-thread snapshot) ─────────────────────────────

        public int   GetLevel()                => Progress.level;
        public float GetExperience()           => Progress.experience;
        public float GetExperienceToNextLevel() => Progress.experienceToNextLevel;
        public int   GetGold()                 => Progress.gold;

        /// <summary>重置进度（同步到逻辑层和本地快照）。</summary>
        public void ResetProgress()
        {
            Progress = new PlayerProgress();
            GameSimulation.Instance?.Progress.RestoreState(1, 0f, 100f, 0);
            NotifyProgressChanged();
        }

        public void NotifyProgressChanged() => OnProgressChanged?.Invoke(Progress);

        public void SaveProgress() => SaveSystem.Instance?.SaveGame();
    }
}
