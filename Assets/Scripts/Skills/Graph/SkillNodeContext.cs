using System.Collections.Generic;
using UnityEngine;

namespace RPG.Skills.Graph
{
    /// <summary>
    /// 技能节点执行上下文 — 节点运行时所需的所有信息。
    ///
    /// 在 <see cref="SkillGraphExecutor.Execute"/> 中创建，然后依次传给每个被激活的节点。
    /// 节点通过 <see cref="ExecuteOutputPort"/> 驱动执行流向下一个连接节点。
    ///
    /// 变量字典 (<see cref="Variables"/>) 允许节点之间共享临时数据（如击杀计数、目标引用等）。
    /// </summary>
    public sealed class SkillNodeContext
    {
        // ── Graph execution state ─────────────────────────────────────────────

        /// <summary>当前正在执行的图资产。</summary>
        public SkillGraph Graph { get; internal set; }

        /// <summary>当前正在执行的节点（节点调用 Execute 前由 Executor 设置）。</summary>
        public SkillNode CurrentNode { get; internal set; }

        // ── Skill info ────────────────────────────────────────────────────────

        public RPG.Skills.SkillData SkillData { get; }
        public int SkillLevel { get; }

        // ── Caster info ───────────────────────────────────────────────────────

        public Transform Caster { get; }
        public Vector3 CasterPosition => Caster != null ? Caster.position : Vector3.zero;
        public GameObject CasterObject => Caster != null ? Caster.gameObject : null;

        // ── Target(s) ─────────────────────────────────────────────────────────

        /// <summary>主要目标（单体技能）。</summary>
        public Collider2D PrimaryTarget { get; set; }

        /// <summary>范围内的所有目标（AOE技能）。</summary>
        public Collider2D[] AreaTargets { get; set; }

        // ── Shared variables ──────────────────────────────────────────────────

        /// <summary>节点间传递临时数据的字典。键由节点自行约定。</summary>
        public Dictionary<string, object> Variables { get; } = new Dictionary<string, object>();

        /// <summary>已执行节点数量，用于检测无限循环（上限 1000）。</summary>
        private int _executionCount;
        private const int MaxExecutions = 1000;

        // ─────────────────────────────────────────────────────────────────────

        public SkillNodeContext(
            SkillGraph graph,
            RPG.Skills.SkillData skillData,
            int skillLevel,
            Transform caster)
        {
            Graph      = graph;
            SkillData  = skillData;
            SkillLevel = skillLevel;
            Caster     = caster;
        }

        // ── Execution helpers ─────────────────────────────────────────────────

        /// <summary>
        /// 沿当前节点的指定输出端口向后驱动执行流。
        /// 此方法会找到所有连向该端口的下游节点并依次执行。
        /// </summary>
        public void ExecuteOutputPort(string portId)
        {
            if (Graph == null || CurrentNode == null) return;

            foreach (var conn in Graph.GetConnectionsFrom(CurrentNode.nodeId, portId))
            {
                if (_executionCount++ >= MaxExecutions)
                {
                    Debug.LogWarning($"[SkillGraph] 执行次数超过上限 {MaxExecutions}，" +
                                     "图中可能存在循环连接。已中止。");
                    return;
                }

                var next = Graph.GetNode(conn.toNodeId);
                if (next == null) continue;

                var prev = CurrentNode;
                CurrentNode = next;
                next.Execute(this);
                CurrentNode = prev;
            }
        }

        // ── Variable helpers ──────────────────────────────────────────────────

        public void SetVar<T>(string key, T value)   => Variables[key] = value;
        public bool HasVar(string key)               => Variables.ContainsKey(key);

        public T GetVar<T>(string key, T defaultValue = default)
        {
            if (Variables.TryGetValue(key, out object val) && val is T typed)
                return typed;
            return defaultValue;
        }

        // ── Damage multiplier (level scaling passthrough) ─────────────────────

        public float DamageMultiplier => SkillData != null
            ? SkillData.damageMultiplier
            : 1f;
    }
}
