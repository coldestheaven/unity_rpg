using UnityEngine;

namespace RPG.Skills
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        Active,         // 主动技能
        Passive,        // 被动技能
        Ultimate,       // 终极技能
        Toggle          // 开关技能
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        Self,           // 自己
        Enemy,          // 敌人
        Ally,           // 盟友
        Ground,         // 地面
        Direction,      // 方向
        Area            // 范围
    }

    /// <summary>
    /// 技能数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "RPG/Skills/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("基本信息")]
        public string skillName;
        [TextArea]
        public string description;
        public Sprite icon;
        public SkillType skillType;

        [Header("属性")]
        public int level = 1;
        public int maxLevel = 10;
        public float cooldown = 10f;
        public float manaCost = 20f;

        [Header("伤害")]
        public int baseDamage = 10;
        public float damageMultiplier = 1.5f;
        public DamageType damageType = DamageType.Physical;

        [Header("范围")]
        public float range = 5f;
        public float areaRadius = 2f;
        public SkillTargetType targetType = SkillTargetType.Enemy;

        [Header("效果")]
        public GameObject skillEffectPrefab;
        public AudioClip castSound;
        public GameObject impactEffect;
        public GameObject trailEffect;

        [Header("升级")]
        public float cooldownReductionPerLevel = 0.5f;
        public int damageIncreasePerLevel = 5;
        public float manaCostIncreasePerLevel = 2f;

        [Header("快捷键")]
        public KeyCode hotkey;

        [Header("执行策略 (Strategy Pattern)")]
        [Tooltip("ScriptableObject that defines how this skill executes. " +
                 "Leave null to use the legacy skill-type switch in SkillController.")]
        public SkillExecutionStrategy executionStrategy;

        [Header("节点图 (Graph Pattern)")]
        [Tooltip("当此字段不为空时，技能执行将由节点图驱动，忽略 executionStrategy。" +
                 "可在 RPG → 技能节点图编辑器 中编辑。")]
        public RPG.Skills.Graph.SkillGraph skillGraph;

        /// <summary>
        /// 获取指定等级的伤害
        /// </summary>
        public int GetDamage(int skillLevel)
        {
            return baseDamage + (skillLevel - 1) * damageIncreasePerLevel;
        }

        /// <summary>
        /// 获取指定等级的冷却时间
        /// </summary>
        public float GetCooldown(int skillLevel)
        {
            return Mathf.Max(1f, cooldown - (skillLevel - 1) * cooldownReductionPerLevel);
        }

        /// <summary>
        /// 获取指定等级的消耗
        /// </summary>
        public float GetManaCost(int skillLevel)
        {
            return manaCost + (skillLevel - 1) * manaCostIncreasePerLevel;
        }

        /// <summary>
        /// 检查是否可以升级
        /// </summary>
        public bool CanLevelUp(int currentLevel)
        {
            return currentLevel < maxLevel;
        }
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
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

    /// <summary>
    /// 技能效果类型
    /// </summary>
    public enum SkillEffectType
    {
        Damage,         // 伤害
        Heal,           // 治疗
        Buff,           // 增益
        Debuff,         // 减益
        Stun,           // 眩晕
        Knockback,      // 击退
        Summon,         // 召唤
        Teleport        // 传送
    }
}
