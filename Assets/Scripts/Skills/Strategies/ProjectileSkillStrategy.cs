using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills
{
    /// <summary>
    /// Skill strategy that spawns a directional projectile prefab.
    /// The spawned object is expected to carry a <see cref="SkillEffect"/> component
    /// (e.g. <see cref="ProjectileEffect"/>) that handles collision and damage.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileSkillStrategy",
                     menuName = "RPG/Skills/Strategies/Projectile")]
    public class ProjectileSkillStrategy : SkillExecutionStrategy
    {
        [Header("Projectile Settings")]
        [Tooltip("Prefab containing a ProjectileEffect component. Falls back to SkillData.skillEffectPrefab.")]
        [SerializeField] private GameObject overridePrefab;

        [Tooltip("Lifetime override. 0 = use ProjectileEffect.lifetime.")]
        [SerializeField] private float lifetimeOverride = 0f;

        public override void Execute(SkillExecutionContext context)
        {
            GameObject prefab = overridePrefab != null
                ? overridePrefab
                : context.SkillData.skillEffectPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[ProjectileSkillStrategy] No prefab set for skill '{context.SkillData.skillName}'.");
                return;
            }

            Vector3 spawnPos = context.Caster != null
                ? context.Caster.position
                : Vector3.zero;

            Quaternion rotation = context.FacingDirection != Vector3.zero
                ? Quaternion.LookRotation(Vector3.forward, context.FacingDirection) *
                  Quaternion.Euler(0f, 0f, -90f)
                : Quaternion.identity;

            GameObject effect = Object.Instantiate(prefab, spawnPos, rotation);

            var skillEffect = effect.GetComponent<SkillEffect>();
            if (skillEffect != null)
                skillEffect.Initialize(context.SkillData, context.Caster, context.SkillLevel);

            float destroyAfter = lifetimeOverride > 0f ? lifetimeOverride : 5f;
            Object.Destroy(effect, destroyAfter);
        }
    }
}
