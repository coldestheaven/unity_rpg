using UnityEngine;

namespace Gameplay.Combat
{
    // ────────────────────────────────────────────────────────────────────────────
    //  伤害数据类型定义
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>伤害元素类型。决定 <see cref="IElementalTarget"/> 的抗性计算。</summary>
    public enum CombatDamageType
    {
        /// <summary>物理伤害（近战攻击、普攻）。</summary>
        Physical,
        /// <summary>魔法伤害（通用法术）。</summary>
        Magic,
        /// <summary>火焰伤害。</summary>
        Fire,
        /// <summary>冰霜伤害。</summary>
        Ice,
        /// <summary>闪电伤害。</summary>
        Lightning,
        /// <summary>毒素伤害（常见于持续伤害效果）。</summary>
        Poison,
        /// <summary>神圣伤害。</summary>
        Holy,
        /// <summary>暗黑伤害。</summary>
        Dark,
    }

    /// <summary>伤害来源的触发方式，用于区分无敌判定、受击反馈等表现逻辑。</summary>
    public enum CombatHitKind
    {
        /// <summary>普通攻击命中。</summary>
        Attack,
        /// <summary>技能命中（可触发技能特效）。</summary>
        Skill,
        /// <summary>碰撞体接触触发（如陷阱、穿透弹体）。</summary>
        Hitbox,
        /// <summary>持续伤害（DoT）周期跳伤，不触发无敌帧。</summary>
        DamageOverTime,
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  DamageInfo — 单次伤害请求的完整数据快照
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 单次伤害请求的只读数据包，在主线程构造后传入伤害处理管线。
    ///
    /// <para>
    /// 作为 <c>readonly struct</c> 在栈上传递，不产生堆分配。<br/>
    /// 逻辑线程通过 <see cref="EntityCombatSnapshot"/> 捕获快照后，
    /// 在 <see cref="RPG.Simulation.HealthSimulation"/> 中处理。
    /// </para>
    ///
    /// 常用工厂方法：<see cref="Physical"/>、<see cref="Skill"/>、<see cref="DoT"/>。
    /// </summary>
    public readonly struct DamageInfo
    {
        // ── 字段 ──────────────────────────────────────────────────────────────

        /// <summary>原始伤害量（处理管线可进一步修改）。</summary>
        public readonly float            Amount;

        /// <summary>攻击来源的世界坐标（用于击退方向计算）。</summary>
        public readonly Vector3          SourcePosition;

        /// <summary>攻击来源对象（可为 null；仅用于表现层效果，不参与伤害计算）。</summary>
        public readonly GameObject       SourceObject;

        /// <summary>伤害元素类型。</summary>
        public readonly CombatDamageType DamageType;

        /// <summary>伤害触发方式。</summary>
        public readonly CombatHitKind    HitKind;

        /// <summary>
        /// 是否为周期性伤害（DoT）。
        /// 语义上与 <c>HitKind == DamageOverTime</c> 等价；
        /// 提供此字段供无法直接检查 HitKind 的管线节点使用。
        /// </summary>
        public readonly bool             IsPeriodic;

        // ── 派生属性 ──────────────────────────────────────────────────────────

        /// <summary>是否为持续伤害（等价于 <c>HitKind == DamageOverTime</c>）。</summary>
        public bool IsDoT => HitKind == CombatHitKind.DamageOverTime;

        // ── 构造 ──────────────────────────────────────────────────────────────

        /// <summary>完整构造函数。大多数场景建议使用下方工厂方法。</summary>
        public DamageInfo(
            float            amount,
            Vector3          sourcePosition,
            GameObject       sourceObject = null,
            CombatDamageType damageType   = CombatDamageType.Physical,
            CombatHitKind    hitKind      = CombatHitKind.Attack,
            bool             isPeriodic   = false)
        {
            Amount         = amount;
            SourcePosition = sourcePosition;
            SourceObject   = sourceObject;
            DamageType     = damageType;
            HitKind        = hitKind;
            IsPeriodic     = isPeriodic;
        }

        // ── 工厂方法 ──────────────────────────────────────────────────────────

        /// <summary>创建一次普通物理攻击伤害。</summary>
        public static DamageInfo Physical(float amount, Vector3 sourcePos, GameObject source = null)
            => new DamageInfo(amount, sourcePos, source, CombatDamageType.Physical, CombatHitKind.Attack);

        /// <summary>创建一次技能伤害。</summary>
        public static DamageInfo Skill(float amount, Vector3 sourcePos,
            CombatDamageType type = CombatDamageType.Magic, GameObject source = null)
            => new DamageInfo(amount, sourcePos, source, type, CombatHitKind.Skill);

        /// <summary>创建一次持续伤害（DoT）跳伤。</summary>
        public static DamageInfo DoT(float amount, Vector3 sourcePos,
            CombatDamageType type = CombatDamageType.Poison, GameObject source = null)
            => new DamageInfo(amount, sourcePos, source, type, CombatHitKind.DamageOverTime, isPeriodic: true);

        /// <summary>创建一次碰撞体触发伤害（陷阱、穿透弹体等）。</summary>
        public static DamageInfo Hitbox(float amount, Vector3 sourcePos,
            CombatDamageType type = CombatDamageType.Physical, GameObject source = null)
            => new DamageInfo(amount, sourcePos, source, type, CombatHitKind.Hitbox);

        // ── 调试 ──────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override string ToString()
            => $"DamageInfo(Amount={Amount:F1}, Type={DamageType}, Kind={HitKind})";
    }
}
