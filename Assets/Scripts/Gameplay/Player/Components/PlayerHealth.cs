using UnityEngine;
using Gameplay.Combat;
using RPG.Simulation;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家健康系统。
    /// 实现 <see cref="IInvincible"/> 使逻辑线程上的 <c>EntityCombatSnapshot</c>
    /// 能正确读取无敌状态，防止受击帧期间逻辑线程再次施加伤害。
    /// </summary>
    public class PlayerHealth : DamageableBase, IInvincible
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
            // 先处理本类独有的状态，再委托给基类。
            // 基类 Revive 负责：设置 IsDead=false、更新 currentHealth、
            // 同步 _healthSim.Restore（修复：旧代码不调 base 导致逻辑线程 HP 仍为死亡状态）、
            // 触发 NotifyHealthChanged 和 OnRevived。
            StopAllCoroutines();
            isInvincible = false;
            base.Revive(healthPercent);
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
            // DoT（持续伤害）不产生击退和无敌帧：
            //   - 击退 (0,0,0) 来源 → ApplyKnockback 内部已做 default 跳过判断
            //   - 但无敌帧需要在这里显式排除，否则每个 DoT tick 都会重置无敌计时
            if (damageInfo.HitKind == CombatHitKind.DamageOverTime) return;

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
            RPG.Core.GameStateManager.Instance?.EndGame();
        }
    }
}
