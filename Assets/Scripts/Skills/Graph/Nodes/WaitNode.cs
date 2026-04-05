using System.Collections;
using UnityEngine;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 延迟节点 — 等待指定时间后继续执行后续节点。
    ///
    /// 实现方式: 通过 SkillGraphCoroutineHost（挂在场景中的 MonoBehaviour）
    /// 启动一个协程，到时再调用 ctx.ExecuteOutputPort("done")。
    /// 若找不到 CoroutineHost，则立即继续（降级）。
    /// </summary>
    [SkillNodeType("延迟等待", "流程控制", "#4AB8E8",
        tooltip: "暂停执行流指定时间后继续。需要场景中存在 SkillGraphCoroutineHost。")]
    public sealed class WaitNode : SkillNode
    {
        [NodeField("等待时间 (s)", 0f, 30f)]
        public float delay = 1f;

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
            if (delay <= 0f)
            {
                ctx.ExecuteOutputPort("done");
                return;
            }

            var host = Object.FindFirstObjectByType<SkillGraphCoroutineHost>();
            if (host != null)
            {
                host.StartCoroutine(WaitAndContinue(ctx));
            }
            else
            {
                // Degraded: continue immediately
                Debug.LogWarning("[WaitNode] 场景中未找到 SkillGraphCoroutineHost，立即继续执行。");
                ctx.ExecuteOutputPort("done");
            }
        }

        private IEnumerator WaitAndContinue(SkillNodeContext ctx)
        {
            var savedNode = ctx.CurrentNode;
            yield return new WaitForSeconds(delay);
            ctx.CurrentNode = this;
            ctx.ExecuteOutputPort("done");
            ctx.CurrentNode = savedNode;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SkillGraphCoroutineHost — 场景中的 MonoBehaviour，为 WaitNode 提供协程宿主
    // 推荐挂在持久化 GameObject（DontDestroyOnLoad）上。
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class SkillGraphCoroutineHost : MonoBehaviour
    {
        private static SkillGraphCoroutineHost _instance;

        public static SkillGraphCoroutineHost EnsureExists()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[SkillGraphCoroutineHost]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SkillGraphCoroutineHost>();
            return _instance;
        }
    }
}
