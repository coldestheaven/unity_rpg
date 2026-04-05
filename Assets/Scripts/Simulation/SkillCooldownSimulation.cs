using System;
using Framework.Presentation;

namespace RPG.Simulation
{
    /// <summary>
    /// Pure C# simulation of skill cooldowns and mana.
    ///
    /// No Unity dependencies — safe to tick on the logic thread.
    ///
    /// Presentation coupling (Command pattern):
    ///   Instead of firing C# events that presentation subscribes to, this simulation
    ///   enqueues <see cref="PresentationCommand"/> value-types into the global
    ///   <see cref="PresentationCommandQueue"/>.  The Unity main thread drains the queue
    ///   each frame via <see cref="PresentationDispatcher"/> — zero per-command GC,
    ///   no lambda closures, no MainThreadDispatcher.Dispatch calls.
    /// </summary>
    public sealed class SkillCooldownSimulation
    {
        private readonly object _lock     = new object();
        private readonly float[] _cooldowns;
        private float _mana;
        private float _maxMana;

        public int   SlotCount => _cooldowns.Length;
        public float MaxMana   { get { lock (_lock) return _maxMana; } }
        public float Mana      { get { lock (_lock) return _mana; } }

        public SkillCooldownSimulation(int slotCount, float maxMana = 100f)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _cooldowns = new float[slotCount];
            _mana = _maxMana = Math.Max(0f, maxMana);
        }

        // ── Called by the logic thread tick ──────────────────────────────────

        /// <summary>
        /// Decrements all active cooldowns by <paramref name="deltaTime"/> seconds
        /// and enqueues <see cref="PresCommandId.SkillCooldownChanged"/> for any slot
        /// that changed meaningfully.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

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

                if (Math.Abs(prev - next) > 0.001f)
                    PresentationCommandQueue.Enqueue(
                        PresentationCommand.SkillCooldownChanged(i, next));
            }
        }

        // ── Called from the logic thread (via GameSimulation.EnqueueWork) ────

        /// <summary>
        /// Attempts to activate the skill in <paramref name="slot"/>.
        /// Returns <c>true</c> and starts the cooldown if the slot is ready and mana is sufficient.
        /// </summary>
        public bool TryActivate(int slot, float cooldownDuration, float manaCost)
        {
            float newMana;
            float maxMana;
            bool activated;

            lock (_lock)
            {
                if (slot < 0 || slot >= _cooldowns.Length) return false;
                if (_cooldowns[slot] > 0f) return false;
                if (_mana < manaCost) return false;

                _cooldowns[slot] = Math.Max(0f, cooldownDuration);
                _mana  -= manaCost;
                newMana = _mana;
                maxMana = _maxMana;
                activated = true;
            }

            if (!activated) return false;

            PresentationCommandQueue.Enqueue(
                PresentationCommand.SkillCooldownChanged(slot, cooldownDuration));

            if (manaCost > 0f)
                PresentationCommandQueue.Enqueue(
                    PresentationCommand.ManaChanged(newMana, maxMana));

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
            lock (_lock)
            {
                if (slot < 0 || slot >= _cooldowns.Length) return;
                _cooldowns[slot] = 0f;
            }
            PresentationCommandQueue.Enqueue(
                PresentationCommand.SkillCooldownChanged(slot, 0f));
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;
            float newMana, maxMana;

            lock (_lock)
            {
                _mana   = Math.Min(_maxMana, _mana + amount);
                newMana = _mana;
                maxMana = _maxMana;
            }

            PresentationCommandQueue.Enqueue(
                PresentationCommand.ManaChanged(newMana, maxMana));
        }

        public void SetMaxMana(float value)
        {
            lock (_lock)
            {
                _maxMana = Math.Max(0f, value);
                _mana    = Math.Min(_mana, _maxMana);
            }
        }
    }
}
