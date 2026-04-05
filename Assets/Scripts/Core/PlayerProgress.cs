using UnityEngine;
using System;
using Framework.Events;
using Framework.Threading;
using RPG.Simulation;

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
    /// 玩家进度管理器 — 表现层桥接器
    ///
    /// 职责分离：
    ///   逻辑层 (<see cref="ProgressSimulation"/>): 运行在后台逻辑线程，执行 XP/等级/金币运算。
    ///   表现层 (本类, MonoBehaviour): 订阅逻辑层事件，通过 <see cref="MainThreadDispatcher"/>
    ///     将结果调度回 Unity 主线程，再更新 <see cref="Progress"/> 快照并触发 UI/EventBus 通知。
    ///
    /// 调用方直接调用 <see cref="AddExperience"/> / <see cref="AddGold"/>；
    /// 内部将工作提交给 <see cref="GameSimulation"/> 的逻辑线程，主线程无阻塞等待。
    /// </summary>
    public class PlayerProgressManager : Singleton<PlayerProgressManager>
    {
        // 主线程只读快照 — 仅在 MainThreadDispatcher 回调中更新，无需加锁
        public PlayerProgress Progress { get; private set; }

        public event Action<int> OnLevelUp;
        public event Action<float> OnExperienceGained;
        public event Action<int> OnGoldGained;
        public event Action<PlayerProgress> OnProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            Progress = new PlayerProgress();
        }

        private void Start()
        {
            // Bind to the logic simulation if it already exists (started by GameManager.Awake).
            // If GameSimulation starts after this object, it will call BindToSimulation explicitly.
            if (GameSimulation.Instance != null)
                BindToSimulation(GameSimulation.Instance);

            NotifyProgressChanged();
        }

        /// <summary>
        /// Subscribes to logic-thread events on the given simulation and marshals results
        /// back to the Unity main thread.  Call once when the simulation is ready.
        /// </summary>
        public void BindToSimulation(GameSimulation sim)
        {
            if (sim == null) return;

            // XP gained — update snapshot and fire presentation events on main thread
            sim.Progress.OnXPGained += (amount, currentXP, xpToNext) =>
            {
                MainThreadDispatcher.Dispatch(() =>
                {
                    Progress.experience = currentXP;
                    Progress.experienceToNextLevel = xpToNext;
                    OnExperienceGained?.Invoke(amount);

                    Framework.Events.EventBus.Publish(new Framework.Events.PlayerXPGainedEvent
                    {
                        Amount = amount,
                        CurrentXP = currentXP,
                        XPToNextLevel = xpToNext
                    });

                    NotifyProgressChanged();
                });
            };

            // Level up — update snapshot and fire level-up events on main thread
            sim.Progress.OnLevelUp += (oldLevel, newLevel, newXPToNext) =>
            {
                MainThreadDispatcher.Dispatch(() =>
                {
                    Progress.level = newLevel;
                    Progress.experience = sim.Progress.Experience;
                    Progress.experienceToNextLevel = newXPToNext;

                    OnLevelUp?.Invoke(newLevel);

                    Framework.Events.EventBus.Publish(new Framework.Events.PlayerLevelUpEvent
                    {
                        OldLevel = oldLevel,
                        NewLevel = newLevel,
                        NewXPToNextLevel = newXPToNext
                    });

                    Debug.Log($"[PlayerProgressManager] Level up: {oldLevel} → {newLevel}");
                    NotifyProgressChanged();
                });
            };

            // Gold changed — update snapshot and fire gold events on main thread
            sim.Progress.OnGoldChanged += (newTotal, delta) =>
            {
                MainThreadDispatcher.Dispatch(() =>
                {
                    Progress.gold = newTotal;
                    OnGoldGained?.Invoke(delta);

                    Framework.Events.EventBus.Publish(new Framework.Events.GoldChangedEvent
                    {
                        CurrentGold = newTotal,
                        Delta = delta
                    });

                    NotifyProgressChanged();
                });
            };
        }

        // ── Public API — submissions go to the logic thread ───────────────────

        /// <summary>提交经验值到逻辑线程计算；结果通过主线程回调通知表现层。</summary>
        public void AddExperience(float amount)
        {
            var sim = GameSimulation.Instance;
            if (sim != null)
            {
                // Non-blocking: enqueue to logic thread
                sim.EnqueueWork(() => sim.Progress.AddExperience(amount));
            }
            else
            {
                // Fallback: direct computation on main thread (no simulation running)
                Progress.AddExperience(amount);
                OnExperienceGained?.Invoke(amount);

                while (Progress.CanLevelUp())
                {
                    int old = Progress.level;
                    Progress.LevelUp();
                    OnLevelUp?.Invoke(Progress.level);
                    Framework.Events.EventBus.Publish(new Framework.Events.PlayerLevelUpEvent
                    {
                        OldLevel = old,
                        NewLevel = Progress.level,
                        NewXPToNextLevel = Progress.experienceToNextLevel
                    });
                }

                NotifyProgressChanged();
            }
        }

        /// <summary>提交金币增量到逻辑线程计算；结果通过主线程回调通知表现层。</summary>
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

                Framework.Events.EventBus.Publish(new Framework.Events.GoldChangedEvent
                {
                    CurrentGold = Progress.gold,
                    Delta = amount
                });

                NotifyProgressChanged();
            }
        }

        // ── Read accessors (read from the main-thread snapshot) ───────────────

        public int GetLevel() => Progress.level;
        public float GetExperience() => Progress.experience;
        public float GetExperienceToNextLevel() => Progress.experienceToNextLevel;
        public int GetGold() => Progress.gold;

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
