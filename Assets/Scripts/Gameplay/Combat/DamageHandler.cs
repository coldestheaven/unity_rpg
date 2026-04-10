using UnityEngine;

namespace Gameplay.Combat
{
    // ────────────────────────────────────────────────────────────────────────────
    //  职责链（Chain of Responsibility）— 伤害处理管线
    //
    //  每个 DamageHandler 节点可以：
    //    • 修改伤害值后传递给下一节点（base.Handle 或 _next?.Handle）
    //    • 返回 null 以完全取消本次伤害（吸收）
    //    • 不调用 base.Handle 以截断后续链
    //
    //  标准链示例：
    //    InvincibilityHandler → DefenseHandler
    //                               → ElementalResistanceHandler
    //                                     → MinimumDamageHandler
    //
    //  通过 DamagePipeline 工厂方法构建预置链；
    //  在 DamageableBase.BuildDamageChain() 中安装自定义链。
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 伤害处理管线节点基类。
    /// 继承此类并重写 <see cref="Handle"/> 以实现自定义伤害处理逻辑。
    /// 返回 <c>null</c> 表示伤害被完全吸收（取消）。
    /// </summary>
    public abstract class DamageHandler
    {
        private DamageHandler _next;

        /// <summary>
        /// 将 <paramref name="next"/> 追加为当前节点的后继，并返回 <paramref name="next"/>
        /// 以支持流式链式调用：<c>a.SetNext(b).SetNext(c)</c>。
        /// </summary>
        public DamageHandler SetNext(DamageHandler next)
        {
            _next = next;
            return next;
        }

        /// <summary>
        /// 处理伤害请求。默认实现将原始值透传给下一节点；
        /// 若无后继节点则直接返回当前伤害值。
        /// </summary>
        public virtual float? Handle(float damage, DamageInfo info, DamageableBase target)
            => _next?.Handle(damage, info, target) ?? damage;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  具体处理节点
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 无敌帧处理节点。
    /// 当 <c>isInvincible()</c> 返回 <c>true</c> 时取消本次伤害。
    /// 通常置于链的首端，保证优先判断无敌状态。
    /// </summary>
    public sealed class InvincibilityHandler : DamageHandler
    {
        private readonly System.Func<bool> _isInvincible;

        /// <param name="isInvincible">无敌状态查询委托（不可为 null）。</param>
        public InvincibilityHandler(System.Func<bool> isInvincible)
            => _isInvincible = isInvincible;

        /// <inheritdoc/>
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
            => _isInvincible() ? null : base.Handle(damage, info, target);
    }

    /// <summary>
    /// 防御减伤节点。
    /// 以目标的 <see cref="DamageableBase.Defense"/> 做平铺减算，伤害最低钳制到 0。
    /// </summary>
    public sealed class DefenseHandler : DamageHandler
    {
        /// <inheritdoc/>
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float reduced = Mathf.Max(0f, damage - (target?.Defense ?? 0f));
            return base.Handle(reduced, info, target);
        }
    }

    /// <summary>
    /// 元素抗性节点。
    /// 当目标实现 <see cref="IElementalTarget"/> 时，以其返回的倍率修正伤害：
    /// <c>0.5</c> = 抗性减半，<c>2.0</c> = 弱点加倍，<c>1.0</c> = 无效果。
    /// </summary>
    public sealed class ElementalResistanceHandler : DamageHandler
    {
        /// <inheritdoc/>
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float multiplier = target is IElementalTarget elemental
                ? elemental.GetDamageMultiplier(info.DamageType)
                : 1f;

            return base.Handle(damage * multiplier, info, target);
        }
    }

    /// <summary>
    /// 最小伤害保底节点。
    /// 将经过全部减伤计算后的最终值钳制到 <see cref="MinimumDamage"/>，
    /// 避免防御过高导致攻击完全无效（<see cref="MinimumDamage"/> = 0 则允许完全免伤）。
    /// </summary>
    public sealed class MinimumDamageHandler : DamageHandler
    {
        /// <summary>伤害保底值。默认 1，可运行时修改。</summary>
        public float MinimumDamage { get; set; }

        /// <param name="minimumDamage">最低伤害下限（默认 1）。</param>
        public MinimumDamageHandler(float minimumDamage = 1f)
            => MinimumDamage = minimumDamage;

        /// <inheritdoc/>
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float? result = base.Handle(damage, info, target);
            if (result == null || result <= 0f) return null;
            return Mathf.Max(MinimumDamage, result.Value);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  元素目标接口
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 使 <see cref="DamageableBase"/> 参与元素伤害倍率计算的接口。
    /// 实现此接口后，<see cref="ElementalResistanceHandler"/> 将调用
    /// <see cref="GetDamageMultiplier"/> 获取元素修正系数。
    /// </summary>
    public interface IElementalTarget
    {
        /// <summary>
        /// 返回对 <paramref name="damageType"/> 的伤害倍率。
        /// <list type="bullet">
        ///   <item><c>0.5</c> — 抗性（伤害减半）</item>
        ///   <item><c>1.0</c> — 中性（无修正）</item>
        ///   <item><c>2.0</c> — 弱点（伤害加倍）</item>
        /// </list>
        /// </summary>
        float GetDamageMultiplier(CombatDamageType damageType);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  管线工厂
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 预置伤害处理链工厂。
    /// 在 <see cref="DamageableBase.BuildDamageChain()"/> 中选用或返回自定义链。
    /// </summary>
    public static class DamagePipeline
    {
        /// <summary>
        /// 默认链：<b>DefenseHandler → MinimumDamageHandler(0)</b>。
        /// 防御可完全抵消伤害；无元素处理。适用于普通敌人。
        /// </summary>
        public static DamageHandler BuildDefault()
        {
            var defense = new DefenseHandler();
            defense.SetNext(new MinimumDamageHandler(0f));
            return defense;
        }

        /// <summary>
        /// 元素链：<b>DefenseHandler → ElementalResistanceHandler → MinimumDamageHandler(1)</b>。
        /// 适用于具有元素属性的 Boss / 特殊敌人。
        /// </summary>
        public static DamageHandler BuildWithElemental()
        {
            var defense   = new DefenseHandler();
            var elemental = new ElementalResistanceHandler();
            defense.SetNext(elemental).SetNext(new MinimumDamageHandler(1f));
            return defense;
        }

        /// <summary>
        /// 无敌链：<b>InvincibilityHandler → DefenseHandler → MinimumDamageHandler(0)</b>。
        /// 适用于需要无敌帧的玩家角色或特殊实体。
        /// </summary>
        /// <param name="isInvincible">无敌状态查询委托（通常绑定到组件字段或属性）。</param>
        public static DamageHandler BuildWithInvincibility(System.Func<bool> isInvincible)
        {
            var inv = new InvincibilityHandler(isInvincible);
            inv.SetNext(new DefenseHandler()).SetNext(new MinimumDamageHandler(0f));
            return inv;
        }
    }
}
