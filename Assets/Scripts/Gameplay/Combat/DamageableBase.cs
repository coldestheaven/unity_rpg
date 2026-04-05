using System;
using Framework.Base;
using Framework.Interfaces;
using Framework.Presentation;
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
    /// Threading architecture (Command pattern — complete logic/presentation separation):
    ///   Damage and healing are routed to the per-entity <see cref="HealthSimulation"/>
    ///   on the logic thread.  The simulation enqueues <see cref="Framework.Presentation.PresentationCommand"/>
    ///   value-types into the global <see cref="PresentationCommandQueue"/> (zero GC, zero coupling).
    ///   <see cref="Framework.Presentation.PresentationDispatcher"/> drains the queue each frame
    ///   and calls the matching <see cref="IEntityPresenter"/> methods on this class — all on
    ///   the Unity main thread, no MainThreadDispatcher.Dispatch closures required.
    ///
    /// Registration:
    ///   Awake()    → EntityPresentRegistry.Register(instanceId, this)
    ///   OnDestroy  → EntityPresentRegistry.Unregister(instanceId)
    ///
    /// Fallback (no simulation):
    ///   When <c>GameSimulation.Instance</c> is null (editor tests, early init), damage
    ///   and healing are resolved synchronously on the main thread via the Chain of
    ///   Responsibility pipeline.
    /// </summary>
    public abstract class DamageableBase : MonoBehaviourBase, IDamageable, IKillable,
                                           IDamageReceiver, IEntityPresenter
    {
        [SerializeField] protected float maxHealth     = 100f;
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float defense       = 0f;

        public float CurrentHealth => currentHealth;
        public float MaxHealth     => maxHealth;
        public float Defense       => defense;
        public bool  IsDead        { get; protected set; }

        public event Action<float> OnDamaged;
        public event Action<float> OnHealed;
        public event Action        OnDied;

        // ── Chain of Responsibility (fallback / direct path) ──────────────────
        private DamageHandler _damageChain;

        // ── Per-entity logic-thread health simulation ─────────────────────────
        private HealthSimulation _healthSim;
        private int _entityId;

        protected override void Awake()
        {
            base.Awake();
            _entityId    = gameObject.GetInstanceID();
            _damageChain = BuildDamageChain();
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

            // Create the per-entity health state machine; pass entity ID so commands
            // can be routed back to this presenter by PresentationDispatcher.
            _healthSim = new HealthSimulation(maxHealth, currentHealth, _entityId);

            // Register for receiving presentation commands routed by PresentationDispatcher.
            EntityPresentRegistry.Register(_entityId, this);
        }

        protected virtual void OnDestroy()
        {
            EntityPresentRegistry.Unregister(_entityId);
        }

        // ── IEntityPresenter — called on the main thread by PresentationDispatcher ──

        /// <inheritdoc/>
        public void ApplyDamageResolved(float finalDamage, float remainingHP,
            float srcX, float srcY, float srcZ, int damageType, int hitKind)
        {
            currentHealth = remainingHP;
            NotifyHealthChanged();
            OnDamaged?.Invoke(finalDamage);

            var info = new DamageInfo(
                finalDamage,
                new Vector3(srcX, srcY, srcZ),
                null,
                (CombatDamageType)damageType,
                (CombatHitKind)hitKind);
            OnDamageTaken(finalDamage, info);
        }

        /// <inheritdoc/>
        public void ApplyHealed(float amount, float newHP)
        {
            currentHealth = newHP;
            NotifyHealthChanged();
            OnHealed?.Invoke(amount);
            OnHealedInternal(amount);
        }

        /// <inheritdoc/>
        public void ApplyEntityDied(float killingDamage)
        {
            if (!IsDead) Die();
        }

        /// <inheritdoc/>
        public void ApplyDoTTick(float tickDamage, int remainingTicks)
        {
            OnDoTTick(tickDamage, remainingTicks);
        }

        // ── Override to install a custom damage processing chain ──────────────
        protected virtual DamageHandler BuildDamageChain() => DamagePipeline.BuildDefault();

        // ── Public damage/heal entry points ──────────────────────────────────

        public virtual void TakeDamage(float damage, Vector3 attackerPosition)
        {
            ReceiveDamage(new DamageInfo(
                damage, attackerPosition, null,
                CombatDamageType.Physical, CombatHitKind.Attack));
        }

        /// <summary>
        /// Entry point for all incoming damage.
        ///
        /// Threaded path: captures a data snapshot on the main thread, then enqueues
        /// the damage resolution to the logic thread.  The presentation update arrives
        /// asynchronously via the PresentationCommandQueue (typically within 1 frame).
        ///
        /// Direct path (no simulation): resolves and applies damage synchronously.
        /// </summary>
        public virtual void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!CanReceiveDamage(damageInfo)) return;

            var sim = RPG.Simulation.GameSimulation.Instance;
            if (sim != null)
            {
                var snapshot  = new EntityCombatSnapshot(this, damageInfo);
                var healthSim = _healthSim;
                sim.EnqueueWork(() => healthSim.ApplyDamage(snapshot));
            }
            else
            {
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
                float applied  = Mathf.Max(0f, amount);
                currentHealth  = Mathf.Min(maxHealth, currentHealth + applied);
                NotifyHealthChanged();
                OnHealed?.Invoke(applied);
                OnHealedInternal(applied);
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
            IsDead        = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            _healthSim?.Restore(currentHealth, maxHealth);
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void ResetHealth()
        {
            IsDead        = false;
            currentHealth = maxHealth;
            _healthSim?.ResetToFull();
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void SetMaxHealth(float value, bool restoreToMax = false)
        {
            maxHealth     = Mathf.Max(1f, value);
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
            // _healthSim.IsDead may be true before IsDead (async gap of ≤1 frame),
            // which correctly blocks further damage before the EntityDied command arrives.
            return !IsDead && (_healthSim == null || !_healthSim.IsDead);
        }

        protected virtual float ResolveDamage(DamageInfo damageInfo)
        {
            float? result = _damageChain?.Handle(damageInfo.Amount, damageInfo, this);
            return result ?? 0f;
        }

        protected virtual void NotifyHealthChanged()   { }
        protected virtual void OnDamageTaken(float damage, DamageInfo damageInfo) { }
        protected virtual void OnHealedInternal(float amount)  { }
        protected virtual void OnDoTTick(float tickDamage, int remainingTicks)    { }
        protected virtual void OnRevived()             { }
        protected virtual void OnDeathInternal()       { }
    }
}
