using System;
using Core.Stats;
using RPG.Items;
using RPG.Skills;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 玩家角色数据定义 — ScriptableObject。
    ///
    /// 定义玩家的基础属性、等级成长曲线、初始装备与技能。
    /// 与 <see cref="Gameplay.Player.PlayerStatsRuntime"/> 配合：
    ///   PlayerStatsRuntime 读取此 SO 中的基础值，运行时 Buff/等级成长在上面叠加。
    ///
    /// 创建: Assets/Create → RPG/Data/Player Data
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerData", menuName = "RPG/Data/Player Data")]
    public class PlayerData : ScriptableObject
    {
        // ── 基本信息 ───────────────────────────────────────────────────────────

        [Header("基本信息")]
        public string displayName   = "英雄";
        [TextArea(2, 4)]
        public string description;
        public Sprite portrait;

        // ── 基础属性 ───────────────────────────────────────────────────────────

        [Header("基础属性")]
        public float baseMaxHealth    = 100f;
        public float baseAttackDamage = 10f;
        public float baseDefense      = 5f;
        public float baseMoveSpeed    = 4f;
        public float baseMana         = 100f;
        public float manaRegen        = 5f;   // mana per second

        // ── 等级成长 ───────────────────────────────────────────────────────────

        [Header("每级成长")]
        public float healthPerLevel    = 15f;
        public float attackPerLevel    = 2f;
        public float defensePerLevel   = 1f;
        public float moveSpeedPerLevel = 0f;
        public float manaPerLevel      = 10f;

        [Header("升级经验")]
        public float baseXpToLevel2   = 100f;
        [Tooltip("每级所需经验 = 上一级 × growthFactor")]
        public float xpGrowthFactor   = 1.5f;
        public int   maxLevel         = 50;

        // ── 初始装备 ───────────────────────────────────────────────────────────

        [Header("初始装备（直接引用资产）")]
        public WeaponData   startingWeapon;
        public ArmorData    startingChest;
        public ArmorData    startingHead;
        public ArmorData    startingLegs;
        public ArmorData    startingFeet;
        public EquipmentData startingRing;
        public EquipmentData startingAmulet;

        // ── 初始背包 ───────────────────────────────────────────────────────────

        [Header("初始背包")]
        public StartingItem[] startingInventory = Array.Empty<StartingItem>();

        // ── 初始技能 ───────────────────────────────────────────────────────────

        [Header("初始技能栏 (4格)")]
        public SkillData[] startingSkills = new SkillData[4];

        // ── 初始资源 ───────────────────────────────────────────────────────────

        [Header("初始资源")]
        public int startingGold  = 50;
        public int startingLevel = 1;

        // ── 运行时计算 ─────────────────────────────────────────────────────────

        /// <summary>返回指定等级下的最大生命值（基础 + 成长）。</summary>
        public float GetMaxHealth(int level)
            => baseMaxHealth + healthPerLevel * Mathf.Max(0, level - 1);

        /// <summary>返回指定等级下的攻击力。</summary>
        public float GetAttack(int level)
            => baseAttackDamage + attackPerLevel * Mathf.Max(0, level - 1);

        /// <summary>返回指定等级下的防御力。</summary>
        public float GetDefense(int level)
            => baseDefense + defensePerLevel * Mathf.Max(0, level - 1);

        /// <summary>计算升到 <paramref name="targetLevel"/> 所需的总经验。</summary>
        public float GetXpRequiredForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0f;
            float xp = baseXpToLevel2;
            for (int lv = 2; lv < targetLevel; lv++)
                xp *= xpGrowthFactor;
            return xp;
        }

        /// <summary>当前 <see cref="PlayerStatBlock"/> 快照（不含 Buff）。</summary>
        public PlayerStatBlock ToStatBlock(int level = 1) => new PlayerStatBlock(
            GetMaxHealth(level), GetAttack(level), GetDefense(level),
            baseMoveSpeed + moveSpeedPerLevel * Mathf.Max(0, level - 1));
    }

    // ── 初始背包条目 ───────────────────────────────────────────────────────────

    [Serializable]
    public class StartingItem
    {
        public ItemData item;
        [Min(1)] public int quantity = 1;
    }
}
