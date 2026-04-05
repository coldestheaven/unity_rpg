using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 治疗节点 — 为施法者或附近目标恢复生命值。
    /// </summary>
    [SkillNodeType("施加治疗", "战斗", "#34C16A",
        tooltip: "恢复施法者或目标的生命值。radius=0 治疗施法者自身。")]
    public sealed class HealNode : SkillNode
    {
        [NodeField("治疗量", 0f, 10000f)]
        public float healAmount = 20f;

        [NodeField("治疗范围 (0=施法者)", 0f, 50f)]
        public float radius = 0f;

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("done", "完成")
        };

        public override void Execute(SkillNodeContext ctx)
        {
            if (radius <= 0f)
            {
                // Heal caster
                if (ctx.Caster != null &&
                    ctx.Caster.TryGetComponent<DamageableBase>(out var casterHealth))
                    casterHealth.Heal(healAmount);
            }
            else
            {
                // Heal allies in radius
                var hits = Physics2D.OverlapCircleAll(ctx.CasterPosition, radius,
                    LayerMask.GetMask("Player", "Ally"));
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<DamageableBase>(out var h))
                        h.Heal(healAmount);
                }
            }

            ctx.ExecuteOutputPort("done");
        }
    }
}
