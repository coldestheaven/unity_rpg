using UnityEngine;

namespace RPG.Skills.Graph.Nodes
{
    public enum BranchCondition
    {
        Always,             // 永远走 True 分支（用作分流点）
        TargetHasFullHP,    // 目标满血
        TargetHpBelow50Pct, // 目标血量 < 50%
        CasterHpBelow30Pct, // 施法者血量 < 30%
        RandomChance,       // 随机概率
        VariableIsTrue      // 变量字典中某 key 为 true
    }

    /// <summary>
    /// 分支节点 — 根据条件走 True 或 False 分支。
    /// </summary>
    [SkillNodeType("条件分支", "流程控制", "#E8C040",
        tooltip: "根据条件判断走 True 或 False 执行路径。")]
    public sealed class BranchNode : SkillNode
    {
        [NodeField("条件")]
        public BranchCondition condition = BranchCondition.Always;

        [NodeField("随机概率 (0-1)", 0f, 1f)]
        public float randomChance = 0.5f;

        [NodeField("变量名（VariableIsTrue 时用）")]
        public string variableKey = "";

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("true",  "True ✓"),
            new NodePortDefinition("false", "False ✗")
        };

        public override void Execute(SkillNodeContext ctx)
        {
            bool result = Evaluate(ctx);
            ctx.ExecuteOutputPort(result ? "true" : "false");
        }

        private bool Evaluate(SkillNodeContext ctx)
        {
            switch (condition)
            {
                case BranchCondition.Always:
                    return true;

                case BranchCondition.TargetHasFullHP:
                    if (ctx.PrimaryTarget != null &&
                        ctx.PrimaryTarget.TryGetComponent<Gameplay.Combat.DamageableBase>(out var tdb))
                        return Mathf.Approximately(tdb.CurrentHealth, tdb.MaxHealth);
                    return false;

                case BranchCondition.TargetHpBelow50Pct:
                    if (ctx.PrimaryTarget != null &&
                        ctx.PrimaryTarget.TryGetComponent<Gameplay.Combat.DamageableBase>(out var tdb2))
                        return tdb2.MaxHealth > 0f && tdb2.CurrentHealth / tdb2.MaxHealth < 0.5f;
                    return false;

                case BranchCondition.CasterHpBelow30Pct:
                    if (ctx.Caster != null &&
                        ctx.Caster.TryGetComponent<Gameplay.Combat.DamageableBase>(out var cdb))
                        return cdb.MaxHealth > 0f && cdb.CurrentHealth / cdb.MaxHealth < 0.3f;
                    return false;

                case BranchCondition.RandomChance:
                    return Random.value < randomChance;

                case BranchCondition.VariableIsTrue:
                    return ctx.GetVar<bool>(variableKey, false);

                default:
                    return false;
            }
        }
    }
}
