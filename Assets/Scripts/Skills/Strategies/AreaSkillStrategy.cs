using UnityEngine;
using Gameplay.Combat;
using Framework.Interfaces;

namespace RPG.Skills
{
    /// <summary>
    /// Skill strategy that spawns an area effect at the caster's position.
    /// Optionally performs an immediate overlap damage burst in addition to spawning the VFX.
    /// </summary>
    [CreateAssetMenu(fileName = "AreaSkillStrategy",
                     menuName = "RPG/Skills/Strategies/Area")]
    public class AreaSkillStrategy : SkillExecutionStrategy
    {
        [Header("Area Settings")]
        [Tooltip("Apply instant damage on cast in addition to spawning the prefab.")]
        [SerializeField] private bool applyInstantDamage = false;

        [Tooltip("Layer mask for targets. If 0 uses LayerMask.GetMask(\"Enemy\").")]
        [SerializeField] private LayerMask targetLayer;

        public override void Execute(SkillExecutionContext context)
        {
            Vector3 center = context.Caster != null ? context.Caster.position : Vector3.zero;

            if (context.SkillData.skillEffectPrefab != null)
            {
                var effect = Object.Instantiate(context.SkillData.skillEffectPrefab,
                                                center, Quaternion.identity);
                var skillEffect = effect.GetComponent<SkillEffect>();
                if (skillEffect != null)
                    skillEffect.Initialize(context.SkillData, context.Caster, context.SkillLevel);
            }

            if (applyInstantDamage)
            {
                LayerMask mask = targetLayer != 0
                    ? targetLayer
                    : context.EnemyLayer != 0
                        ? context.EnemyLayer
                        : LayerMask.GetMask("Enemy");

                Collider2D[] hits = Physics2D.OverlapCircleAll(center,
                    context.SkillData.areaRadius, mask);

                DamageInfo info = new DamageInfo(
                    context.SkillData.GetDamage(context.SkillLevel),
                    center,
                    context.Caster != null ? context.Caster.gameObject : null,
                    CombatDamageTypeMapper.FromSkillDamageType(context.SkillData.damageType),
                    CombatHitKind.Skill);

                foreach (var hit in hits)
                    CombatResolver.TryApplyDamage(hit, info);
            }
        }
    }
}
