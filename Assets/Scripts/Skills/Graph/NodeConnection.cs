using System;

namespace RPG.Skills.Graph
{
    /// <summary>
    /// 描述节点图中一条有向边：从 fromNode 的某个输出端口
    /// 连向 toNode 的某个输入端口。
    /// 序列化存储在 <see cref="SkillGraph.connections"/> 中。
    /// </summary>
    [Serializable]
    public sealed class NodeConnection
    {
        public string fromNodeId;
        public string fromPortId;
        public string toNodeId;
        public string toPortId;

        public NodeConnection() { }

        public NodeConnection(string fromNodeId, string fromPortId,
                              string toNodeId,   string toPortId)
        {
            this.fromNodeId = fromNodeId;
            this.fromPortId = fromPortId;
            this.toNodeId   = toNodeId;
            this.toPortId   = toPortId;
        }

        public bool Matches(string fNode, string fPort, string tNode, string tPort)
            => fromNodeId == fNode && fromPortId == fPort &&
               toNodeId   == tNode && toPortId   == tPort;
    }
}
