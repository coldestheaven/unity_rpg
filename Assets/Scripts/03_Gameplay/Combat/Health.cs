using UnityEngine;

namespace Gameplay.Combat
{
    /// <summary>
    /// 通用健康组件
    /// </summary>
    public class Health : Framework.Base.MonoBehaviourBase, Framework.Interfaces.IDamageable, Framework.Interfaces.IKillable
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private GameObject deathEffect;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private bool isDead = false;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

        protected override void Awake()
        {
            base.Awake();
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage, Vector3 attackerPosition = default)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - (int)damage);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + (int)amount);
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void Die()
        {
            isDead = true;
            OnDeath?.Invoke();

            PlayDeathEffect();

            if (destroyOnDeath)
            {
                Destroy(gameObject, 0.5f);
            }
        }

        private void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth);
        }
    }
}
