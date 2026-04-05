using UnityEngine;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 生成特效节点 — 在指定偏移位置实例化一个 GameObject 预制体，可选自动销毁。
    /// </summary>
    [SkillNodeType("生成特效", "视觉", "#7B5EA7",
        tooltip: "在施法者位置（加偏移）实例化一个特效/弹丸预制体。")]
    public sealed class SpawnEffectNode : SkillNode
    {
        [NodeField("预制体")]
        public GameObject prefab;

        [NodeField("位置偏移")]
        public Vector2 offset = Vector2.zero;

        [NodeField("自动销毁时间 (s)", 0f, 60f)]
        public float destroyAfter = 3f;

        [NodeField("随施法者朝向旋转")]
        public bool alignToFacing = true;

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
            if (prefab != null)
            {
                Vector3 spawnPos = ctx.CasterPosition + (Vector3)(Vector2)offset;

                Quaternion rot = Quaternion.identity;
                if (alignToFacing && ctx.Caster != null)
                {
                    var sr = ctx.Caster.GetComponent<SpriteRenderer>();
                    bool flipX = sr != null && sr.flipX;
                    rot = Quaternion.Euler(0f, flipX ? 180f : 0f, 0f);
                }

                var go = Object.Instantiate(prefab, spawnPos, rot);

                // Pass skill data if the prefab has a SkillEffect
                if (ctx.SkillData != null &&
                    go.TryGetComponent<RPG.Skills.SkillEffect>(out var se))
                    se.Initialize(ctx.SkillData, ctx.Caster, ctx.SkillLevel);

                if (destroyAfter > 0f)
                    Object.Destroy(go, destroyAfter);
            }

            ctx.ExecuteOutputPort("done");
        }
    }
}
