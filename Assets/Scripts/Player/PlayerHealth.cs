using UnityEngine;
using RPG.Core;
using Framework.Events;
using Framework.Interfaces;
using UI.Views;

namespace RPG.Player
{
    /// <summary>
    /// 玩家健康系统 - 重构版
    /// 使用事件系统解耦UI更新
    /// </summary>
    [RequireComponent(typeof(PlayerState))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("生命值设置")]
        public PlayerStats stats = new PlayerStats(100, 10, 0, 5f);

        [Header("受伤设置")]
        public float invincibilityTime = 1f;
        public float knockbackForce = 5f;
        public GameObject hitEffect;
        public GameObject deathEffect;

        [Header("生命条引用")]
        public HealthBar healthBar;

        private PlayerState state;
        private PlayerController controller;
        private bool isInvincible;
        private bool isDead;

        public int CurrentHealth => state?.CurrentData?.health ?? 0;
        public int MaxHealth => state?.CurrentData?.maxHealth ?? 0;
        public bool IsInvincible => isInvincible;
        public bool IsDead => isDead;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnPlayerDeath;
        public event System.Action<int> OnPlayerDamaged;

        private void Awake()
        {
            state = GetComponent<PlayerState>();
            controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            InitializeHealth();
        }

        private void InitializeHealth()
        {
            state.Initialize(stats);
            healthBar?.SetHealth(CurrentHealth, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        public void TakeDamage(int damage, Vector2 attackerPosition)
        {
            if (isInvincible || isDead) return;

            int actualDamage = Mathf.Max(damage - state.CurrentData.defense, 0);
            state.ModifyHealth(-actualDamage);

            healthBar?.SetHealth(CurrentHealth, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);
            OnPlayerDamaged?.Invoke(actualDamage);

            ApplyKnockback(attackerPosition);
            PlayHitEffect();
            StartCoroutine(InvincibilityCoroutine());

            if (state.IsDead())
            {
                Die();
            }

            EventManager.Instance?.TriggerEvent("PlayerHealthChanged", new HealthChangedEventArgs
            {
                currentHealth = CurrentHealth,
                maxHealth = MaxHealth
            });
        }

        public void Heal(int amount)
        {
            if (isDead) return;

            state.ModifyHealth(amount);
            healthBar?.SetHealth(CurrentHealth, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);

            EventManager.Instance?.TriggerEvent("PlayerHealthChanged", new HealthChangedEventArgs
            {
                currentHealth = CurrentHealth,
                maxHealth = MaxHealth
            });
        }

        private void ApplyKnockback(Vector2 attackerPosition)
        {
            if (controller != null)
            {
                Vector2 direction = ((Vector2)transform.position - attackerPosition).normalized;
                controller.Knockback(direction, knockbackForce);
            }
        }

        private void PlayHitEffect()
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
        }

        private System.Collections.IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
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

        private void Die()
        {
            isDead = true;
            OnPlayerDeath?.Invoke();

            PlayDeathEffect();

            EventManager.Instance?.TriggerEvent("PlayerDied", null);

            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }

            GameManager.Instance?.GameOver();
        }

        private void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
        }

        public void Revive(int healthAmount)
        {
            isDead = false;
            state.ModifyHealth(healthAmount);
            healthBar?.SetHealth(CurrentHealth, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);

            EventManager.Instance?.TriggerEvent("PlayerHealthChanged", new HealthChangedEventArgs
            {
                currentHealth = CurrentHealth,
                maxHealth = MaxHealth
            });
        }

        public void ResetHealth()
        {
            state.ModifyHealth(MaxHealth);
            healthBar?.SetHealth(CurrentHealth, MaxHealth);
            isInvincible = false;
            isDead = false;
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        public void SetInvincible(bool value)
        {
            isInvincible = value;
        }

        public bool IsAlive()
        {
            return !isDead;
        }
    }

    [System.Serializable]
    public class HealthChangedEventArgs
    {
        public int currentHealth;
        public int maxHealth;
    }
}
