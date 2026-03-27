using UnityEngine;

namespace Gameplay.Combat
{
    public enum CombatDamageType
    {
        Physical,
        Magic,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Dark
    }

    public enum CombatHitKind
    {
        Attack,
        Skill,
        Hitbox,
        DamageOverTime
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 SourcePosition;
        public readonly GameObject SourceObject;
        public readonly CombatDamageType DamageType;
        public readonly CombatHitKind HitKind;
        public readonly bool IsPeriodic;

        public DamageInfo(
            float amount,
            Vector3 sourcePosition,
            GameObject sourceObject = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatHitKind hitKind = CombatHitKind.Attack,
            bool isPeriodic = false)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            SourceObject = sourceObject;
            DamageType = damageType;
            HitKind = hitKind;
            IsPeriodic = isPeriodic;
        }
    }
}
