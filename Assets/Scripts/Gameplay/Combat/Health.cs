using UnityEngine;

namespace Gameplay.Combat
{
    /// <summary>
    /// 通用健康组件
    /// </summary>
    public class Health : DamageableBase
    {
        [Header("Health")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private GameObject deathEffect;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

        private void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
        }

        public override void ResetHealth()
        {
            base.ResetHealth();
        }

        protected override float ResolveDamage(DamageInfo damageInfo)
        {
            return Mathf.RoundToInt(base.ResolveDamage(damageInfo));
        }

        protected override void NotifyHealthChanged()
        {
            OnHealthChanged?.Invoke(Mathf.RoundToInt(currentHealth));
        }

        protected override void OnDeathInternal()
        {
            OnDeath?.Invoke();
            PlayDeathEffect();

            if (destroyOnDeath)
            {
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
