namespace Framework.Presentation
{
    /// <summary>
    /// Zero-GC value-type command produced by the logic thread and consumed by the
    /// presentation layer on the Unity main thread.
    ///
    /// Layout: 1 byte (Id) + 3 pad + 20 (5 floats) + 12 (3 ints) = 36 bytes.
    /// Stored by value in <see cref="PresentationCommandQueue"/> — no boxing, no heap allocation.
    ///
    /// Use the static factory methods to construct commands with named payload semantics.
    /// The slot mapping for each command type is documented on its factory method.
    /// </summary>
    public readonly struct PresentationCommand
    {
        public readonly PresCommandId Id;

        // Generic payload slots — which fields are meaningful depends on Id.
        // Five floats cover progress scalars, health amounts, and a full Vector3.
        // Three ints cover entity IDs, enum casts, and slot indices.
        public readonly float F0, F1, F2, F3, F4;
        public readonly int   I0, I1, I2;

        private PresentationCommand(PresCommandId id,
            float f0 = 0f, float f1 = 0f, float f2 = 0f, float f3 = 0f, float f4 = 0f,
            int   i0 = 0,  int   i1 = 0,  int   i2 = 0)
        {
            Id = id;
            F0 = f0; F1 = f1; F2 = f2; F3 = f3; F4 = f4;
            I0 = i0; I1 = i1; I2 = i2;
        }

        // ── Progress factories ────────────────────────────────────────────────

        /// <summary>F0=amount, F1=currentXP, F2=xpToNext.</summary>
        public static PresentationCommand XPGained(float amount, float currentXP, float xpToNext)
            => new PresentationCommand(PresCommandId.XPGained,
                f0: amount, f1: currentXP, f2: xpToNext);

        /// <summary>I0=oldLevel, I1=newLevel, F0=xpToNextLevel.</summary>
        public static PresentationCommand LevelUp(int oldLevel, int newLevel, float xpToNext)
            => new PresentationCommand(PresCommandId.LevelUp,
                f0: xpToNext, i0: oldLevel, i1: newLevel);

        /// <summary>I0=newTotal, I1=delta (positive=gain, negative=spend).</summary>
        public static PresentationCommand GoldChanged(int newTotal, int delta)
            => new PresentationCommand(PresCommandId.GoldChanged,
                i0: newTotal, i1: delta);

        // ── Health factories ──────────────────────────────────────────────────

        /// <summary>
        /// I0=entityId, F0=finalDamage, F1=remainingHP,
        /// F2/F3/F4=source position (x,y,z), I1=damageType (int), I2=hitKind (int).
        /// </summary>
        public static PresentationCommand DamageResolved(
            int entityId, float finalDamage, float remainingHP,
            float srcX, float srcY, float srcZ,
            int damageType, int hitKind)
            => new PresentationCommand(PresCommandId.DamageResolved,
                f0: finalDamage, f1: remainingHP, f2: srcX, f3: srcY, f4: srcZ,
                i0: entityId, i1: damageType, i2: hitKind);

        /// <summary>I0=entityId, F0=healAmount, F1=newHP.</summary>
        public static PresentationCommand Healed(int entityId, float healAmount, float newHP)
            => new PresentationCommand(PresCommandId.Healed,
                f0: healAmount, f1: newHP, i0: entityId);

        /// <summary>I0=entityId, F0=killingDamage.</summary>
        public static PresentationCommand EntityDied(int entityId, float killingDamage)
            => new PresentationCommand(PresCommandId.EntityDied,
                f0: killingDamage, i0: entityId);

        /// <summary>I0=entityId, I1=remainingTicks, F0=tickDamage.</summary>
        public static PresentationCommand DoTTick(int entityId, float tickDamage, int remainingTicks)
            => new PresentationCommand(PresCommandId.DoTTick,
                f0: tickDamage, i0: entityId, i1: remainingTicks);

        // ── Skill factories ───────────────────────────────────────────────────

        /// <summary>I0=slotIndex, F0=remainingSeconds.</summary>
        public static PresentationCommand SkillCooldownChanged(int slotIndex, float remaining)
            => new PresentationCommand(PresCommandId.SkillCooldownChanged,
                f0: remaining, i0: slotIndex);

        /// <summary>F0=currentMana, F1=maxMana.</summary>
        public static PresentationCommand ManaChanged(float current, float max)
            => new PresentationCommand(PresCommandId.ManaChanged,
                f0: current, f1: max);
    }
}
