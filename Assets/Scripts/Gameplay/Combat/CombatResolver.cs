using Framework.Interfaces;
using UnityEngine;

namespace Gameplay.Combat
{
    public static class CombatResolver
    {
        public static bool TryApplyDamage(Collider2D target, in DamageInfo damageInfo)
        {
            if (target == null)
            {
                return false;
            }

            if (target.TryGetComponent(out IDamageReceiver damageReceiver))
            {
                damageReceiver.ReceiveDamage(damageInfo);
                return true;
            }

            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damageInfo.Amount, damageInfo.SourcePosition);
                return true;
            }

            return false;
        }

        public static bool TryApplyDamage(GameObject target, in DamageInfo damageInfo)
        {
            if (target == null)
            {
                return false;
            }

            if (target.TryGetComponent(out IDamageReceiver damageReceiver))
            {
                damageReceiver.ReceiveDamage(damageInfo);
                return true;
            }

            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damageInfo.Amount, damageInfo.SourcePosition);
                return true;
            }

            return false;
        }

        public static bool TryApplyDamage(IDamageable target, in DamageInfo damageInfo)
        {
            if (target == null)
            {
                return false;
            }

            if (target is IDamageReceiver damageReceiver)
            {
                damageReceiver.ReceiveDamage(damageInfo);
                return true;
            }

            target.TakeDamage(damageInfo.Amount, damageInfo.SourcePosition);
            return true;
        }
    }
}
