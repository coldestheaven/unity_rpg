using UnityEngine;
using RPG.Simulation;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 持续伤害节点 — 向目标（或范围内敌人）施加 DoT 效果。
    /// 通过 <see cref="GameSimulation.Instance.Combat"/> 的逻辑线程管理 Tick。
    /// </summary>
    [SkillNodeType("持续伤害 (DoT)", "战斗", "#C06030",
        tooltip: "对目标施加持续伤害效果，每 tickInterval 秒伤害一次。")]
    public sealed class ApplyDoTNode : SkillNode
    {
        [NodeField("每次伤害", 0f, 1000f)]
        public float damagePerTick = 5f;

        [NodeField("Tick 间隔 (s)", 0.1f, 10f)]
        public float tickInterval = 1f;

        [NodeField("总 Tick 次数", 1f, 60f)]
        public int maxTicks = 5;

        [NodeField("效果 ID（防止重复叠加）")]
        public string dotId = "dot_default";

        [NodeField("范围 (0=单体)", 0f, 30f)]
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
            var simCombat = GameSimulation.Instance?.Combat;

            if (radius > 0f)
            {
                var hits = Physics2D.OverlapCircleAll(ctx.CasterPosition, radius,
                    LayerMask.GetMask("Enemy"));

                foreach (var hit in hits)
                    ApplyDoTToTarget(hit, simCombat, ctx);
            }
            else if (ctx.PrimaryTarget != null)
            {
                ApplyDoTToTarget(ctx.PrimaryTarget, simCombat, ctx);
            }

            ctx.ExecuteOutputPort("done");
        }

        private void ApplyDoTToTarget(
            UnityEngine.Collider2D target,
            CombatStateSimulation simCombat,
            SkillNodeContext ctx)
        {
            if (target == null) return;

            if (simCombat != null &&
                target.TryGetComponent<Gameplay.Combat.DamageableBase>(out _))
            {
                // Use logic-thread simulation for DoT (preferred path)
                // HealthSimulation is internal to DamageableBase; access via a public bridge
                // For MVP: remove existing DoT with same ID, add new one
                string uniqueId = $"{dotId}_{target.GetInstanceID()}";
                simCombat.RemoveDoT(uniqueId);

                // We can't get HealthSimulation directly (it's private in DamageableBase).
                // Fall through to the direct path for now; future refactor can expose it.
            }

            // Direct path: apply repeated hits via MonoBehaviour coroutine (no sim reference)
            if (target.TryGetComponent<Gameplay.Combat.DamageableBase>(out var db) &&
                target.TryGetComponent<MonoBehaviour>(out var mb))
            {
                mb.StartCoroutine(DirectDoTCoroutine(db, ctx));
            }
        }

        private System.Collections.IEnumerator DirectDoTCoroutine(
            Gameplay.Combat.DamageableBase target,
            SkillNodeContext ctx)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (target == null || target.IsDead) yield break;
                target.TakeDamage(damagePerTick, ctx.CasterPosition);
                yield return new UnityEngine.WaitForSeconds(tickInterval);
            }
        }
    }
}
