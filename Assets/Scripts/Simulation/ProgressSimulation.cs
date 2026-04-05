using System;

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
    /// IMPORTANT: The events (<see cref="OnLevelUp"/>, <see cref="OnXPGained"/>,
    /// <see cref="OnGoldChanged"/>) fire on whichever thread calls the mutating method.
    /// Subscribers that need to touch Unity objects MUST marshal work to the main thread
    /// via <see cref="Framework.Threading.MainThreadDispatcher.Dispatch"/>.
    /// </summary>
    public sealed class ProgressSimulation
    {
        private readonly object _lock = new object();

        private int _level = 1;
        private float _experience = 0f;
        private float _experienceToNextLevel = 100f;
        private int _gold = 0;

        // ── Public read-only state ────────────────────────────────────────────
        public int Level { get { lock (_lock) return _level; } }
        public float Experience { get { lock (_lock) return _experience; } }
        public float ExperienceToNextLevel { get { lock (_lock) return _experienceToNextLevel; } }
        public int Gold { get { lock (_lock) return _gold; } }
        public float ExperienceProgress
        {
            get
            {
                lock (_lock)
                    return _experienceToNextLevel > 0f ? _experience / _experienceToNextLevel : 0f;
            }
        }

        // ── Events (fire on the calling thread) ──────────────────────────────
        /// <summary>Fires when a level-up occurs. Args: (oldLevel, newLevel, newXPToNextLevel)</summary>
        public event Action<int, int, float> OnLevelUp;

        /// <summary>Fires whenever XP is added. Args: (amount, currentXP, xpToNextLevel)</summary>
        public event Action<float, float, float> OnXPGained;

        /// <summary>Fires whenever gold changes. Args: (newTotal, delta)</summary>
        public event Action<int, int> OnGoldChanged;

        // ── Mutations ─────────────────────────────────────────────────────────

        public void AddExperience(float amount)
        {
            if (amount <= 0f) return;

            Action<int, int, float> levelUpHandler;
            Action<float, float, float> xpGainedHandler;
            float xpSnapshot, xpNextSnapshot;
            int[] levelUps = null; // (oldLevel, newLevel) pairs

            lock (_lock)
            {
                _experience += amount;
                xpSnapshot = _experience;
                xpNextSnapshot = _experienceToNextLevel;
                xpGainedHandler = OnXPGained;
                levelUpHandler = OnLevelUp;

                // Process level-ups while XP overflows
                while (_experience >= _experienceToNextLevel)
                {
                    int old = _level;
                    _level++;
                    _experience -= _experienceToNextLevel;
                    _experienceToNextLevel = Math.Max(1f, _experienceToNextLevel * 1.5f);

                    // Collect for firing outside the lock
                    Array.Resize(ref levelUps, (levelUps?.Length ?? 0) + 2);
                    int idx = (levelUps.Length - 2);
                    levelUps[idx] = old;
                    levelUps[idx + 1] = _level;
                }

                xpNextSnapshot = _experienceToNextLevel;
            }

            // Fire events outside the lock to avoid deadlocks
            xpGainedHandler?.Invoke(amount, xpSnapshot, xpNextSnapshot);

            if (levelUps != null && levelUpHandler != null)
            {
                for (int i = 0; i < levelUps.Length; i += 2)
                {
                    lock (_lock)
                        xpNextSnapshot = _experienceToNextLevel;
                    levelUpHandler(levelUps[i], levelUps[i + 1], xpNextSnapshot);
                }
            }
        }

        public void AddGold(int amount)
        {
            int newTotal;
            Action<int, int> handler;

            lock (_lock)
            {
                _gold += amount;
                newTotal = _gold;
                handler = OnGoldChanged;
            }

            handler?.Invoke(newTotal, amount);
        }

        /// <summary>
        /// Restores simulation state (e.g. after loading a save file).
        /// Call from the main thread; no events are fired.
        /// </summary>
        public void RestoreState(int level, float xp, float xpToNext, int gold)
        {
            lock (_lock)
            {
                _level = Math.Max(1, level);
                _experience = Math.Max(0f, xp);
                _experienceToNextLevel = Math.Max(1f, xpToNext);
                _gold = Math.Max(0, gold);
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
