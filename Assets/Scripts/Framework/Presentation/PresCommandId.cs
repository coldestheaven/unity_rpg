namespace Framework.Presentation
{
    /// <summary>
    /// Identifies the type of a <see cref="PresentationCommand"/> produced by the logic thread.
    ///
    /// Values are intentionally sequential so they can be used as switch cases without
    /// gaps; add new entries before <c>_Count</c>.
    /// </summary>
    public enum PresCommandId : byte
    {
        // ── Progress ──────────────────────────────────────────────────────────
        /// <summary>Experience was gained. F0=amount, F1=currentXP, F2=xpToNext.</summary>
        XPGained = 0,
        /// <summary>A level-up occurred. I0=oldLevel, I1=newLevel, F0=xpToNext.</summary>
        LevelUp,
        /// <summary>Gold changed. I0=newTotal, I1=delta.</summary>
        GoldChanged,

        // ── Health ────────────────────────────────────────────────────────────
        /// <summary>Damage was resolved. I0=entityId, F0=finalDamage, F1=remainingHP.</summary>
        DamageResolved,
        /// <summary>A heal was applied. I0=entityId, F0=healAmount, F1=newHP.</summary>
        Healed,
        /// <summary>An entity died. I0=entityId, F0=killingDamage.</summary>
        EntityDied,
        /// <summary>A DoT effect ticked. I0=entityId, I1=remainingTicks, F0=tickDamage.</summary>
        DoTTick,

        // ── Skills ────────────────────────────────────────────────────────────
        /// <summary>A skill cooldown changed. I0=slotIndex, F0=remaining, F1=maxCooldown.</summary>
        SkillCooldownChanged,

        _Count
    }
}
