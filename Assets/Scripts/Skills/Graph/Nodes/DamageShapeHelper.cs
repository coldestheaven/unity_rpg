using System.Collections.Generic;
using UnityEngine;
using Gameplay.Combat;
using SkillDamageType = RPG.Skills.DamageType;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 形状伤害节点共用工具。
    /// 所有 *DamageNode 均依赖此类，以保证伤害计算逻辑统一。
    /// </summary>
    internal static class DamageShapeHelper
    {
        // ── 伤害结算 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 对命中列表逐一结算伤害；返回是否至少击杀了一个目标。
        /// </summary>
        internal static bool ApplyToTargets(
            IEnumerable<Collider2D> hits,
            DamageInfo info)
        {
            bool killedAny = false;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                bool wasAlive = hit.TryGetComponent<DamageableBase>(out var db) && !db.IsDead;
                CombatResolver.TryApplyDamage(hit, info);
                if (wasAlive && db != null && db.IsDead)
                    killedAny = true;
            }
            return killedAny;
        }

        // ── DamageInfo 构建 ────────────────────────────────────────────────────

        internal static DamageInfo BuildInfo(
            SkillNodeContext ctx,
            float baseDamage,
            float damageMultiplier,
            SkillDamageType damageType)
        {
            float raw = baseDamage * damageMultiplier *
                        (ctx.SkillData != null ? ctx.SkillData.damageMultiplier : 1f);
            float final = ctx.SkillData != null
                ? ctx.SkillData.GetDamage(ctx.SkillLevel) * damageMultiplier
                : raw;

            var combatType = CombatDamageTypeMapper.FromSkillDamageType(damageType);
            return new DamageInfo(final, ctx.CasterPosition, ctx.CasterObject,
                                  combatType, CombatHitKind.Skill);
        }

        // ── 朝向工具 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 返回施法者的朝向单位向量。
        /// 优先读取 SpriteRenderer.flipX（2D 横版常用约定），
        /// 降级使用 transform.right。
        /// </summary>
        internal static Vector2 GetFacing(Transform caster)
        {
            if (caster == null) return Vector2.right;
            var sr = caster.GetComponent<SpriteRenderer>();
            if (sr != null)
                return sr.flipX ? Vector2.left : Vector2.right;
            return (Vector2)caster.right;
        }

        // ── 层级掩码 ───────────────────────────────────────────────────────────

        internal static readonly int EnemyMask = LayerMask.GetMask("Enemy");
    }
}
