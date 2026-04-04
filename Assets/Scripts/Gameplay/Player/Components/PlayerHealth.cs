using UnityEngine;
using Gameplay.Combat;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家健康系统
    /// </summary>
    public class PlayerHealth : DamageableBase
    {
        [Header("Damage")]
        [SerializeField] private float invincibilityTime = 1f;
        [SerializeField] private GameObject hitEffect;

        private bool isInvincible = false;

        public bool IsInvincible => isInvincible;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

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

        public override void Revive(float healthPercent = 1f)
        {
            StopAllCoroutines();
            isInvincible = false;
            IsDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            NotifyHealthChanged();
            OnRevived();
        }

        public override void ResetHealth()
        {
            StopAllCoroutines();
            isInvincible = false;
            base.ResetHealth();
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

        protected override bool CanReceiveDamage(DamageInfo damageInfo)
        {
            return base.CanReceiveDamage(damageInfo) && !isInvincible;
        }

        protected override void NotifyHealthChanged()
        {
            OnHealthChanged?.Invoke(Mathf.RoundToInt(currentHealth));
        }

        protected override void OnDamageTaken(float damage, DamageInfo damageInfo)
        {
            ApplyKnockback(damageInfo.SourcePosition);
            PlayHitEffect();
            StartCoroutine(InvincibilityCoroutine());
        }

        protected override void OnRevived()
        {
            isInvincible = false;
        }

        protected override void OnDeathInternal()
        {
            OnDeath?.Invoke();
            Managers.GameStateManager.Instance?.ChangeState(Managers.GameState.GameOver);
        }
    }
}
