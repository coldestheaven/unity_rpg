using UnityEngine;
using Gameplay.Combat;
using Framework.Interfaces;

namespace RPG.Skills
{
    /// <summary>
    /// Skill strategy that applies immediate melee damage to all targets
    /// within the skill's range in the caster's facing direction.
    /// No projectile or delayed effect — damage resolves on cast.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeSkillStrategy",
                     menuName = "RPG/Skills/Strategies/Melee")]
    public class MeleeSkillStrategy : SkillExecutionStrategy
    {
        [Header("Melee Settings")]
        [Tooltip("Additional arc angle (degrees) centred on facing direction. 0 = full circle.")]
        [SerializeField] [Range(0f, 360f)] private float arcAngle = 180f;

        [Tooltip("Layer mask for targets. If 0 uses LayerMask.GetMask(\"Enemy\").")]
        [SerializeField] private LayerMask targetLayer;

        public override void Execute(SkillExecutionContext context)
        {
            if (context.Caster == null) return;

            Vector3 origin = context.Caster.position;

            LayerMask mask = targetLayer != 0
                ? targetLayer
                : context.EnemyLayer != 0
                    ? context.EnemyLayer
                    : LayerMask.GetMask("Enemy");

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin,
                context.SkillData.range, mask);

            DamageInfo info = new DamageInfo(
                context.SkillData.GetDamage(context.SkillLevel),
                origin,
                context.Caster.gameObject,
                CombatDamageTypeMapper.FromSkillDamageType(context.SkillData.damageType),
                CombatHitKind.Skill);

            foreach (var hit in hits)
            {
                if (arcAngle < 360f)
                {
                    Vector2 toTarget = (hit.transform.position - origin).normalized;
                    Vector2 facing = context.FacingDirection != Vector3.zero
                        ? (Vector2)context.FacingDirection.normalized
                        : Vector2.right;

                    float dot = Vector2.Dot(toTarget, facing);
                    float requiredDot = Mathf.Cos(arcAngle * 0.5f * Mathf.Deg2Rad);
                    if (dot < requiredDot) continue;
                }

                CombatResolver.TryApplyDamage(hit, info);
            }

            if (context.SkillData.skillEffectPrefab != null)
                Object.Destroy(
                    Object.Instantiate(context.SkillData.skillEffectPrefab, origin,
                        Quaternion.identity),
                    2f);
        }
    }
}
