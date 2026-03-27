using System;
using Framework.Base;
using UnityEngine;

namespace Gameplay.Combat
{
    public abstract class AttackComponent : MonoBehaviourBase
    {
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackCooldown = 0.5f;

        protected float lastAttackTime;

        public bool IsAttacking { get; protected set; }
        public float AttackDamage => attackDamage;
        public bool CanAttack => !IsAttacking && Time.time >= lastAttackTime + attackCooldown;

        public event Action<float> OnAttackStarted;
        public event Action OnAttackFinished;

        public bool TryAttack()
        {
            if (!CanAttack)
            {
                return false;
            }

            IsAttacking = true;
            lastAttackTime = Time.time;
            OnAttackStarted?.Invoke(attackDamage);
            PerformAttack();
            return true;
        }

        protected abstract void PerformAttack();

        protected void FinishAttack()
        {
            IsAttacking = false;
            OnAttackFinished?.Invoke();
        }

        public virtual void SetAttackDamage(float value)
        {
            attackDamage = Mathf.Max(0f, value);
        }

        public virtual void SetAttackCooldown(float value)
        {
            attackCooldown = Mathf.Max(0.01f, value);
        }

        public virtual void ResetAttack()
        {
            IsAttacking = false;
            lastAttackTime = 0f;
        }
    }
}
