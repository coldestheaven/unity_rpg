namespace Framework.Presentation
{
    /// <summary>
    /// Zero-GC value-type command produced by the logic thread and consumed by the
    /// presentation layer on the Unity main thread.
    ///
    /// Layout: 1 byte (Id) + 3 pad + 12 (3 floats) + 8 (2 ints) = 24 bytes.
    /// Stored by value in <see cref="PresentationCommandQueue"/> — no boxing, no heap allocation.
    ///
    /// Use the static factory methods to construct commands with named payload semantics.
    /// Read the payload fields in <see cref="PresentationDispatcher"/> via the documented
    /// mapping on each factory method.
    /// </summary>
    public readonly struct PresentationCommand
    {
        public readonly PresCommandId Id;

        // Generic payload slots — which fields are meaningful depends on Id.
        // See factory-method XML docs for the slot mapping per command type.
        public readonly float F0, F1, F2;
        public readonly int   I0, I1;

        private PresentationCommand(PresCommandId id,
            float f0 = 0f, float f1 = 0f, float f2 = 0f,
            int   i0 = 0,  int   i1 = 0)
        {
            Id = id; F0 = f0; F1 = f1; F2 = f2; I0 = i0; I1 = i1;
        }

        // ── Progress factories ────────────────────────────────────────────────

        /// <summary>
        /// XP was added to the player. Payload: F0=amount, F1=currentXP, F2=xpToNext.
        /// </summary>
        public static PresentationCommand XPGained(float amount, float currentXP, float xpToNext)
            => new PresentationCommand(PresCommandId.XPGained,
                f0: amount, f1: currentXP, f2: xpToNext);

        /// <summary>
        /// The player levelled up. Payload: I0=oldLevel, I1=newLevel, F0=xpToNextLevel.
        /// </summary>
        public static PresentationCommand LevelUp(int oldLevel, int newLevel, float xpToNext)
            => new PresentationCommand(PresCommandId.LevelUp,
                f0: xpToNext, i0: oldLevel, i1: newLevel);

        /// <summary>
        /// Gold total changed. Payload: I0=newTotal, I1=delta (positive=gain, negative=spend).
        /// </summary>
        public static PresentationCommand GoldChanged(int newTotal, int delta)
            => new PresentationCommand(PresCommandId.GoldChanged,
                i0: newTotal, i1: delta);

        // ── Health factories ──────────────────────────────────────────────────

        /// <summary>
        /// Damage was resolved on an entity. Payload: I0=entityInstanceId, F0=finalDamage, F1=remainingHP.
        /// </summary>
        public static PresentationCommand DamageResolved(int entityId, float finalDamage, float remainingHP)
            => new PresentationCommand(PresCommandId.DamageResolved,
                f0: finalDamage, f1: remainingHP, i0: entityId);

        /// <summary>
        /// A heal was applied to an entity. Payload: I0=entityInstanceId, F0=healAmount, F1=newHP.
        /// </summary>
        public static PresentationCommand Healed(int entityId, float healAmount, float newHP)
            => new PresentationCommand(PresCommandId.Healed,
                f0: healAmount, f1: newHP, i0: entityId);

        /// <summary>
        /// An entity's health reached zero. Payload: I0=entityInstanceId, F0=killingDamage.
        /// </summary>
        public static PresentationCommand EntityDied(int entityId, float killingDamage)
            => new PresentationCommand(PresCommandId.EntityDied,
                f0: killingDamage, i0: entityId);

        /// <summary>
        /// A DoT effect ticked on an entity.
        /// Payload: I0=entityInstanceId, I1=remainingTicks, F0=tickDamage.
        /// </summary>
        public static PresentationCommand DoTTick(int entityId, float tickDamage, int remainingTicks)
            => new PresentationCommand(PresCommandId.DoTTick,
                f0: tickDamage, i0: entityId, i1: remainingTicks);

        // ── Skill factories ───────────────────────────────────────────────────

        /// <summary>
        /// A skill slot's cooldown changed.
        /// Payload: I0=slotIndex, F0=remainingSeconds, F1=maxCooldownSeconds.
        /// </summary>
        public static PresentationCommand SkillCooldownChanged(int slotIndex, float remaining, float maxCooldown)
            => new PresentationCommand(PresCommandId.SkillCooldownChanged,
                f0: remaining, f1: maxCooldown, i0: slotIndex);
    }
}
