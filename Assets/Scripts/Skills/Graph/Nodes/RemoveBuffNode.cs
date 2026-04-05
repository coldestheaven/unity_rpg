using UnityEngine;
using RPG.Buff;

namespace RPG.Skills.Graph.Nodes
{
    public enum RemoveBuffMode
    {
        ByData,         // 移除指定 BuffData（精确匹配）
        AllDebuffs,     // 移除全部减益（净化）
        AllBuffs,       // 移除全部增益
        All             // 移除全部 Buff
    }

    /// <summary>
    /// 移除 Buff 节点 — 从目标上移除指定的 Buff/Debuff。
    ///
    /// 典型用法:
    ///   HitEnemy → RemoveBuff(护盾, target=Caster) → done    (碰撞移除护盾)
    ///   OnCast   → RemoveBuff(mode=AllDebuffs, target=Caster) → done (净化)
    /// </summary>
    [SkillNodeType("移除 Buff", "Buff", "#A83434",
        tooltip: "从目标移除指定 Buff，或移除所有减益（净化）/所有增益（驱散）。")]
    public sealed class RemoveBuffNode : SkillNode
    {
        [NodeField("移除模式")]
        public RemoveBuffMode mode = RemoveBuffMode.ByData;

        [NodeField("Buff 数据 (ByData 时用)")]
        public BuffData buffData;

        [NodeField("目标")]
        public BuffTargetMode targetMode = BuffTargetMode.Caster;

        [NodeField("自动检测半径 (AllInRadius)", 0f, 50f)]
        public float autoRadius = 5f;

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("done",     "完成"),
            new NodePortDefinition("notFound", "未找到")
        };

        public override void Execute(SkillNodeContext ctx)
        {
            bool removedAny = false;

            switch (targetMode)
            {
                case BuffTargetMode.Caster:
                    removedAny = TryRemove(ctx.Caster?.gameObject);
                    break;

                case BuffTargetMode.PrimaryTarget:
                    if (ctx.PrimaryTarget != null)
                        removedAny = TryRemove(ctx.PrimaryTarget.gameObject);
                    break;

                case BuffTargetMode.AllAreaTargets:
                    if (ctx.AreaTargets != null)
                        foreach (var t in ctx.AreaTargets)
                            if (t != null && TryRemove(t.gameObject))
                                removedAny = true;
                    break;

                case BuffTargetMode.AllInRadius:
                    var hits = Physics2D.OverlapCircleAll(
                        ctx.CasterPosition, autoRadius, DamageShapeHelper.EnemyMask);
                    foreach (var h in hits)
                        if (h != null && TryRemove(h.gameObject))
                            removedAny = true;
                    break;
            }

            ctx.ExecuteOutputPort(removedAny ? "done" : "notFound");
        }

        private bool TryRemove(GameObject target)
        {
            if (target == null) return false;
            var bc = target.GetComponent<BuffController>();
            if (bc == null) return false;

            switch (mode)
            {
                case RemoveBuffMode.ByData:
                    if (buffData == null) return false;
                    string id = string.IsNullOrEmpty(buffData.buffId) ? buffData.name : buffData.buffId;
                    return bc.RemoveBuff(id);

                case RemoveBuffMode.AllDebuffs:
                    int before = bc.ActiveBuffs.Count;
                    bc.RemoveAllDebuffs();
                    return bc.ActiveBuffs.Count < before;

                case RemoveBuffMode.AllBuffs:
                    before = bc.ActiveBuffs.Count;
                    bc.RemoveAllByCategory(BuffCategory.Buff);
                    return bc.ActiveBuffs.Count < before;

                case RemoveBuffMode.All:
                    before = bc.ActiveBuffs.Count;
                    bc.RemoveAllBuffs();
                    return before > 0;

                default: return false;
            }
        }
    }
}
