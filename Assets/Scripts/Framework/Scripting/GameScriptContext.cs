using System;
using Framework.Events;
using RPG.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.Scripting
{
    /// <summary>
    /// 提供给动态脚本的游戏 API 上下文。
    ///
    /// 设计原则：
    ///   • 只暴露业务需要的最小 API 面（门面模式），避免脚本直接访问内部类。
    ///   • 所有 Unity API 调用均在主线程；若需逻辑线程操作，使用 <see cref="EnqueueWork"/>。
    ///   • 上下文本身是轻量对象，每次 <see cref="ScriptRunner.Run"/> 重用同一实例。
    /// </summary>
    public sealed class GameScriptContext
    {
        // ── 快捷属性 ──────────────────────────────────────────────────────────

        /// <summary>当前活跃场景名称。</summary>
        public string CurrentScene => SceneManager.GetActiveScene().name;

        /// <summary>逻辑线程仿真根（为 null 则仿真未运行）。</summary>
        public GameSimulation Simulation => GameSimulation.Instance;

        /// <summary>游戏运行时间（秒）。</summary>
        public float GameTime => Time.time;

        // ── 日志 ──────────────────────────────────────────────────────────────

        /// <summary>在 Console 中打印普通信息（带 [Script] 前缀）。</summary>
        public void Log(string message)      => Debug.Log($"[Script] {message}");
        /// <summary>打印警告。</summary>
        public void LogWarning(string msg)   => Debug.LogWarning($"[Script] {msg}");
        /// <summary>打印错误。</summary>
        public void LogError(string msg)     => Debug.LogError($"[Script] {msg}");

        // ── EventBus 封装 ──────────────────────────────────────────────────────

        /// <summary>
        /// 向 EventBus 发布一个事件（在主线程调用）。
        /// <code>ctx.Publish(new PlayerDiedEvent(transform.position));</code>
        /// </summary>
        public void Publish<T>(T evt) where T : struct, IGameEvent
            => EventBus.Publish(evt);

        /// <summary>订阅 EventBus 事件。记得在不需要时调用 <see cref="Unsubscribe{T}"/>。</summary>
        public void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
            => EventBus.Subscribe(handler);

        /// <summary>取消订阅。</summary>
        public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
            => EventBus.Unsubscribe(handler);

        // ── 逻辑线程 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 将一个 Action 提交到逻辑线程（GameSimulation）执行。
        /// <para>
        /// 适用于需要修改仿真状态（HP、冷却、XP 等）但不阻塞主线程的操作。
        /// </para>
        /// </summary>
        /// <returns><c>false</c> 表示仿真未运行或队列已满。</returns>
        public bool EnqueueWork(Action work)
        {
            var sim = GameSimulation.Instance;
            if (sim == null) return false;
            return sim.EnqueueWork(work);
        }

        // ── 场景工具 ──────────────────────────────────────────────────────────

        /// <summary>在场景中按名称查找 GameObject（同 <c>GameObject.Find</c>）。</summary>
        public GameObject Find(string name) => GameObject.Find(name);

        /// <summary>获取第一个指定类型的组件（同 <c>Object.FindObjectOfType</c>）。</summary>
        public T FindComponent<T>() where T : Component
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        // ── 仿真快捷操作 ──────────────────────────────────────────────────────

        /// <summary>向逻辑线程的进度仿真追加经验值。</summary>
        public bool AddExperience(float amount)
            => EnqueueWork(() => GameSimulation.Instance?.Progress.AddExperience(amount));

        /// <summary>向逻辑线程的进度仿真追加金币。</summary>
        public bool AddGold(int amount)
            => EnqueueWork(() => GameSimulation.Instance?.Progress.AddGold(amount));
    }
}
