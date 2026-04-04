using UnityEngine;

namespace Gameplay.Combat
{
    // ──────────────────────────────────────────────────────────────────────────
    // Chain of Responsibility — Damage Processing Pipeline
    //
    // Each DamageHandler in the chain inspects or transforms the incoming damage
    // value and decides whether to pass it to the next handler or cancel it
    // (return null).  Build chains with SetNext(); the tail handler always runs last.
    //
    // Example chain:  InvincibilityHandler → DefenseHandler
    //                                              → ElementalResistanceHandler
    //                                                    → MinimumDamageHandler
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstract base for a node in the damage processing chain.
    /// Override <see cref="Handle"/> to intercept or modify damage.
    /// Return <c>null</c> to cancel (absorb) the damage entirely.
    /// </summary>
    public abstract class DamageHandler
    {
        private DamageHandler _next;

        /// <summary>Appends <paramref name="next"/> as the successor and returns it for fluent chaining.</summary>
        public DamageHandler SetNext(DamageHandler next)
        {
            _next = next;
            return next;
        }

        /// <summary>
        /// Process <paramref name="damage"/> for <paramref name="target"/>.
        /// The default implementation forwards unchanged to the next handler.
        /// </summary>
        public virtual float? Handle(float damage, DamageInfo info, DamageableBase target) =>
            _next?.Handle(damage, info, target) ?? damage;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Concrete handlers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cancels damage when the provided predicate returns <c>true</c>.
    /// Typically used to model invincibility frames.
    /// </summary>
    public sealed class InvincibilityHandler : DamageHandler
    {
        private readonly System.Func<bool> _isInvincible;

        public InvincibilityHandler(System.Func<bool> isInvincible)
        {
            _isInvincible = isInvincible;
        }

        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            if (_isInvincible != null && _isInvincible()) return null;
            return base.Handle(damage, info, target);
        }
    }

    /// <summary>
    /// Reduces damage by the target's flat defense stat (floored at 0).
    /// </summary>
    public sealed class DefenseHandler : DamageHandler
    {
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float reduced = Mathf.Max(0f, damage - (target?.Defense ?? 0f));
            return base.Handle(reduced, info, target);
        }
    }

    /// <summary>
    /// Multiplies damage by an elemental resistance/weakness factor
    /// when the target implements <see cref="IElementalTarget"/>.
    /// </summary>
    public sealed class ElementalResistanceHandler : DamageHandler
    {
        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float multiplier = target is IElementalTarget elemental
                ? elemental.GetDamageMultiplier(info.DamageType)
                : 1f;

            return base.Handle(damage * multiplier, info, target);
        }
    }

    /// <summary>
    /// Clamps the final damage to a configured minimum so no attack deals zero.
    /// Set <see cref="MinimumDamage"/> to 0 to allow full negation by armor.
    /// </summary>
    public sealed class MinimumDamageHandler : DamageHandler
    {
        public float MinimumDamage { get; set; }

        public MinimumDamageHandler(float minimumDamage = 1f)
        {
            MinimumDamage = minimumDamage;
        }

        public override float? Handle(float damage, DamageInfo info, DamageableBase target)
        {
            float? result = base.Handle(damage, info, target);
            if (result == null || result <= 0f) return null;
            return Mathf.Max(MinimumDamage, result.Value);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Interface for elemental targeting
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Implement on a <see cref="DamageableBase"/> to participate in elemental
    /// damage modification.  Return a multiplier: 0.5 = resist, 2.0 = weak, 1.0 = neutral.
    /// </summary>
    public interface IElementalTarget
    {
        float GetDamageMultiplier(CombatDamageType damageType);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Factory helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Factory methods that build commonly used handler chains.
    /// Override <c>DamageableBase.BuildDamageChain()</c> to install a custom chain.
    /// </summary>
    public static class DamagePipeline
    {
        /// <summary>
        /// Default chain: DefenseHandler → MinimumDamageHandler(0).
        /// Damage can be fully negated by defense; no elemental processing.
        /// </summary>
        public static DamageHandler BuildDefault()
        {
            var defense = new DefenseHandler();
            defense.SetNext(new MinimumDamageHandler(0f));
            return defense;
        }

        /// <summary>
        /// Full chain: DefenseHandler → ElementalResistanceHandler → MinimumDamageHandler(1).
        /// Suitable for enemies with elemental type properties.
        /// </summary>
        public static DamageHandler BuildWithElemental()
        {
            var defense = new DefenseHandler();
            var elemental = new ElementalResistanceHandler();
            defense.SetNext(elemental).SetNext(new MinimumDamageHandler(1f));
            return defense;
        }

        /// <summary>
        /// Chain with configurable invincibility:
        /// InvincibilityHandler → DefenseHandler → MinimumDamageHandler(0).
        /// </summary>
        public static DamageHandler BuildWithInvincibility(System.Func<bool> isInvincible)
        {
            var inv = new InvincibilityHandler(isInvincible);
            inv.SetNext(new DefenseHandler()).SetNext(new MinimumDamageHandler(0f));
            return inv;
        }
    }
}
