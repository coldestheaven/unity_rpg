using UnityEngine;

namespace RPG.Skills
{
    /// <summary>
    /// Immutable context object passed to every <see cref="SkillExecutionStrategy"/>.
    /// Encapsulates all information needed to resolve a single skill cast.
    /// </summary>
    public sealed class SkillExecutionContext
    {
        public SkillData SkillData { get; }
        public int SkillLevel { get; }
        public Transform Caster { get; }
        public Vector3 FacingDirection { get; }
        public Vector3 TargetPosition { get; }
        public LayerMask EnemyLayer { get; }

        public SkillExecutionContext(
            SkillData skillData,
            int skillLevel,
            Transform caster,
            Vector3 facingDirection,
            Vector3 targetPosition = default,
            LayerMask enemyLayer = default)
        {
            SkillData = skillData;
            SkillLevel = skillLevel;
            Caster = caster;
            FacingDirection = facingDirection;
            TargetPosition = targetPosition;
            EnemyLayer = enemyLayer;
        }
    }

    /// <summary>
    /// Abstract ScriptableObject base for skill execution strategies (Strategy Pattern).
    ///
    /// Each concrete strategy encapsulates one execution behaviour (projectile, area blast,
    /// melee swing, buff application) so SkillController never needs to switch on skill type.
    ///
    /// Create concrete strategy assets via Create > RPG > Skills > Strategies.
    /// Assign the strategy asset to SkillData.executionStrategy in the Inspector.
    /// </summary>
    public abstract class SkillExecutionStrategy : ScriptableObject
    {
        /// <summary>Executes the skill effect described by <paramref name="context"/>.</summary>
        public abstract void Execute(SkillExecutionContext context);
    }
}
