using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 圆形伤害节点 — 以指定点为圆心，在半径范围内的所有敌人造成伤害。
    ///
    /// 典型用途：地震震荡、爆炸、波纹冲击。
    ///
    ///          ╭───────╮
    ///        ╭╯         ╰╮
    ///       │   ●offset  │  ← radius
    ///        ╰╮         ╭╯
    ///          ╰───────╯
    ///
    /// offset = 0 时圆心在施法者自身位置。
    /// </summary>
    [SkillNodeType("圆形伤害", "战斗/形状", "#E85050",
        tooltip: "以施法者（加偏移）为圆心，对半径内所有敌人造成伤害。可选边缘衰减。")]
    public sealed class CircleDamageNode : SkillNode
    {
        [NodeField("基础伤害", 0f, 10000f)]
        public float baseDamage = 25f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Physical;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        [NodeField("半径", 0.1f, 50f)]
        public float radius = 3f;

        /// <summary>沿施法者朝向的圆心偏移距离。</summary>
        [NodeField("前向偏移", 0f, 30f)]
        public float forwardOffset = 0f;

        /// <summary>
        /// 启用边缘衰减：圆心处满伤，边缘处乘以 <see cref="edgeDamageRatio"/>。
        /// 使爆炸感更真实。
        /// </summary>
        [NodeField("启用边缘衰减")]
        public bool falloff = false;

        [NodeField("边缘伤害比例", 0f, 1f)]
        public float edgeDamageRatio = 0.3f;

        // ── Ports ──────────────────────────────────────────────────────────────

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("done",   "完成"),
            new NodePortDefinition("onKill", "击杀时")
        };

        // ── Execute ────────────────────────────────────────────────────────────

        public override void Execute(SkillNodeContext ctx)
        {
            Vector2 facing = DamageShapeHelper.GetFacing(ctx.Caster);
            Vector2 center = (Vector2)ctx.CasterPosition + facing * forwardOffset;

            var hits = Physics2D.OverlapCircleAll(center, radius, DamageShapeHelper.EnemyMask);
            if (hits.Length == 0)
            {
                ctx.ExecuteOutputPort("done");
                return;
            }

            var baseInfo = DamageShapeHelper.BuildInfo(ctx, baseDamage, damageMultiplier, damageType);

            bool killedAny = false;
            foreach (var hit in hits)
            {
                if (hit == null) continue;

                DamageInfo info = baseInfo;

                if (falloff && radius > 0f)
                {
                    float dist  = Vector2.Distance(center, hit.bounds.center);
                    float t     = Mathf.Clamp01(dist / radius);
                    float ratio = Mathf.Lerp(1f, edgeDamageRatio, t);
                    float reducedDmg = Mathf.Max(1f, baseInfo.Amount * ratio);
                    info = new DamageInfo(
                        reducedDmg, baseInfo.SourcePosition, baseInfo.SourceObject,
                        baseInfo.DamageType, baseInfo.HitKind);
                }

                bool wasAlive = hit.TryGetComponent<DamageableBase>(out var db) && !db.IsDead;
                CombatResolver.TryApplyDamage(hit, info);
                if (wasAlive && db != null && db.IsDead)
                    killedAny = true;
            }

            ctx.ExecuteOutputPort(killedAny ? "onKill" : "done");
            if (killedAny) ctx.ExecuteOutputPort("done");
        }
    }
}
