using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Skills.Graph
{
    /// <summary>
    /// 技能节点图资产。
    ///
    /// 存储一组原子节点（<see cref="nodes"/>）和它们之间的连线（<see cref="connections"/>）。
    /// 通过 <see cref="SkillGraphExecutor"/> 在运行时执行，也可直接由
    /// <see cref="RPG.Skills.SkillData.skillGraph"/> 引用，作为技能执行逻辑的替代策略。
    ///
    /// 序列化说明:
    ///   nodes 列表使用 [SerializeReference]，支持 SkillNode 子类的多态序列化。
    ///   Unity 2019.3+ 特性，当前项目 (Unity 2023) 完全支持。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillGraph", menuName = "RPG/Skills/Skill Graph")]
    public class SkillGraph : ScriptableObject
    {
        [SerializeReference]
        public List<SkillNode> nodes = new List<SkillNode>();

        public List<NodeConnection> connections = new List<NodeConnection>();

        // ── Node management ───────────────────────────────────────────────────

        public SkillNode GetNode(string nodeId)
            => nodes.FirstOrDefault(n => n.nodeId == nodeId);

        public T GetFirstNode<T>() where T : SkillNode
            => nodes.OfType<T>().FirstOrDefault();

        public void AddNode(SkillNode node)
        {
            if (node != null && !nodes.Contains(node))
                nodes.Add(node);
        }

        public void RemoveNode(string nodeId)
        {
            int idx = nodes.FindIndex(n => n.nodeId == nodeId);
            if (idx < 0) return;
            nodes.RemoveAt(idx);

            // Remove all connections referencing this node
            connections.RemoveAll(c => c.fromNodeId == nodeId || c.toNodeId == nodeId);
        }

        // ── Connection management ─────────────────────────────────────────────

        /// <summary>
        /// 添加连线。如果目标输入端口已有连线则先删除旧连线（一对一语义）。
        /// </summary>
        public void Connect(string fromNodeId, string fromPortId,
                            string toNodeId,   string toPortId)
        {
            // Remove existing connection to the same input port (one connection per input)
            connections.RemoveAll(c => c.toNodeId == toNodeId && c.toPortId == toPortId);
            connections.Add(new NodeConnection(fromNodeId, fromPortId, toNodeId, toPortId));
        }

        public void Disconnect(string fromNodeId, string fromPortId,
                               string toNodeId,   string toPortId)
        {
            connections.RemoveAll(c =>
                c.fromNodeId == fromNodeId && c.fromPortId == fromPortId &&
                c.toNodeId   == toNodeId   && c.toPortId   == toPortId);
        }

        public void DisconnectAllFrom(string nodeId, string portId)
            => connections.RemoveAll(c => c.fromNodeId == nodeId && c.fromPortId == portId);

        public void DisconnectAllTo(string nodeId, string portId)
            => connections.RemoveAll(c => c.toNodeId == nodeId && c.toPortId == portId);

        // ── Query helpers ─────────────────────────────────────────────────────

        public IEnumerable<NodeConnection> GetConnectionsFrom(string nodeId, string portId)
            => connections.Where(c => c.fromNodeId == nodeId && c.fromPortId == portId);

        public IEnumerable<NodeConnection> GetConnectionsTo(string nodeId, string portId)
            => connections.Where(c => c.toNodeId == nodeId && c.toPortId == portId);

        public bool IsPortConnected(string nodeId, string portId, bool isOutput)
            => isOutput
                ? connections.Any(c => c.fromNodeId == nodeId && c.fromPortId == portId)
                : connections.Any(c => c.toNodeId   == nodeId && c.toPortId   == portId);

        /// <summary>
        /// 验证所有连线：删除引用了不存在节点或端口的悬空连线。
        /// </summary>
        public int Validate()
        {
            int removed = connections.RemoveAll(c =>
            {
                var fromNode = GetNode(c.fromNodeId);
                var toNode   = GetNode(c.toNodeId);
                if (fromNode == null || toNode == null) return true;

                bool outputExists = fromNode.GetOutputPorts().Any(p => p.Id == c.fromPortId);
                bool inputExists  = toNode.GetInputPorts().Any(p => p.Id == c.toPortId);
                return !outputExists || !inputExists;
            });
            return removed;
        }
    }
}
