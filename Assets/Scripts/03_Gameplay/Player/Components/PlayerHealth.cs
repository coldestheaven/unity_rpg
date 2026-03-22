using UnityEngine;
using Gameplay.Combat;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家健康系统
    /// </summary>
    public class PlayerHealth : Framework.Base.MonoBehaviourBase, Framework.Interfaces.IDamageable
    {
        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Damage")]
        [SerializeField] private float invincibilityTime = 1f;
        [SerializeField] private GameObject hitEffect;

        private bool isInvincible = false;
        private bool isDead = false;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

        protected override void Awake()
        {
            base.Awake();
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage, Vector3 attackerPosition = default)
        {
            if (isDead || isInvincible) return;

            currentHealth = Mathf.Max(0, currentHealth - (int)damage);
            OnHealthChanged?.Invoke(currentHealth);

            ApplyKnockback(attackerPosition);
            PlayHitEffect();

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

        private void ApplyKnockback(Vector3 attackerPosition)
        {
            if (attackerPosition == default) return;

            Vector3 direction = (transform.position - attackerPosition).normalized;
            GetComponent<PlayerMovement>()?.AddKnockback(direction, 5f);
        }

        private void PlayHitEffect()
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
        }

        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();

            Managers.GameStateManager.Instance?.ChangeState(Managers.GameState.GameOver);
        }

        public void Revive(int healthAmount)
        {
            isDead = false;
            currentHealth = healthAmount;
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            isInvincible = false;
            OnHealthChanged?.Invoke(currentHealth);
        }

        private System.Collections.IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Color originalColor = spriteRenderer.color;
                Color invincibleColor = new Color(1f, 0f, 0f, 0.5f);

                float blinkInterval = 0.1f;
                float elapsed = 0f;

                while (elapsed < invincibilityTime)
                {
                    spriteRenderer.color = spriteRenderer.color == invincibleColor ? originalColor : invincibleColor;
                    yield return new WaitForSeconds(blinkInterval);
                    elapsed += blinkInterval;
                }

                spriteRenderer.color = originalColor;
            }
            else
            {
                yield return new WaitForSeconds(invincibilityTime);
            }

            isInvincible = false;
        }
    }
}
