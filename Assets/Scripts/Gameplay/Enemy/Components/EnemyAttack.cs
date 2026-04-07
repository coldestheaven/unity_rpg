using System.Collections;
using Framework.Core.Utils;
using Gameplay.Combat;
using UnityEngine;

namespace Gameplay.Enemy
{
    public class EnemyAttack : AttackComponent
    {
        [Header("Attack Shape")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float attackDuration = 0.35f;
        [SerializeField] private GameObject hitEffect;

        public float AttackRange => attackRange;

        public bool TryAttackTarget(GameObject target)
        {
            return TryAttack();
        }

        protected override void PerformAttack()
        {
            Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
            DamageInfo damageInfo = new DamageInfo(
                attackDamage,
                origin,
                gameObject,
                CombatDamageType.Physical,
                CombatHitKind.Attack);

            int count = PhysicsHelper.OverlapCircle(origin, attackRange, targetLayer);
            for (int i = 0; i < count; i++)
            {
                var hit = PhysicsHelper.Buffer[i];
                if (hit == null) continue;

                if (CombatResolver.TryApplyDamage(hit, damageInfo) && hitEffect != null)
                    Instantiate(hitEffect, hit.transform.position, Quaternion.identity);
            }

            StartCoroutine(FinishAfterDelay());
        }

        private IEnumerator FinishAfterDelay()
        {
            yield return new WaitForSeconds(attackDuration);
            FinishAttack();
        }

        public void SetTargetLayer(LayerMask layer)
        {
            targetLayer = layer;
        }

        public void SetAttackRange(float value)
        {
            attackRange = Mathf.Max(0.1f, value);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, attackRange);
        }
    }
}
