using UnityEngine;

namespace RPG.Skills.Graph
{
    /// <summary>
    /// 静态工具类，负责启动技能节点图的运行时执行。
    ///
    /// 典型调用路径:
    ///   SkillController.TryUseSkill()
    ///     → SkillGraphExecutor.Execute(graph, skillData, level, caster)
    ///       → 从 OnCastNode 入口节点开始，沿连线递归驱动后续节点
    ///
    /// 线程: 必须在 Unity 主线程调用（涉及 Transform / Physics 等 Unity API）。
    /// </summary>
    public static class SkillGraphExecutor
    {
        /// <summary>
        /// 执行技能节点图。
        ///
        /// 找到图中第一个 <see cref="Nodes.OnCastNode"/> 作为入口，
        /// 构建 <see cref="SkillNodeContext"/> 并开始执行。
        /// </summary>
        /// <param name="graph">要执行的技能图。</param>
        /// <param name="skillData">触发此图的技能数据。</param>
        /// <param name="skillLevel">技能当前等级。</param>
        /// <param name="caster">施法者 Transform。</param>
        /// <returns>如果找到入口节点并成功执行，返回 true。</returns>
        public static bool Execute(
            SkillGraph graph,
            RPG.Skills.SkillData skillData,
            int skillLevel,
            Transform caster)
        {
            if (graph == null) return false;

            var entry = graph.GetFirstNode<Nodes.OnCastNode>();
            if (entry == null)
            {
                Debug.LogWarning($"[SkillGraphExecutor] 图 '{graph.name}' 中未找到 OnCastNode 入口节点。");
                return false;
            }

            var ctx = new SkillNodeContext(graph, skillData, skillLevel, caster);
            ctx.CurrentNode = entry;
            entry.Execute(ctx);
            return true;
        }
    }
}
