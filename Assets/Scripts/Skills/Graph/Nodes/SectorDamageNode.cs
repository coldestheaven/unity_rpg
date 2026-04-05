using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 扇形伤害节点 — 以施法者为顶点，沿施法朝向检测指定角度和半径内的敌人。
    ///
    /// 典型用途：横扫、冰锥、火焰喷射、魔法阵前扇。
    ///
    ///          ╱‾‾‾‾‾‾‾‾╲
    ///        ╱  angleWidth  ╲
    ///       ╱                ╲
    ///      ●─────────────────▶  facing
    ///  caster      radius
    ///
    /// 检测流程：OverlapCircleAll → 逐目标判断角度。
    /// </summary>
    [SkillNodeType("扇形伤害", "战斗/形状", "#E8A030",
        tooltip: "在施法者朝向的扇形区域内对敌人造成伤害（OverlapCircle + 角度过滤）。")]
    public sealed class SectorDamageNode : SkillNode
    {
        [NodeField("基础伤害", 0f, 10000f)]
        public float baseDamage = 30f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Physical;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        [NodeField("半径", 0.1f, 50f)]
        public float radius = 4f;

        /// <summary>扇形总张角（度）。90° = 正前方左右各 45°。</summary>
        [NodeField("张角 (°)", 1f, 360f)]
        public float angleWidth = 90f;

        /// <summary>在施法者朝向基础上额外旋转的角度（正值逆时针）。</summary>
        [NodeField("朝向偏转 (°)", -180f, 180f)]
        public float directionOffset = 0f;

        /// <summary>使用目标碰撞体中心而非 position 计算角度（对大型敌人更准确）。</summary>
        [NodeField("使用碰撞体中心")]
        public bool useBoundsCenter = true;

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
            Vector2 origin = ctx.CasterPosition;

            // Compute final facing direction (with optional offset)
            Vector2 baseFacing  = DamageShapeHelper.GetFacing(ctx.Caster);
            float   baseAngle   = Vector2.SignedAngle(Vector2.right, baseFacing);
            float   finalAngle  = baseAngle + directionOffset;
            Vector2 facing      = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                                              Mathf.Sin(finalAngle * Mathf.Deg2Rad));

            float halfAngle = angleWidth * 0.5f;

            // Broad phase: circle
            var candidates = Physics2D.OverlapCircleAll(origin, radius, DamageShapeHelper.EnemyMask);
            if (candidates.Length == 0)
            {
                ctx.ExecuteOutputPort("done");
                return;
            }

            var info       = DamageShapeHelper.BuildInfo(ctx, baseDamage, damageMultiplier, damageType);
            bool killedAny = false;

            foreach (var hit in candidates)
            {
                if (hit == null) continue;

                // Narrow phase: angle filter
                Vector2 toTarget = useBoundsCenter
                    ? (Vector2)hit.bounds.center - origin
                    : (Vector2)hit.transform.position - origin;

                // toTarget.sqrMagnitude == 0 means the collider overlaps the origin (always inside)
                if (toTarget.sqrMagnitude > 0f &&
                    Vector2.Angle(facing, toTarget) > halfAngle)
                    continue;

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
