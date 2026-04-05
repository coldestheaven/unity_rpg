using UnityEngine;
using RPG.Buff;

namespace RPG.Skills.Graph.Nodes
{
    public enum BuffCheckMode
    {
        HasBuff,        // 目标是否有指定 Buff
        StackCountGte,  // 叠层数 ≥ minStacks
        AnyBuff,        // 是否有任意 Buff
        AnyDebuff       // 是否有任意 Debuff
    }

    /// <summary>
    /// Buff 条件节点 — 检查目标是否持有指定 Buff，并走 True/False 分支。
    ///
    /// 典型用法:
    ///   OnHit → CheckBuff(中毒, target=HitTarget)
    ///             ├─ True  → 叠加毒层 (ApplyBuff 升级)
    ///             └─ False → 初始施毒 (ApplyBuff)
    /// </summary>
    [SkillNodeType("Buff 条件", "Buff", "#A8A834",
        tooltip: "检查目标是否持有某 Buff，走 True 或 False 分支。")]
    public sealed class CheckBuffNode : SkillNode
    {
        [NodeField("检查模式")]
        public BuffCheckMode checkMode = BuffCheckMode.HasBuff;

        [NodeField("Buff 数据 (HasBuff/StackCount 时用)")]
        public BuffData buffData;

        [NodeField("最少叠层数 (StackCountGte)", 1f, 20f)]
        public int minStacks = 2;

        [NodeField("目标")]
        public BuffTargetMode targetMode = BuffTargetMode.PrimaryTarget;

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
            GameObject target = ResolveTarget(ctx);
            bool result = target != null && Evaluate(target);
            ctx.ExecuteOutputPort(result ? "true" : "false");
        }

        private bool Evaluate(GameObject target)
        {
            var bc = target.GetComponent<BuffController>();
            if (bc == null) return false;

            switch (checkMode)
            {
                case BuffCheckMode.HasBuff:
                    if (buffData == null) return false;
                    string id = string.IsNullOrEmpty(buffData.buffId) ? buffData.name : buffData.buffId;
                    return bc.HasBuff(id);

                case BuffCheckMode.StackCountGte:
                    if (buffData == null) return false;
                    string sid = string.IsNullOrEmpty(buffData.buffId) ? buffData.name : buffData.buffId;
                    var inst = bc.GetBuff(sid);
                    return inst != null && inst.Stacks >= minStacks;

                case BuffCheckMode.AnyBuff:
                    foreach (var b in bc.ActiveBuffs)
                        if (b.Data.category == BuffCategory.Buff) return true;
                    return false;

                case BuffCheckMode.AnyDebuff:
                    foreach (var b in bc.ActiveBuffs)
                        if (b.Data.category == BuffCategory.Debuff) return true;
                    return false;

                default: return false;
            }
        }

        private GameObject ResolveTarget(SkillNodeContext ctx)
        {
            switch (targetMode)
            {
                case BuffTargetMode.Caster:        return ctx.Caster?.gameObject;
                case BuffTargetMode.PrimaryTarget: return ctx.PrimaryTarget?.gameObject;
                default:                           return null;
            }
        }
    }
}
