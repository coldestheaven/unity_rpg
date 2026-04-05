using UnityEngine;
using RPG.Buff;

namespace RPG.Skills.Graph.Nodes
{
    public enum BuffTargetMode
    {
        Caster,         // 施法者自身
        PrimaryTarget,  // ctx.PrimaryTarget（单体目标）
        AllAreaTargets, // ctx.AreaTargets（前置范围检测节点的所有目标）
        AllInRadius     // 以施法者为圆心自动检测半径内目标
    }

    /// <summary>
    /// 施加 Buff 节点 — 向指定目标应用一个 <see cref="BuffData"/> 资产。
    ///
    /// 目标必须拥有 <see cref="BuffController"/> 组件才能接受 Buff。
    ///
    /// 典型用法:
    ///   OnCast → ApplyBuff(中毒, target=NearestEnemy, radius=5) → done
    ///   OnCast → ApplyBuff(护盾, target=Caster, duration=10)    → done
    /// </summary>
    [SkillNodeType("施加 Buff", "Buff", "#34A870",
        tooltip: "向目标（施法者/主目标/范围内所有目标）施加一个 Buff/Debuff。")]
    public sealed class ApplyBuffNode : SkillNode
    {
        [NodeField("Buff 数据")]
        public BuffData buffData;

        [NodeField("目标")]
        public BuffTargetMode targetMode = BuffTargetMode.PrimaryTarget;

        [NodeField("自动检测半径 (AllInRadius)", 0f, 50f)]
        public float autoRadius = 5f;

        [NodeField("持续时间覆盖 (0=用BuffData)", 0f, 300f)]
        public float durationOverride = 0f;

        [NodeField("传递技能等级")]
        public bool passSkillLevel = true;

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("applied",    "已施加"),
            new NodePortDefinition("alreadyHas", "目标已有"),
            new NodePortDefinition("noTarget",   "无目标")
        };

        public override void Execute(SkillNodeContext ctx)
        {
            if (buffData == null)
            {
                Debug.LogWarning("[ApplyBuffNode] 未设置 BuffData，跳过。");
                ctx.ExecuteOutputPort("noTarget");
                return;
            }

            int level = passSkillLevel ? ctx.SkillLevel : 1;
            float dur = durationOverride > 0f ? durationOverride : -1f;
            bool appliedAny = false;
            bool allHadBuff = true;

            switch (targetMode)
            {
                case BuffTargetMode.Caster:
                    if (TryApply(ctx.Caster?.gameObject, level, dur, ctx.Caster))
                        appliedAny = true;
                    else
                        allHadBuff = false;
                    break;

                case BuffTargetMode.PrimaryTarget:
                    if (ctx.PrimaryTarget == null)
                    {
                        ctx.ExecuteOutputPort("noTarget");
                        return;
                    }
                    bool hadBuff = HasBuff(ctx.PrimaryTarget.gameObject);
                    if (TryApply(ctx.PrimaryTarget.gameObject, level, dur, ctx.Caster))
                        appliedAny = !hadBuff;
                    allHadBuff = hadBuff;
                    break;

                case BuffTargetMode.AllAreaTargets:
                    if (ctx.AreaTargets == null || ctx.AreaTargets.Length == 0)
                    {
                        ctx.ExecuteOutputPort("noTarget");
                        return;
                    }
                    allHadBuff = true;
                    foreach (var t in ctx.AreaTargets)
                    {
                        if (t == null) continue;
                        bool had = HasBuff(t.gameObject);
                        TryApply(t.gameObject, level, dur, ctx.Caster);
                        if (!had) { appliedAny = true; allHadBuff = false; }
                    }
                    break;

                case BuffTargetMode.AllInRadius:
                    var hits = Physics2D.OverlapCircleAll(
                        ctx.CasterPosition, autoRadius, DamageShapeHelper.EnemyMask);
                    if (hits.Length == 0)
                    {
                        ctx.ExecuteOutputPort("noTarget");
                        return;
                    }
                    allHadBuff = true;
                    foreach (var h in hits)
                    {
                        if (h == null) continue;
                        bool had = HasBuff(h.gameObject);
                        TryApply(h.gameObject, level, dur, ctx.Caster);
                        if (!had) { appliedAny = true; allHadBuff = false; }
                    }
                    break;
            }

            if (allHadBuff && !appliedAny)
                ctx.ExecuteOutputPort("alreadyHas");
            else
                ctx.ExecuteOutputPort("applied");
        }

        private bool TryApply(GameObject target, int level, float dur, Transform caster)
        {
            if (target == null) return false;
            var bc = target.GetComponent<BuffController>();
            if (bc == null) return false;
            bc.ApplyBuff(buffData, level, caster, dur);
            return true;
        }

        private bool HasBuff(GameObject target)
        {
            if (target == null || buffData == null) return false;
            string id = string.IsNullOrEmpty(buffData.buffId) ? buffData.name : buffData.buffId;
            return target.GetComponent<BuffController>()?.HasBuff(id) ?? false;
        }
    }
}
