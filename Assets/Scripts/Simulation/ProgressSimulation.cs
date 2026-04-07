using System;
using Framework.Presentation;

namespace RPG.Simulation
{
    /// <summary>
    /// Pure C# simulation of player XP, level, and gold progression.
    ///
    /// No Unity dependencies — safe to construct and run on any thread.
    ///
    /// Thread safety:
    ///   All state mutations are guarded by an internal lock, so multiple producers
    ///   can call AddExperience / AddGold concurrently without data races.
    ///
    /// Presentation coupling:
    ///   Instead of firing C# events (which require subscribers and cause lambda-capture
    ///   GC allocations), this simulation enqueues <see cref="PresentationCommand"/>
    ///   value-types into the global <see cref="PresentationCommandQueue"/>.
    ///   The Unity main thread drains the queue each frame via
    ///   <see cref="PresentationDispatcher"/> — zero per-command GC on the logic thread.
    /// </summary>
    public sealed class ProgressSimulation
    {
        private readonly object _lock = new object();

        private int   _level                 = 1;
        private float _experience            = 0f;
        private float _experienceToNextLevel = 100f;
        private int   _gold                  = 0;

        // 预分配升级缓冲区 — 单次 AddExperience 升级超过 8 级极不可能；
        // 避免 Array.Resize 在热路径上产生 GC。
        private const int LevelUpBufferSize = 8;
        private readonly int[] _levelUpBuffer = new int[LevelUpBufferSize * 2]; // (old, new) pairs

        // ── Public read-only state (thread-safe) ──────────────────────────────
        public int   Level                 { get { lock (_lock) return _level; } }
        public float Experience            { get { lock (_lock) return _experience; } }
        public float ExperienceToNextLevel { get { lock (_lock) return _experienceToNextLevel; } }
        public int   Gold                  { get { lock (_lock) return _gold; } }
        public float ExperienceProgress
        {
            get
            {
                lock (_lock)
                    return _experienceToNextLevel > 0f ? _experience / _experienceToNextLevel : 0f;
            }
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        public void AddExperience(float amount)
        {
            if (amount <= 0f) return;

            float xpSnapshot, xpNextSnapshot;
            int levelUpsCount = 0;

            lock (_lock)
            {
                _experience   += amount;
                xpSnapshot     = _experience;
                xpNextSnapshot = _experienceToNextLevel;

                while (_experience >= _experienceToNextLevel)
                {
                    int old = _level;
                    _level++;
                    _experience            -= _experienceToNextLevel;
                    _experienceToNextLevel  = Math.Max(1f, _experienceToNextLevel * 1.5f);

                    // 写入预分配缓冲区；超出上限时静默截断（极不可能触发）
                    if (levelUpsCount < LevelUpBufferSize)
                    {
                        int idx = levelUpsCount * 2;
                        _levelUpBuffer[idx]     = old;
                        _levelUpBuffer[idx + 1] = _level;
                        levelUpsCount++;
                    }
                }

                xpNextSnapshot = _experienceToNextLevel;
            }

            // 在锁外入队 — struct 拷贝，零 GC。
            PresentationCommandQueue.Enqueue(
                PresentationCommand.XPGained(amount, xpSnapshot, xpNextSnapshot));

            for (int i = 0; i < levelUpsCount; i++)
            {
                float threshold;
                lock (_lock) threshold = _experienceToNextLevel;
                PresentationCommandQueue.Enqueue(
                    PresentationCommand.LevelUp(
                        _levelUpBuffer[i * 2], _levelUpBuffer[i * 2 + 1], threshold));
            }
        }

        public void AddGold(int amount)
        {
            int newTotal;
            lock (_lock)
            {
                _gold   += amount;
                newTotal = _gold;
            }

            PresentationCommandQueue.Enqueue(
                PresentationCommand.GoldChanged(newTotal, amount));
        }

        /// <summary>
        /// Restores simulation state (e.g. after loading a save file).
        /// Call from the main thread; no commands are enqueued.
        /// </summary>
        public void RestoreState(int level, float xp, float xpToNext, int gold)
        {
            lock (_lock)
            {
                _level                 = Math.Max(1, level);
                _experience            = Math.Max(0f, xp);
                _experienceToNextLevel = Math.Max(1f, xpToNext);
                _gold                  = Math.Max(0, gold);
            }
        }

        /// <summary>Returns an atomic snapshot of all progress values.</summary>
        public (int level, float xp, float xpToNext, int gold) GetSnapshot()
        {
            lock (_lock)
                return (_level, _experience, _experienceToNextLevel, _gold);
        }
    }
}
