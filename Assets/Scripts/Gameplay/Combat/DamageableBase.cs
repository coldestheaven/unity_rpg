using System;
using Framework.Base;
using Framework.Interfaces;
using UnityEngine;

namespace Gameplay.Combat
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(DamageInfo damageInfo);
    }

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

        protected override void Awake()
        {
            base.Awake();
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        }

        public virtual void TakeDamage(float damage, Vector3 attackerPosition)
        {
            ReceiveDamage(new DamageInfo(
                damage,
                attackerPosition,
                null,
                CombatDamageType.Physical,
                CombatHitKind.Attack));
        }

        public virtual void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!CanReceiveDamage(damageInfo))
            {
                return;
            }

            float appliedDamage = ResolveDamage(damageInfo);
            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            NotifyHealthChanged();
            OnDamaged?.Invoke(appliedDamage);
            OnDamageTaken(appliedDamage, damageInfo);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            float appliedHeal = Mathf.Max(0f, amount);
            currentHealth = Mathf.Min(maxHealth, currentHealth + appliedHeal);
            NotifyHealthChanged();
            OnHealed?.Invoke(appliedHeal);
            OnHealedInternal(appliedHeal);
        }

        public virtual void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            OnDied?.Invoke();
            OnDeathInternal();
        }

        public virtual void Revive(float healthPercent = 1f)
        {
            IsDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void ResetHealth()
        {
            IsDead = false;
            currentHealth = maxHealth;
            NotifyHealthChanged();
            OnRevived();
        }

        public virtual void SetMaxHealth(float value, bool restoreToMax = false)
        {
            maxHealth = Mathf.Max(1f, value);
            currentHealth = restoreToMax ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
            NotifyHealthChanged();
        }

        public virtual void SetDefense(float value)
        {
            defense = Mathf.Max(0f, value);
        }

        protected virtual bool CanReceiveDamage(DamageInfo damageInfo)
        {
            return !IsDead;
        }

        protected virtual float ResolveDamage(DamageInfo damageInfo)
        {
            return Mathf.Max(0f, damageInfo.Amount - defense);
        }

        protected virtual void NotifyHealthChanged() { }

        protected virtual void OnDamageTaken(float damage, DamageInfo damageInfo) { }

        protected virtual void OnHealedInternal(float amount) { }

        protected virtual void OnRevived() { }

        protected virtual void OnDeathInternal() { }
    }
}
