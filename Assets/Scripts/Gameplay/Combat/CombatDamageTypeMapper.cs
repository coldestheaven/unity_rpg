namespace Gameplay.Combat
{
    public static class CombatDamageTypeMapper
    {
        public static CombatDamageType FromSkillDamageType(RPG.Skills.DamageType damageType)
        {
            switch (damageType)
            {
                case RPG.Skills.DamageType.Magic:
                    return CombatDamageType.Magic;
                case RPG.Skills.DamageType.Fire:
                    return CombatDamageType.Fire;
                case RPG.Skills.DamageType.Ice:
                    return CombatDamageType.Ice;
                case RPG.Skills.DamageType.Lightning:
                    return CombatDamageType.Lightning;
                case RPG.Skills.DamageType.Poison:
                    return CombatDamageType.Poison;
                case RPG.Skills.DamageType.Holy:
                    return CombatDamageType.Holy;
                case RPG.Skills.DamageType.Dark:
                    return CombatDamageType.Dark;
                default:
                    return CombatDamageType.Physical;
            }
        }
    }
}
