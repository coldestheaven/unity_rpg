using System;
using RPG.Skills.Graph;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 入口节点 — 技能施放时的起始点。
    ///
    /// 每张技能图必须有且仅有一个 OnCastNode。
    /// 没有输入端口；唯一的输出端口"执行"驱动后续节点链。
    /// </summary>
    [SkillNodeType("技能施放", "触发器", "#E89034",
        tooltip: "技能图的入口。技能被使用时从此节点开始执行。")]
    public sealed class OnCastNode : SkillNode
    {
        public override NodePortDefinition[] GetInputPorts()
            => Array.Empty<NodePortDefinition>();

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override void Execute(SkillNodeContext ctx)
            => ctx.ExecuteOutputPort("execute");
    }
}
