using System;

namespace RPG.Simulation
{
    /// <summary>
    /// Pure C# simulation of skill cooldowns and mana.
    ///
    /// No Unity dependencies — safe to tick on the logic thread.
    ///
    /// The logic thread calls <see cref="Tick"/> once per frame to decrement
    /// active cooldowns.  The main thread calls <see cref="TryActivate"/> (routed via
    /// the logic thread) and reads <see cref="GetCooldown"/> / <see cref="IsOnCooldown"/>
    /// directly (reads are lock-protected and safe from any thread).
    /// </summary>
    public sealed class SkillCooldownSimulation
    {
        private readonly object _lock = new object();
        private readonly float[] _cooldowns;
        private float _mana;
        private float _maxMana;

        public int SlotCount => _cooldowns.Length;
        public float MaxMana { get { lock (_lock) return _maxMana; } }
        public float Mana { get { lock (_lock) return _mana; } }

        // ── Events (fire on the logic thread) ────────────────────────────────
        /// <summary>Fires when a cooldown value changes. Args: (slotIndex, remainingSeconds)</summary>
        public event Action<int, float> OnCooldownChanged;

        /// <summary>Fires when mana changes. Args: (currentMana, maxMana)</summary>
        public event Action<float, float> OnManaChanged;

        public SkillCooldownSimulation(int slotCount, float maxMana = 100f)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _cooldowns = new float[slotCount];
            _mana = _maxMana = Math.Max(0f, maxMana);
        }

        // ── Called by the logic thread tick ──────────────────────────────────

        /// <summary>
        /// Decrements all active cooldowns by <paramref name="deltaTime"/> seconds.
        /// Call once per logic tick.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            Action<int, float> handler;
            lock (_lock) { handler = OnCooldownChanged; }

            for (int i = 0; i < _cooldowns.Length; i++)
            {
                float prev;
                float next;

                lock (_lock)
                {
                    prev = _cooldowns[i];
                    if (prev <= 0f) continue;
                    next = Math.Max(0f, prev - deltaTime);
                    _cooldowns[i] = next;
                }

                // Only notify if there was a meaningful change
                if (Math.Abs(prev - next) > 0.001f)
                    handler?.Invoke(i, next);
            }
        }

        // ── Called from the logic thread (via GameSimulation.EnqueueWork) ────

        /// <summary>
        /// Attempts to activate the skill in <paramref name="slot"/>.
        /// Returns <c>true</c> and starts the cooldown if the slot is ready and mana is sufficient.
        /// Thread-safe.
        /// </summary>
        public bool TryActivate(int slot, float cooldownDuration, float manaCost)
        {
            Action<int, float> cooldownHandler;
            Action<float, float> manaHandler;
            float newMana;
            float maxMana;

            lock (_lock)
            {
                if (slot < 0 || slot >= _cooldowns.Length) return false;
                if (_cooldowns[slot] > 0f) return false;
                if (_mana < manaCost) return false;

                _cooldowns[slot] = Math.Max(0f, cooldownDuration);
                _mana -= manaCost;

                newMana = _mana;
                maxMana = _maxMana;
                cooldownHandler = OnCooldownChanged;
                manaHandler = OnManaChanged;
            }

            cooldownHandler?.Invoke(slot, cooldownDuration);
            if (manaCost > 0f)
                manaHandler?.Invoke(newMana, maxMana);

            return true;
        }

        // ── Read accessors (safe from any thread) ────────────────────────────

        public float GetCooldown(int slot)
        {
            lock (_lock)
                return slot >= 0 && slot < _cooldowns.Length ? _cooldowns[slot] : 0f;
        }

        public bool IsOnCooldown(int slot)
        {
            lock (_lock)
                return slot >= 0 && slot < _cooldowns.Length && _cooldowns[slot] > 0f;
        }

        /// <summary>Returns cooldown progress in [0,1] where 1 = ready.</summary>
        public float GetCooldownProgress(int slot, float maxCooldown)
        {
            if (maxCooldown <= 0f) return 1f;
            float remaining = GetCooldown(slot);
            return remaining <= 0f ? 1f : 1f - (remaining / maxCooldown);
        }

        // ── Mutations callable from any thread ───────────────────────────────

        public void ResetCooldown(int slot)
        {
            Action<int, float> handler;
            lock (_lock)
            {
                if (slot < 0 || slot >= _cooldowns.Length) return;
                _cooldowns[slot] = 0f;
                handler = OnCooldownChanged;
            }
            handler?.Invoke(slot, 0f);
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;
            float newMana, maxMana;
            Action<float, float> handler;

            lock (_lock)
            {
                _mana = Math.Min(_maxMana, _mana + amount);
                newMana = _mana;
                maxMana = _maxMana;
                handler = OnManaChanged;
            }

            handler?.Invoke(newMana, maxMana);
        }

        public void SetMaxMana(float value)
        {
            lock (_lock)
            {
                _maxMana = Math.Max(0f, value);
                _mana = Math.Min(_mana, _maxMana);
            }
        }
    }
}
