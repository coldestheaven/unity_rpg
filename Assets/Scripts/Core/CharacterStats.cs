namespace RPG.Core
{
    [System.Serializable]
    public class CharacterStats
    {
        public int health;
        public int maxHealth;
        public int attackPower;
        public int defense;
        public float movementSpeed;
        public float attackRange;
        public float attackCooldown;

        public CharacterStats(int maxHealth, int attackPower, int defense, float movementSpeed)
        {
            this.maxHealth = maxHealth;
            this.health = maxHealth;
            this.attackPower = attackPower;
            this.defense = defense;
            this.movementSpeed = movementSpeed;
            this.attackRange = 2f;
            this.attackCooldown = 1f;
        }

        public void TakeDamage(int damage)
        {
            int actualDamage = Mathf.Max(damage - defense, 0);
            health = Mathf.Max(health - actualDamage, 0);
        }

        public void Heal(int amount)
        {
            health = Mathf.Min(health + amount, maxHealth);
        }

        public bool IsDead()
        {
            return health <= 0;
        }

        public void Reset()
        {
            health = maxHealth;
        }
    }
}
