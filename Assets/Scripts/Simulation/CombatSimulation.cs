using System;

namespace RPG.Simulation
{
    /// <summary>
    /// Pure C# static helpers for combat mathematics.
    ///
    /// No Unity dependencies — all methods are stateless and thread-safe.
    /// Call from the logic thread for bulk combat resolution without blocking rendering.
    ///
    /// The Chain of Responsibility pipeline in <c>DamageHandler</c> already handles the
    /// per-entity resolution path; these helpers are for lighter, formula-only calculations
    /// (stat scaling, dodge chance, critical hits, etc.) that don't need the full chain.
    /// </summary>
    public static class CombatSimulation
    {
        // ── Damage ────────────────────────────────────────────────────────────

        /// <summary>Subtracts flat <paramref name="defense"/> from raw damage, floored at 0.</summary>
        public static float ApplyDefense(float rawDamage, float defense)
            => Math.Max(0f, rawDamage - defense);

        /// <summary>Multiplies post-defense damage by an elemental factor.</summary>
        public static float ApplyElementalMultiplier(float damage, float multiplier)
            => damage * Math.Max(0f, multiplier);

        /// <summary>
        /// Full single-hit resolution: defense reduction → elemental scaling.
        /// </summary>
        public static float ResolveDamage(float rawDamage, float defense,
                                          float elementalMultiplier = 1f)
            => ApplyElementalMultiplier(ApplyDefense(rawDamage, defense), elementalMultiplier);

        /// <summary>
        /// Returns the final damage after a critical hit roll.
        /// <paramref name="critChance"/> is in [0,1]; <paramref name="critMultiplier"/> is
        /// the extra factor applied on a crit (e.g. 1.5 = 150% damage).
        /// </summary>
        public static float ApplyCritical(float damage, float critChance,
                                          float critMultiplier, Random rng)
        {
            if (rng == null || critChance <= 0f) return damage;
            return rng.NextDouble() < critChance ? damage * Math.Max(1f, critMultiplier) : damage;
        }

        /// <summary>
        /// Applies a percentage dodge chance.  Returns 0 if the attack is dodged,
        /// otherwise returns <paramref name="damage"/> unchanged.
        /// </summary>
        public static float ApplyDodge(float damage, float dodgeChance, Random rng)
        {
            if (rng == null || dodgeChance <= 0f) return damage;
            return rng.NextDouble() < dodgeChance ? 0f : damage;
        }

        // ── Healing ───────────────────────────────────────────────────────────

        /// <summary>Clamps a heal amount so it does not exceed the remaining HP pool.</summary>
        public static float ClampHeal(float healAmount, float currentHP, float maxHP)
            => Math.Min(healAmount, Math.Max(0f, maxHP - currentHP));

        // ── Progression ───────────────────────────────────────────────────────

        /// <summary>XP required for the next level using geometric growth.</summary>
        public static float NextLevelXP(float currentRequired, float growthFactor = 1.5f)
            => Math.Max(1f, currentRequired * growthFactor);

        /// <summary>Returns true when a level-up should trigger.</summary>
        public static bool ShouldLevelUp(float currentXP, float xpToNextLevel)
            => currentXP >= xpToNextLevel;

        // ── Stat scaling ──────────────────────────────────────────────────────

        /// <summary>
        /// Linear interpolation of a stat between base and max values over levels.
        /// </summary>
        public static float ScaleStat(float baseStat, float maxStat, int level, int maxLevel)
        {
            if (maxLevel <= 1) return baseStat;
            float t = Math.Max(0f, Math.Min(1f, (float)(level - 1) / (maxLevel - 1)));
            return baseStat + (maxStat - baseStat) * t;
        }
    }
}
