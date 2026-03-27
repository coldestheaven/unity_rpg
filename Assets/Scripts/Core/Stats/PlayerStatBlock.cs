using System;

namespace Core.Stats
{
    [Serializable]
    public struct PlayerStatBlock
    {
        public float MaxHealth;
        public float AttackDamage;
        public float Defense;
        public float MoveSpeed;

        public PlayerStatBlock(float maxHealth, float attackDamage, float defense, float moveSpeed)
        {
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            Defense = defense;
            MoveSpeed = moveSpeed;
        }

        public void Add(float maxHealth, float attackDamage, float defense, float moveSpeed)
        {
            MaxHealth += maxHealth;
            AttackDamage += attackDamage;
            Defense += defense;
            MoveSpeed += moveSpeed;
        }
    }

    public interface IPlayerStatModifierSource
    {
        event Action ModifiersChanged;

        void ApplyModifiers(ref PlayerStatBlock stats);
    }
}
