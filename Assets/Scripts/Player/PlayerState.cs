using UnityEngine;
using RPG.Core;

namespace RPG.Player
{
    /// <summary>
    /// 玩家状态管理器 - 负责玩家的所有状态数据
    /// </summary>
    public class PlayerState : ScriptableObject
    {
        [System.Serializable]
        public class PlayerData
        {
            public int health;
            public int maxHealth;
            public int level = 1;
            public float experience;
            public float experienceToNextLevel = 100f;
            public int gold = 0;
            public int attackPower;
            public int defense;
            public float moveSpeed;
        }

        public PlayerData CurrentData { get; private set; }

        public void Initialize(PlayerStats stats)
        {
            CurrentData = new PlayerData
            {
                health = stats.maxHealth,
                maxHealth = stats.maxHealth,
                attackPower = stats.attackPower,
                defense = stats.defense,
                moveSpeed = stats.movementSpeed
            };
        }

        public void ModifyHealth(int amount)
        {
            CurrentData.health = Mathf.Clamp(CurrentData.health + amount, 0, CurrentData.maxHealth);
        }

        public void ModifyMaxHealth(int amount)
        {
            CurrentData.maxHealth += amount;
            if (CurrentData.health > CurrentData.maxHealth)
            {
                CurrentData.health = CurrentData.maxHealth;
            }
        }

        public void AddExperience(float amount)
        {
            CurrentData.experience += amount;
        }

        public void LevelUp()
        {
            CurrentData.level++;
            CurrentData.experience -= CurrentData.experienceToNextLevel;
            CurrentData.experienceToNextLevel *= 1.5f;
        }

        public void AddGold(int amount)
        {
            CurrentData.gold += amount;
        }

        public bool IsMaxLevel()
        {
            return CurrentData.experience >= CurrentData.experienceToNextLevel;
        }

        public bool IsDead()
        {
            return CurrentData.health <= 0;
        }
    }
}
