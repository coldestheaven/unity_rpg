using Core.Stats;
using UnityEngine;

namespace RPG.Buff
{
    // ── 分类 ──────────────────────────────────────────────────────────────────

    public enum BuffCategory
    {
        Buff,       // 增益（绿）
        Debuff,     // 减益（红）
        Neutral     // 中性（灰，如护盾）
    }

    // ── 叠加模式 ──────────────────────────────────────────────────────────────

    public enum BuffStackMode
    {
        Refresh,    // 刷新持续时间（默认，适合大多数 buff）
        Stack,      // 叠层，最多 maxStacks 层，每层独立计时
        Replace,    // 用新实例替换旧实例
        Ignore      // 已存在则忽略新施加
    }

    /// <summary>
    /// Buff / Debuff 数据定义资产。
    ///
    /// 通过 <see cref="BuffController.ApplyBuff"/> 在运行时创建
    /// <see cref="BuffInstance"/> 并挂载到目标实体。
    ///
    /// 创建路径: Assets/Create → RPG/Buffs/Buff Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuff", menuName = "RPG/Buffs/Buff Data")]
    public class BuffData : ScriptableObject
    {
        // ── 基本信息 ───────────────────────────────────────────────────────────

        [Header("基本信息")]
        [Tooltip("唯一 ID，用于查询 / 叠加控制。留空则使用资产名。")]
        public string buffId;

        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public BuffCategory category = BuffCategory.Buff;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(buffId))
                buffId = name;
        }

        // ── 持续时间 ───────────────────────────────────────────────────────────

        [Header("持续时间")]
        [Tooltip("持续秒数。0 = 永久（直到被主动移除）。")]
        public float duration = 5f;

        // ── 叠加控制 ───────────────────────────────────────────────────────────

        [Header("叠加控制")]
        public BuffStackMode stackMode = BuffStackMode.Refresh;
        [Tooltip("Stack 模式下的最大叠层数。")]
        public int maxStacks = 1;

        // ── 属性修改（加算） ───────────────────────────────────────────────────

        [Header("属性修改 (加算)")]
        [Tooltip("叠加到持有者的基础属性上。负值为减益。")]
        public PlayerStatBlock statModifier;

        // ── 周期效果 ───────────────────────────────────────────────────────────

        [Header("周期效果")]
        [Tooltip("每次触发的间隔（秒）。0 = 禁用周期效果。")]
        public float tickInterval = 0f;

        [Tooltip("每次 tick 造成的伤害（>0 = DoT）。")]
        public float tickDamage = 0f;

        [Tooltip("每次 tick 恢复的生命（>0 = HoT）。")]
        public float tickHeal = 0f;

        [Tooltip("DoT 的伤害类型。")]
        public RPG.Skills.DamageType tickDamageType = RPG.Skills.DamageType.Poison;

        // ── 视觉效果 ───────────────────────────────────────────────────────────

        [Header("视觉效果")]
        [Tooltip("施加时瞬间播放一次的特效（自动销毁）。")]
        public GameObject applyEffectPrefab;

        [Tooltip("持续挂在目标身上的特效（移除 Buff 时销毁）。")]
        public GameObject persistentEffectPrefab;

        [Tooltip("Buff 到期/被移除时播放一次的特效。")]
        public GameObject removeEffectPrefab;

        [Tooltip("Buff 图标边框 / UI 叠加颜色。")]
        public Color buffColor = Color.white;

        // ── 等级成长 ───────────────────────────────────────────────────────────

        [Header("等级成长（每级额外加算）")]
        public PlayerStatBlock statPerLevel;

        [Tooltip("每级增加的 tick 伤害。")]
        public float tickDamagePerLevel = 0f;

        [Tooltip("每级增加的 tick 治疗。")]
        public float tickHealPerLevel = 0f;

        [Tooltip("每级增加的持续时间（秒）。")]
        public float durationPerLevel = 0f;

        // ── 运行时计算 ─────────────────────────────────────────────────────────

        /// <summary>返回指定等级下的持续时间。</summary>
        public float GetDuration(int level)
            => duration + durationPerLevel * Mathf.Max(0, level - 1);

        /// <summary>返回指定等级下的属性修改量。</summary>
        public PlayerStatBlock GetStatModifier(int level)
        {
            if (level <= 1) return statModifier;
            var result = statModifier;
            var perLv  = statPerLevel;
            float mult = level - 1;
            result.Add(perLv.MaxHealth * mult,
                       perLv.AttackDamage * mult,
                       perLv.Defense * mult,
                       perLv.MoveSpeed * mult);
            return result;
        }

        /// <summary>返回指定等级下的 tick 伤害。</summary>
        public float GetTickDamage(int level)
            => tickDamage + tickDamagePerLevel * Mathf.Max(0, level - 1);

        /// <summary>返回指定等级下的 tick 治疗。</summary>
        public float GetTickHeal(int level)
            => tickHeal + tickHealPerLevel * Mathf.Max(0, level - 1);
    }
}
