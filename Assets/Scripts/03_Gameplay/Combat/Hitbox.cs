using UnityEngine;

namespace Gameplay.Combat
{
    /// <summary>
    /// 碰撞伤害盒
    /// </summary>
    public class Hitbox : Framework.Base.MonoBehaviourBase
    {
        [Header("Hitbox")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Settings")]
        [SerializeField] private bool destroyOnHit = false;
        [SerializeField] private GameObject hitEffect;

        private Collider2D hitboxCollider;

        protected override void Awake()
        {
            base.Awake();
            hitboxCollider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & targetLayer) != 0)
            {
                if (other.TryGetComponent(out Framework.Interfaces.IDamageable damageable))
                {
                    damageable.TakeDamage(damage, transform.position);
                    OnHit(other);

                    if (destroyOnHit)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        private void OnHit(Collider2D target)
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, target.transform.position, Quaternion.identity);
            }

            if (target.TryGetComponent(out Rigidbody2D rb))
            {
                Vector2 direction = (target.transform.position - transform.position).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }

        public void SetDamage(int newDamage)
        {
            damage = newDamage;
        }

        public void SetTargetLayer(LayerMask layer)
        {
            targetLayer = layer;
        }
    }
}
