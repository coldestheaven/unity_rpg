using System;
using Framework.Base;
using Framework.Interfaces;
using Framework.Threading;
using RPG.Simulation;
using UnityEngine;

namespace Gameplay.Combat
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(DamageInfo damageInfo);
    }

    /// <summary>
    /// Abstract base for all damageable entities.
    ///
    /// Threading architecture:
    ///   When <see cref="RPG.Simulation.GameSimulation"/> is running, damage and healing
    ///   are routed to the per-entity <see cref="HealthSimulation"/> on the logic thread.
    ///   The simulation fires events on the logic thread; this class subscribes in Awake
    ///   and marshals results back to the Unity main thread via
    ///   <see cref="MainThreadDispatcher"/>, then applies presentation effects
    ///   (health bar updates, VFX, death).
    ///
    ///   When no simulation is available (editor tests, early init), the legacy direct
    ///   path is used — all computation runs on the main thread synchronously.
    ///
    /// Chain of Responsibility:
    ///   The <see cref="DamageHandler"/> chain is still built and used for the direct
    ///   (fallback) path.  The threaded path mirrors its logic inside
    ///   <see cref="HealthSimulation.ApplyDamage"/>.
    /// </summary>
    public abstract class DamageableBase : MonoBehaviourBase, IDamageable, IKillable, IDamageReceiver
    {
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float defense = 0f;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float Defense => defense;
        public bool IsDead { get; protected set; }

        public event Action<float> OnDamaged;
        public event Action<float> OnHealed;
        public event Action OnDied;

        // ── Chain of Responsibility (fallback / direct path) ──────────────────
        private DamageHandler _damageChain;

        // ── Per-entity logic-thread health simulation ─────────────────────────
        private HealthSimulation _healthSim;

        protected override void Awake()
        {
            base.Awake();
            _damageChain = BuildDamageChain();
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

            // Create the logic-thread health state machine for this entity.
            _healthSim = new HealthSimulation(maxHealth, currentHealth);
            BindSimulationEvents();
        }

        /// <summary>
        /// Subscribe to <see cref="HealthSimulation"/> events and marshal results
        /// back to the Unity main thread.  Called once from Awake.
        /// </summary>
        private void BindSimulationEvents()
        {
            // Damage resolved on logic thread → record stats + apply presentation on main thread
            _healthSim.OnDamageResolved += (finalDmg, remaining, info) =>
            {
                // Record in global combat stats (still on logic thread — thread-safe)
                RPG.Simulation.GameSimulation.Instance?.Combat.RecordHit(finalDmg);

                MainThreadDispatcher.Dispatch(() =>
                {
                    currentHealth = remaining;
                    NotifyHealthChanged();
                    OnDamaged?.Invoke(finalDmg);
                    OnDamageTaken(finalDmg, info);
                    if (remaining <= 0f && !IsDead)
                        Die();
                });
            };

            // Death (only fires once from simulation) → record kill + Die() on main thread
            _healthSim.OnDied += _ =>
            {
                RPG.Simulation.GameSimulation.Instance?.Combat.RecordKill();

                MainThreadDispatcher.Dispatch(() =>
                {
                    if (!IsDead) Die();
                });
            };

            // Heal resolved on logic thread → record + apply presentation on main thread
            _healthSim.OnHealApplied += (amount, newHP) =>
            {
                RPG.Simulation.GameSimulation.Instance?.Combat.RecordHeal(amount);

                MainThreadDispatcher.Dispatch(() =>
                {
                    currentHealth = newHP;
                    NotifyHealthChanged();
                    OnHealed?.Invoke(amount);
                    OnHealedInternal(amount);
                });
            };
        }

        /// <summary>
        /// Override to install a custom damage processing chain for this entity type.
        /// Default chain: DefenseHandler → MinimumDamageHandler(0).
        /// Used only in the direct (no-simulation) path.
        /// </summary>
        protected virtual DamageHandler BuildDamageChain() => DamagePipeline.BuildDefault();

        public virtual void TakeDamage(float damage, Vector3 attackerPosition)
        {
            ReceiveDamage(new DamageInfo(
                damage,
                attackerPosition,
                null,
                CombatDamageType.Physical,
                CombatHitKind.Attack));
        }

        /// <summary>
        /// Entry point for all incoming damage.
        ///
        /// Threaded path: captures a data snapshot on the main thread, then enqueues
        /// the damage resolution to the logic thread.  Health changes are applied
        /// asynchronously (next dispatch cycle, typically within 1 frame).
        ///
        /// Direct path (no simulation): resolves and applies damage synchronously on
        /// the main thread using the Chain of Responsibility pipeline.
        /// </summary>
        public virtual void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!CanReceiveDamage(damageInfo)) return;

            var sim = RPG.Simulation.GameSimulation.Instance;
            if (sim != null)
            {
                // Capture all entity state on the main thread — HealthSimulation.ApplyDamage
                // runs on the logic thread and must not touch MonoBehaviour fields.
                var snapshot = new EntityCombatSnapshot(this, damageInfo);
                var healthSim = _healthSim;   // avoid closure over 'this'
                sim.EnqueueWork(() => healthSim.ApplyDamage(snapshot));
            }
            else
            {
                // Direct (synchronous) fallback — no simulation running.
                float appliedDamage = ResolveDamage(damageInfo);
                currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
                NotifyHealthChanged();
                OnDamaged?.Invoke(appliedDamage);
                OnDamageTaken(appliedDamage, damageInfo);
                if (currentHealth <= 0f) Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead) return;

            var sim = RPG.Simulation.GameSimulation.Instance;
            if (sim != null)
            {
                var healthSim = _healthSim;
                sim.EnqueueWork(() => healthSim.ApplyHeal(amount));
            }
            else
            {
                float appliedHeal = Mathf.Max(0f, amount);
                currentHealth = Mathf.Min(maxHealth, currentHealth + appliedHeal);
                NotifyHealthChanged();
                OnHealed?.Invoke(appliedHeal);
                OnHealedInternal(appliedHeal);
            }
        }

        public virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDied?.Invoke();
            OnDeathInternal();
        }

        public virtual void Revive(float healthPercent = 1f)
        {
            IsDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            _healthSim?.Restore(currentHealth, maxHealth);
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void ResetHealth()
        {
            IsDead = false;
            currentHealth = maxHealth;
            _healthSim?.ResetToFull();
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void SetMaxHealth(float value, bool restoreToMax = false)
        {
            maxHealth = Mathf.Max(1f, value);
            currentHealth = restoreToMax ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
            _healthSim?.SetMax(maxHealth, restoreToMax);
            NotifyHealthChanged();
        }

        public virtual void SetDefense(float value)
        {
            defense = Mathf.Max(0f, value);
        }

        protected virtual bool CanReceiveDamage(DamageInfo damageInfo)
        {
            // Use simulation dead state if available (may differ from local by 1 dispatch cycle).
            return !IsDead && (_healthSim == null || !_healthSim.IsDead);
        }

        /// <summary>
        /// Resolves damage using the Chain of Responsibility pipeline.
        /// Only called in the direct (no-simulation) path — do not call from logic thread.
        /// </summary>
        protected virtual float ResolveDamage(DamageInfo damageInfo)
        {
            float? result = _damageChain?.Handle(damageInfo.Amount, damageInfo, this);
            return result ?? 0f;
        }

        protected virtual void NotifyHealthChanged() { }

        protected virtual void OnDamageTaken(float damage, DamageInfo damageInfo) { }

        protected virtual void OnHealedInternal(float amount) { }

        protected virtual void OnRevived() { }

        protected virtual void OnDeathInternal() { }
    }
}
