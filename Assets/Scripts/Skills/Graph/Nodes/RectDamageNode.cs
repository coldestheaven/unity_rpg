using UnityEngine;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 矩形伤害节点 — 以施法者为原点，用旋转矩形检测范围内的敌人并造成伤害。
    ///
    /// 典型用途：横向斩击、冲锋踩踏、地面裂缝。
    ///
    ///         ┌──────────────────────────┐
    ///         │                          │
    ///   ──────┼──────────────────────────┤  ← height
    ///  caster │  ← offset →  ■ center    │
    ///   ──────┼──────────────────────────┤
    ///         │                          │
    ///         └──────────────────────────┘
    ///                  width
    /// </summary>
    [SkillNodeType("矩形伤害", "战斗/形状", "#E87050",
        tooltip: "以施法者朝向旋转矩形区域检测敌人并造成伤害。")]
    public sealed class RectDamageNode : SkillNode
    {
        [NodeField("基础伤害", 0f, 10000f)]
        public float baseDamage = 20f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Physical;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        [NodeField("矩形宽度 (X)", 0.1f, 50f)]
        public float width = 3f;

        [NodeField("矩形高度 (Y)", 0.1f, 50f)]
        public float height = 1.5f;

        /// <summary>矩形中心相对施法者的前向偏移距离。</summary>
        [NodeField("前向偏移", 0f, 30f)]
        public float forwardOffset = 1.5f;

        /// <summary>额外旋转角度（叠加在施法者朝向上）。</summary>
        [NodeField("额外旋转角", -180f, 180f)]
        public float extraAngleDeg = 0f;

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
            Vector2 facing  = DamageShapeHelper.GetFacing(ctx.Caster);
            float baseAngle = Vector2.SignedAngle(Vector2.right, facing);
            float totalAngle = baseAngle + extraAngleDeg;

            // Rotate offset vector to align with facing direction
            float rad = totalAngle * Mathf.Deg2Rad;
            Vector2 forwardDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 center = (Vector2)ctx.CasterPosition + forwardDir * forwardOffset;

            var hits = Physics2D.OverlapBoxAll(
                center,
                new Vector2(width, height),
                totalAngle,
                DamageShapeHelper.EnemyMask);

            var info = DamageShapeHelper.BuildInfo(ctx, baseDamage, damageMultiplier, damageType);
            bool killed = DamageShapeHelper.ApplyToTargets(hits, info);

            ctx.ExecuteOutputPort(killed ? "onKill" : "done");
            if (killed) ctx.ExecuteOutputPort("done");
        }
    }
}
