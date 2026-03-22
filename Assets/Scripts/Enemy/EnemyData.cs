using UnityEngine;
using RPG.Core;

namespace RPG.Enemy
{
    /// <summary>
    /// 敌人数据配置 - 使用ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "RPG/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本信息")]
        public string enemyName;
        public EnemyType enemyType;
        public Sprite enemyIcon;

        [Header("属性")]
        public int maxHealth = 100;
        public int attackDamage = 10;
        public int defense = 0;
        public float moveSpeed = 2f;

        [Header("AI设置")]
        public float detectionRange = 5f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;
        public float chaseSpeed = 3f;
        public float patrolSpeed = 1.5f;
        public bool canPatrol = true;
        public float patrolWaitTime = 2f;

        [Header("奖励")]
        public int experienceReward = 50;
        public int goldReward = 10;
        public LootTable lootTable;

        [Header("视觉反馈")]
        public GameObject deathEffectPrefab;
        public GameObject hitEffectPrefab;
        public GameObject attackEffectPrefab;

        [Header("音效")]
        public AudioClip[] spawnSounds;
        public AudioClip[] attackSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] deathSounds;

        public int GetRandomSpawnSoundIndex()
        {
            return spawnSounds.Length > 0 ? Random.Range(0, spawnSounds.Length) : -1;
        }

        public int GetRandomAttackSoundIndex()
        {
            return attackSounds.Length > 0 ? Random.Range(0, attackSounds.Length) : -1;
        }

        public int GetRandomHitSoundIndex()
        {
            return hitSounds.Length > 0 ? Random.Range(0, hitSounds.Length) : -1;
        }

        public int GetRandomDeathSoundIndex()
        {
            return deathSounds.Length > 0 ? Random.Range(0, deathSounds.Length) : -1;
        }
    }

    /// <summary>
    /// 敌人类型枚举
    /// </summary>
    public enum EnemyType
    {
        Slime,
        Goblin,
        Skeleton,
        Boss,
        Elite,
        Flying,
        Ranged
    }

    /// <summary>
    /// 掉落表
    /// </summary>
    [System.Serializable]
    public class LootTable
    {
        public LootItem[] possibleDrops;

        public RPG.Items.ItemData GetRandomDrop()
        {
            if (possibleDrops == null || possibleDrops.Length == 0)
                return null;

            float totalWeight = 0f;
            foreach (var drop in possibleDrops)
            {
                totalWeight += drop.dropChance;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var drop in possibleDrops)
            {
                currentWeight += drop.dropChance;
                if (randomValue <= currentWeight)
                {
                    return drop.itemData;
                }
            }

            return null;
        }

        public int GetRandomDropAmount(LootItem lootItem)
        {
            return Random.Range(lootItem.minAmount, lootItem.maxAmount + 1);
        }
    }

    [System.Serializable]
    public class LootItem
    {
        public RPG.Items.ItemData itemData;
        public float dropChance = 50f;
        public int minAmount = 1;
        public int maxAmount = 1;
    }
}
