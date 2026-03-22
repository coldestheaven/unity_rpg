using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家战斗系统
    /// </summary>
    public class PlayerCombat : Framework.Base.MonoBehaviourBase
    {
        [Header("Combat Stats")]
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float attackCooldown = 0.5f;

        [Header("Attack")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private LayerMask enemyLayer;

        private bool isAttacking = false;
        private float lastAttackTime = 0f;

        public bool IsAttacking => isAttacking;
        public int CurrentDamage => baseDamage;

        public event System.Action<int> OnAttack;

        public void Attack()
        {
            if (isAttacking || Time.time < lastAttackTime + attackCooldown) return;

            isAttacking = true;
            lastAttackTime = Time.time;

            PerformAttack();

            StartCoroutine(AttackCoroutine());
        }

        private void PerformAttack()
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.TryGetComponent(out Framework.Interfaces.IDamageable damageable))
                {
                    damageable.TakeDamage(baseDamage, transform.position);
                }
            }

            OnAttack?.Invoke(baseDamage);
        }

        private System.Collections.IEnumerator AttackCoroutine()
        {
            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
        }

        public void SetAttackDamage(int damage)
        {
            baseDamage = damage;
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            }
        }
    }
}
