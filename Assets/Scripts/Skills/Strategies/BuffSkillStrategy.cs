using UnityEngine;
using Core.Stats;
using Gameplay.Player;

namespace RPG.Skills
{
    /// <summary>
    /// Skill strategy that applies a timed stat buff to the caster
    /// via <see cref="PlayerBuffController"/>.
    /// Duration scales by +2 s per skill level.
    /// </summary>
    [CreateAssetMenu(fileName = "BuffSkillStrategy",
                     menuName = "RPG/Skills/Strategies/Buff")]
    public class BuffSkillStrategy : SkillExecutionStrategy
    {
        [Header("Buff Settings")]
        [Tooltip("Unique source name for this buff (prevents double-stacking).")]
        [SerializeField] private string buffId = "skill_buff";

        [SerializeField] private float duration = 10f;

        [Header("Stat Modifiers (additive to base)")]
        [SerializeField] private float attackBonus = 0f;
        [SerializeField] private float defenseBonus = 0f;
        [SerializeField] private float moveSpeedBonus = 0f;
        [SerializeField] private float maxHealthBonus = 0f;

        public override void Execute(SkillExecutionContext context)
        {
            if (context.Caster == null) return;

            var buffController = context.Caster.GetComponent<PlayerBuffController>();
            if (buffController == null)
            {
                Debug.LogWarning(
                    $"[BuffSkillStrategy] No PlayerBuffController on caster '{context.Caster.name}'.");
                return;
            }

            float scaledDuration = duration + (context.SkillLevel - 1) * 2f;

            var modifier = new PlayerStatBlock(
                maxHealth: maxHealthBonus,
                attackDamage: attackBonus,
                defense: defenseBonus,
                moveSpeed: moveSpeedBonus);

            buffController.ApplyBuff(
                $"{buffId}_lv{context.SkillLevel}",
                modifier,
                scaledDuration);

            if (context.SkillData.skillEffectPrefab != null)
            {
                Object.Destroy(
                    Object.Instantiate(context.SkillData.skillEffectPrefab,
                        context.Caster.position, Quaternion.identity),
                    scaledDuration);
            }
        }
    }
}
