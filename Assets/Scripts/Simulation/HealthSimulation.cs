using System;
using Framework.Presentation;
using Gameplay.Combat;

namespace RPG.Simulation
{
    // ──────────────────────────────────────────────────────────────────────────
    // EntityCombatSnapshot
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a <see cref="DamageableBase"/> subclass as having invincibility frames.
    /// <see cref="EntityCombatSnapshot"/> reads this on the main thread so the
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

        /// <summary>MUST be called on the Unity main thread.</summary>
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
    //
    // Presentation coupling (Command pattern):
    //   Instead of firing C# events (which require presentation subscribers and cause
    //   lambda-capture GC), this simulation writes PresentationCommands into the global
    //   PresentationCommandQueue.  The Unity main thread drains the queue each frame
    //   via PresentationDispatcher — zero coupling, zero per-command GC.
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class HealthSimulation
    {
        private readonly object _lock = new object();
        private readonly int _entityId;

        private float _current;
        private float _max;
        private bool _isDead;

        // ── Public read-only state (thread-safe) ─────────────────────────────
        public int   EntityId            => _entityId;
        public float Current             { get { lock (_lock) return _current; } }
        public float Max                 { get { lock (_lock) return _max; } }
        public bool  IsDead              { get { lock (_lock) return _isDead; } }
        public float HealthFraction
        {
            get
            {
                lock (_lock)
                    return _max > 0f ? _current / _max : 0f;
            }
        }

        /// <param name="entityId">
        /// Unity <c>GameObject.GetInstanceID()</c> — used to route presentation commands
        /// back to the correct <see cref="IEntityPresenter"/> via
        /// <see cref="EntityPresentRegistry"/>.  Pass 0 for headless/editor simulations.
        /// </param>
        public HealthSimulation(float max, float initial, int entityId = 0)
        {
            _entityId = entityId;
            _max      = Math.Max(1f, max);
            _current  = Math.Max(0f, Math.Min(initial, _max));
        }

        // ── Core mutations (called on the logic thread) ───────────────────────

        /// <summary>
        /// Atomically resolves damage from a pre-captured snapshot and updates health.
        /// Enqueues <see cref="PresCommandId.DamageResolved"/> (and
        /// <see cref="PresCommandId.EntityDied"/> if lethal) to the presentation layer.
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

                float afterDefense  = Math.Max(0f, snapshot.OriginalInfo.Amount - snapshot.Defense);
                finalDamage         = afterDefense * Math.Max(0f, snapshot.ElementalMultiplier);
                finalDamage         = Math.Max(0f, finalDamage);

                _current = Math.Max(0f, _current - finalDamage);
                newHP    = _current;

                if (_current <= 0f && !_isDead)
                {
                    _isDead = true;
                    died    = true;
                }
            }

            // Record stats on the logic thread (CombatSessionStats is thread-safe).
            GameSimulation.Instance?.Combat.RecordHit(finalDamage);

            // Enqueue presentation commands outside the lock — struct copies, zero GC.
            var info = snapshot.OriginalInfo;
            PresentationCommandQueue.Enqueue(PresentationCommand.DamageResolved(
                _entityId, finalDamage, newHP,
                info.SourcePosition.x, info.SourcePosition.y, info.SourcePosition.z,
                (int)info.DamageType, (int)info.HitKind));

            if (died)
            {
                GameSimulation.Instance?.Combat.RecordKill();
                PresentationCommandQueue.Enqueue(
                    PresentationCommand.EntityDied(_entityId, finalDamage));
            }
        }

        /// <summary>
        /// Atomically applies a heal.  No-op if entity is dead.
        /// Enqueues <see cref="PresCommandId.Healed"/>.
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
                _current    = Math.Min(_max, _current + appliedHeal);
                newHP       = _current;
            }

            if (appliedHeal > 0f)
            {
                GameSimulation.Instance?.Combat.RecordHeal(appliedHeal);
                PresentationCommandQueue.Enqueue(
                    PresentationCommand.Healed(_entityId, appliedHeal, newHP));
            }
        }

        // ── Synchronisation helpers (call on the main thread) ─────────────────

        /// <summary>Restores simulation state to match a revive or load. Does not enqueue commands.</summary>
        public void Restore(float current, float max)
        {
            lock (_lock)
            {
                _max     = Math.Max(1f, max);
                _current = Math.Max(0f, Math.Min(current, _max));
                _isDead  = _current <= 0f;
            }
        }

        /// <summary>Resets to full health and clears the dead flag. Does not enqueue commands.</summary>
        public void ResetToFull()
        {
            lock (_lock)
            {
                _current = _max;
                _isDead  = false;
            }
        }

        /// <summary>Updates the maximum health cap. Does not enqueue commands.</summary>
        public void SetMax(float newMax, bool restoreToFull = false)
        {
            lock (_lock)
            {
                _max     = Math.Max(1f, newMax);
                _current = restoreToFull ? _max : Math.Min(_current, _max);
            }
        }

        /// <summary>
        /// Applies a pre-resolved damage amount directly, bypassing defense/elemental
        /// calculation.  Used for DoT ticks whose damage was resolved at application time.
        ///
        /// Enqueues <see cref="PresCommandId.DamageResolved"/> with
        /// <c>hitKind = CombatHitKind.DamageOverTime</c>.
        /// Stats are NOT recorded here — the caller (<c>CombatStateSimulation</c>) is
        /// responsible to avoid double-counting.
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
                newHP    = _current;
                if (_current <= 0f && !_isDead)
                {
                    _isDead = true;
                    died    = true;
                }
            }

            PresentationCommandQueue.Enqueue(PresentationCommand.DamageResolved(
                _entityId, finalAmount, newHP, 0f, 0f, 0f,
                (int)CombatDamageType.Physical,
                (int)CombatHitKind.DamageOverTime));

            if (died)
            {
                GameSimulation.Instance?.Combat.RecordKill();
                PresentationCommandQueue.Enqueue(
                    PresentationCommand.EntityDied(_entityId, finalAmount));
            }
        }
    }
}
