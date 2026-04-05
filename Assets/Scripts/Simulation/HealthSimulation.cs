using System;
using Gameplay.Combat;

namespace RPG.Simulation
{
    // ──────────────────────────────────────────────────────────────────────────
    // EntityCombatSnapshot
    //
    // A value-type snapshot of all entity-specific combat data captured on the
    // Unity main thread immediately before dispatching damage to the logic thread.
    //
    // Because MonoBehaviour fields cannot be safely read from a background thread,
    // we capture everything we need here so HealthSimulation.ApplyDamage() is
    // fully self-contained and has no Unity dependencies.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a <see cref="Gameplay.Combat.DamageableBase"/> subclass as having invincibility
    /// frames.  <see cref="EntityCombatSnapshot"/> reads this on the main thread so the
    /// invincibility state is correctly captured before the logic thread runs.
    /// </summary>
    public interface IInvincible
    {
        bool IsInvincible { get; }
    }

    /// <summary>
    /// Immutable snapshot of entity combat data captured on the Unity main thread.
    /// Pass to <see cref="HealthSimulation.ApplyDamage"/> on the logic thread.
    /// </summary>
    public readonly struct EntityCombatSnapshot
    {
        public readonly float Defense;
        public readonly bool IsInvincible;
        public readonly float ElementalMultiplier;
        public readonly DamageInfo OriginalInfo;

        /// <summary>
        /// Builds a snapshot from a <see cref="Gameplay.Combat.DamageableBase"/> instance.
        /// MUST be called on the Unity main thread.
        /// </summary>
        public EntityCombatSnapshot(DamageableBase entity, DamageInfo info)
        {
            Defense = entity.Defense;
            IsInvincible = entity is IInvincible inv && inv.IsInvincible;
            ElementalMultiplier = entity is IElementalTarget elemental
                ? elemental.GetDamageMultiplier(info.DamageType)
                : 1f;
            OriginalInfo = info;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HealthSimulation
    //
    // Pure C# health state machine — no Unity dependencies.
    // One instance per damageable entity, created in DamageableBase.Awake().
    //
    // Thread safety:
    //   All state mutations are guarded by an internal lock.
    //   Events fire on the logic thread; subscribers MUST use
    //   MainThreadDispatcher.Dispatch() before touching any Unity object.
    //
    // Lifetime:
    //   Owned by DamageableBase; no external registration needed.
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class HealthSimulation
    {
        private readonly object _lock = new object();

        private float _current;
        private float _max;
        private bool _isDead;

        // ── Public read-only state (thread-safe) ─────────────────────────────
        public float Current { get { lock (_lock) return _current; } }
        public float Max { get { lock (_lock) return _max; } }
        public bool IsDead { get { lock (_lock) return _isDead; } }
        public float HealthFraction
        {
            get
            {
                lock (_lock)
                    return _max > 0f ? _current / _max : 0f;
            }
        }

        // ── Events (fire on the LOGIC thread) ────────────────────────────────

        /// <summary>
        /// Fired when damage is successfully applied.
        /// Args: (finalDamage, remainingHP, originalInfo)
        /// Subscribers MUST marshal to the main thread.
        /// </summary>
        public event Action<float, float, DamageInfo> OnDamageResolved;

        /// <summary>
        /// Fired once when the entity first reaches 0 HP.
        /// Args: (finalDamage) — the killing blow amount.
        /// Subscribers MUST marshal to the main thread.
        /// </summary>
        public event Action<float> OnDied;

        /// <summary>
        /// Fired when healing is applied.
        /// Args: (healAmount, newHP)
        /// Subscribers MUST marshal to the main thread.
        /// </summary>
        public event Action<float, float> OnHealApplied;

        // ──────────────────────────────────────────────────────────────────────

        public HealthSimulation(float max, float initial)
        {
            _max = Math.Max(1f, max);
            _current = Math.Max(0f, Math.Min(initial, _max));
        }

        // ── Core mutations (called on the logic thread) ───────────────────────

        /// <summary>
        /// Atomically resolves damage from a pre-captured snapshot and updates health.
        ///
        /// Damage formula: max(0, rawAmount − defense) × elementalMultiplier, floored at 0.
        /// Returns immediately without effect if entity is dead or invincible.
        /// </summary>
        public void ApplyDamage(EntityCombatSnapshot snapshot)
        {
            float finalDamage;
            float newHP;
            bool died = false;

            lock (_lock)
            {
                if (_isDead) return;
                if (snapshot.IsInvincible) return;

                // Damage math — identical logic to DefenseHandler → ElementalResistanceHandler
                float afterDefense = Math.Max(0f, snapshot.OriginalInfo.Amount - snapshot.Defense);
                finalDamage = afterDefense * Math.Max(0f, snapshot.ElementalMultiplier);

                // Minimum 0 — allow full negation by armor
                finalDamage = Math.Max(0f, finalDamage);

                _current = Math.Max(0f, _current - finalDamage);
                newHP = _current;

                if (_current <= 0f && !_isDead)
                {
                    _isDead = true;
                    died = true;
                }
            }

            // Fire events outside the lock to avoid deadlocks
            OnDamageResolved?.Invoke(finalDamage, newHP, snapshot.OriginalInfo);
            if (died)
                OnDied?.Invoke(finalDamage);
        }

        /// <summary>
        /// Atomically applies a heal.  No-op if entity is dead.
        /// </summary>
        public void ApplyHeal(float amount)
        {
            if (amount <= 0f) return;

            float appliedHeal;
            float newHP;

            lock (_lock)
            {
                if (_isDead) return;
                appliedHeal = Math.Min(amount, Math.Max(0f, _max - _current));
                _current = Math.Min(_max, _current + appliedHeal);
                newHP = _current;
            }

            if (appliedHeal > 0f)
                OnHealApplied?.Invoke(appliedHeal, newHP);
        }

        // ── Synchronisation helpers (call on the main thread) ─────────────────

        /// <summary>
        /// Restores simulation state to match a revive or load.
        /// Does NOT fire any events.
        /// </summary>
        public void Restore(float current, float max)
        {
            lock (_lock)
            {
                _max = Math.Max(1f, max);
                _current = Math.Max(0f, Math.Min(current, _max));
                _isDead = _current <= 0f;
            }
        }

        /// <summary>Resets to full health and clears the dead flag.</summary>
        public void ResetToFull()
        {
            lock (_lock)
            {
                _current = _max;
                _isDead = false;
            }
        }

        /// <summary>Updates the maximum health cap.  Current HP is clamped if necessary.</summary>
        public void SetMax(float newMax, bool restoreToFull = false)
        {
            lock (_lock)
            {
                _max = Math.Max(1f, newMax);
                _current = restoreToFull ? _max : Math.Min(_current, _max);
            }
        }

        /// <summary>
        /// Applies a pre-resolved damage amount directly, bypassing the
        /// defense/elemental snapshot calculation.  Used by DoT effects whose damage
        /// is already the final resolved value.
        ///
        /// Fires <see cref="OnDamageResolved"/> and <see cref="OnDied"/> as normal.
        /// </summary>
        public void ApplyDirectRaw(float finalAmount)
        {
            if (finalAmount <= 0f) return;

            float newHP;
            bool died = false;

            lock (_lock)
            {
                if (_isDead) return;
                _current = Math.Max(0f, _current - finalAmount);
                newHP = _current;
                if (_current <= 0f && !_isDead)
                {
                    _isDead = true;
                    died = true;
                }
            }

            OnDamageResolved?.Invoke(finalAmount, newHP, default);
            if (died) OnDied?.Invoke(finalAmount);
        }
    }
}
