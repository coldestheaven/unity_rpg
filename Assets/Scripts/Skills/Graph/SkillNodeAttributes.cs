using System;

namespace RPG.Skills.Graph
{
    // ──────────────────────────────────────────────────────────────────────────
    // [SkillNodeType]  — 标记一个类为可被编辑器反射发现的原子节点
    //
    // 用法:
    //   [SkillNodeType("施加伤害", "战斗", "#E84040",
    //       tooltip: "对目标施加一次物理/魔法伤害")]
    //   public class DamageNode : SkillNode { ... }
    //
    // 只要继承 SkillNode 并加上此特性，节点就会自动出现在技能图编辑器的
    // 右键菜单和左侧节点面板中，无需修改任何编辑器代码。
    // ──────────────────────────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SkillNodeTypeAttribute : Attribute
    {
        /// <summary>节点在编辑器中显示的名称。</summary>
        public readonly string DisplayName;

        /// <summary>节点分类（用于右键菜单和面板分组）。</summary>
        public readonly string Category;

        /// <summary>节点标题栏颜色（HTML hex，如 "#E84040"）。</summary>
        public readonly string HexColor;

        /// <summary>悬停提示。</summary>
        public readonly string Tooltip;

        public SkillNodeTypeAttribute(
            string displayName,
            string category = "通用",
            string hexColor = "#4A7FA5",
            string tooltip = "")
        {
            DisplayName = displayName;
            Category    = category;
            HexColor    = hexColor;
            Tooltip     = tooltip;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // [NodeField]  — 标记字段为可在节点编辑器中直接编辑的参数
    //
    // 编辑器通过反射扫描 SkillNode 子类的所有带此特性的 public 字段，
    // 并根据字段类型自动选择对应的 GUI 控件（FloatField / EnumPopup 等）。
    // ──────────────────────────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NodeFieldAttribute : Attribute
    {
        /// <summary>字段在编辑器中显示的标签。留 null 则使用字段名。</summary>
        public readonly string Label;

        /// <summary>是否启用 Min/Max 范围限制（仅对 float/int 有效）。</summary>
        public readonly bool HasRange;

        public readonly float Min;
        public readonly float Max;

        public NodeFieldAttribute(string label = null)
        {
            Label = label;
        }

        public NodeFieldAttribute(string label, float min, float max)
        {
            Label    = label;
            HasRange = true;
            Min      = min;
            Max      = max;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // [NodeInput] / [NodeOutput]  — 在节点类注释中说明端口（文档用途）
    // 实际端口由 SkillNode.GetInputPorts() / GetOutputPorts() 返回。
    // ──────────────────────────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class NodeInputAttribute : Attribute
    {
        public readonly string PortId;
        public readonly string Label;
        public NodeInputAttribute(string portId, string label) { PortId = portId; Label = label; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class NodeOutputAttribute : Attribute
    {
        public readonly string PortId;
        public readonly string Label;
        public NodeOutputAttribute(string portId, string label) { PortId = portId; Label = label; }
    }
}
