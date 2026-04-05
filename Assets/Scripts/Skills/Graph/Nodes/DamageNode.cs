using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 施加伤害节点 — 对施法者附近的目标（或特定目标）应用一次伤害。
    ///
    /// radius > 0 → 范围伤害（OverlapCircle）
    /// radius = 0 → 仅作用于 ctx.PrimaryTarget（如为 null 则跳过）
    /// </summary>
    [SkillNodeType("施加伤害", "战斗", "#E84040",
        tooltip: "对目标施加物理/魔法伤害。radius=0 为单体，>0 为AOE。")]
    [NodeInput("execute", "执行")]
    [NodeOutput("done",   "完成")]
    [NodeOutput("onKill", "击杀时")]
    public sealed class DamageNode : SkillNode
    {
        [NodeField("基础伤害", 0f, 10000f)]
        public float baseDamage = 10f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Physical;

        [NodeField("伤害范围 (0=单体)")]
        public float radius = 0f;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("done",   "完成"),
            new NodePortDefinition("onKill", "击杀时")
        };

        public override void Execute(SkillNodeContext ctx)
        {
            float finalDamage = baseDamage * damageMultiplier *
                                (ctx.SkillData != null ? ctx.SkillData.damageMultiplier : 1f);
            int scaledDmg = ctx.SkillData != null
                ? ctx.SkillData.GetDamage(ctx.SkillLevel)
                : Mathf.RoundToInt(finalDamage);

            var combatType = CombatDamageTypeMapper.FromSkillDamageType(damageType);
            var info = new DamageInfo(scaledDmg, ctx.CasterPosition, ctx.CasterObject,
                                       combatType, CombatHitKind.Skill);

            bool killedAny = false;

            if (radius > 0f)
            {
                var hits = Physics2D.OverlapCircleAll(ctx.CasterPosition, radius,
                    UnityEngine.LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    bool wasAlive = hit.TryGetComponent<Gameplay.Combat.DamageableBase>(out var db)
                                    && !db.IsDead;
                    CombatResolver.TryApplyDamage(hit, info);
                    if (wasAlive && db != null && db.IsDead) killedAny = true;
                }
            }
            else if (ctx.PrimaryTarget != null)
            {
                if (ctx.PrimaryTarget.TryGetComponent<Gameplay.Combat.DamageableBase>(out var db))
                {
                    bool wasAlive = !db.IsDead;
                    CombatResolver.TryApplyDamage(ctx.PrimaryTarget, info);
                    if (wasAlive && db.IsDead) killedAny = true;
                }
            }

            ctx.ExecuteOutputPort(killedAny ? "onKill" : "done");
            if (killedAny) ctx.ExecuteOutputPort("done");
        }
    }
}
