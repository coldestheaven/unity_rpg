namespace Framework.Presentation
{
    /// <summary>
    /// Presentation-layer contract for receiving health-related commands from the logic thread.
    ///
    /// Implement this interface on any MonoBehaviour that owns a <c>HealthSimulation</c>
    /// (typically <c>DamageableBase</c> subclasses) and register the instance with
    /// <see cref="EntityPresentRegistry"/> using the GameObject's instance ID.
    ///
    /// All methods are called on the Unity main thread by <see cref="PresentationDispatcher"/>.
    ///
    /// Damage-type / hit-kind values use the integer casts of
    /// <c>Gameplay.Combat.CombatDamageType</c> and <c>Gameplay.Combat.CombatHitKind</c>;
    /// implementers cast back to those enums as needed.
    /// </summary>
    public interface IEntityPresenter
    {
        /// <summary>
        /// Damage was resolved on the logic thread.
        /// Called before <see cref="ApplyEntityDied"/> (if the entity died this hit).
        /// </summary>
        /// <param name="finalDamage">Damage after defense and elemental multiplier.</param>
        /// <param name="remainingHP">HP after the hit.</param>
        /// <param name="srcX">Source position X (for knockback / hit-direction effects).</param>
        /// <param name="srcY">Source position Y.</param>
        /// <param name="srcZ">Source position Z.</param>
        /// <param name="damageType">Cast of <c>CombatDamageType</c> enum.</param>
        /// <param name="hitKind">Cast of <c>CombatHitKind</c> enum.</param>
        void ApplyDamageResolved(float finalDamage, float remainingHP,
            float srcX, float srcY, float srcZ, int damageType, int hitKind);

        /// <summary>Healing was applied. <paramref name="newHP"/> is the post-heal value.</summary>
        void ApplyHealed(float amount, float newHP);

        /// <summary>
        /// The entity's HP reached zero on the logic thread.
        /// Called after the corresponding <see cref="ApplyDamageResolved"/> in the same frame.
        /// </summary>
        void ApplyEntityDied(float killingDamage);

        /// <summary>
        /// A Damage-over-Time effect ticked.
        /// <paramref name="remainingTicks"/> includes the tick that just fired.
        /// </summary>
        void ApplyDoTTick(float tickDamage, int remainingTicks);
    }
}
