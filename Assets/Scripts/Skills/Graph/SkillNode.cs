using System;
using UnityEngine;

namespace RPG.Skills.Graph
{
    // ──────────────────────────────────────────────────────────────────────────
    // 端口数据类型
    // ──────────────────────────────────────────────────────────────────────────

    public enum PortDataType
    {
        Execution,  // 执行流（灰/白）
        Float,      // 浮点数（绿）
        Int,        // 整数（浅绿）
        Bool,       // 布尔（黄）
        GameObject, // 对象引用（蓝）
        String      // 字符串（橙）
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 端口定义  — 描述节点上的一个输入/输出端口
    // ──────────────────────────────────────────────────────────────────────────

    [Serializable]
    public sealed class NodePortDefinition
    {
        /// <summary>端口唯一 ID（同一节点内唯一，用于连线索引）。</summary>
        public readonly string Id;

        /// <summary>编辑器中显示的端口名称。</summary>
        public readonly string Label;

        /// <summary>端口传递的数据类型。</summary>
        public readonly PortDataType DataType;

        public NodePortDefinition(string id, string label, PortDataType dataType = PortDataType.Execution)
        {
            Id       = id;
            Label    = label;
            DataType = dataType;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SkillNode — 原子节点抽象基类
    //
    // 所有具体节点继承此类，并添加 [SkillNodeType] 特性以被编辑器自动发现。
    //
    // 最简节点示例:
    //
    //   [SkillNodeType("打印日志", "调试", "#888888")]
    //   public class LogNode : SkillNode
    //   {
    //       [NodeField("消息")] public string message = "Hello!";
    //
    //       public override NodePortDefinition[] GetInputPorts() => new[]
    //           { new NodePortDefinition("exec", "执行") };
    //
    //       public override NodePortDefinition[] GetOutputPorts() => new[]
    //           { new NodePortDefinition("done", "完成") };
    //
    //       public override void Execute(SkillNodeContext ctx)
    //       {
    //           Debug.Log(message);
    //           ctx.ExecuteOutputPort("done");
    //       }
    //   }
    //
    // 该类存入 SkillGraph.nodes（[SerializeReference] List），Unity 负责序列化。
    // ──────────────────────────────────────────────────────────────────────────

    [Serializable]
    public abstract class SkillNode
    {
        // ── Graph metadata (serialised, not shown in node field editor) ───────

        [HideInInspector] public string nodeId = Guid.NewGuid().ToString("N").Substring(0, 10);
        [HideInInspector] public Vector2 editorPosition = new Vector2(100f, 100f);

        // ── Port API ─────────────────────────────────────────────────────────

        /// <summary>此节点的输入端口列表（执行流进入方向）。</summary>
        public abstract NodePortDefinition[] GetInputPorts();

        /// <summary>此节点的输出端口列表（执行流离开方向）。</summary>
        public abstract NodePortDefinition[] GetOutputPorts();

        // ── Runtime execution ─────────────────────────────────────────────────

        /// <summary>
        /// 节点执行主体。调用 <see cref="SkillNodeContext.ExecuteOutputPort"/> 以驱动后续节点。
        /// </summary>
        public abstract void Execute(SkillNodeContext ctx);

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>返回此节点的 [SkillNodeType] 特性，未找到则返回 null。</summary>
        public SkillNodeTypeAttribute GetNodeTypeAttribute()
            => GetType().GetCustomAttributes(typeof(SkillNodeTypeAttribute), false) is
               SkillNodeTypeAttribute[] arr && arr.Length > 0 ? arr[0] : null;

        /// <summary>节点在编辑器中显示的名称（来自特性；若未标注则取类型名）。</summary>
        public string GetDisplayName()
            => GetNodeTypeAttribute()?.DisplayName ?? GetType().Name;
    }
}
