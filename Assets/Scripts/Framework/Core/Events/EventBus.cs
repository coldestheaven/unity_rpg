using System;
using System.Collections.Generic;
using Framework.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace Framework.Events
{
    /// <summary>
    /// 零 GC 类型安全事件总线（枚举 key + struct 事件）。
    ///
    /// 设计要点：
    ///   • <see cref="GameEventId"/> 枚举值直接作为数组下标 — O(1)，无哈希，无装箱。
    ///   • 事件载体为 <c>readonly struct</c> — 栈分配，不上堆。
    ///   • <see cref="Publish{TEvent}"/> 用 <c>in</c> 参数 — 传引用，不复制结构体。
    ///   • <see cref="EventIdOf{T}"/> 静态缓存 — 每个泛型实例化只算一次 ID。
    ///   • 分发使用静态缓冲区 — 无 <c>ToArray()</c>，无临时堆分配。
    ///
    /// 用法（与旧版 API 完全兼容）：
    /// <code>
    ///   EventBus.Subscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
    ///   EventBus.Publish(new PlayerDiedEvent(transform.position));
    ///   EventBus.Unsubscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
    /// </code>
    ///
    /// 注意：仅在 Unity 主线程调用；无内置线程锁。
    /// </summary>
    public static class EventBus
    {
        // 以枚举值为下标的处理器列表数组；长度固定，无扩容，无装箱。
        private static readonly List<Delegate>[] _handlers =
            new List<Delegate>[(int)GameEventId._Count];

        // ── 重入安全分发缓冲区 ─────────────────────────────────────────────────
        //
        // 问题：若处理器 A 在执行时调用 Publish(eventB)，将覆写同一个 _dispatchBuffer，
        //       破坏外层循环的快照。
        //
        // 方案：维护调用深度计数器 _depth。
        //   • 深度 0（常路径）：直接使用共享静态缓冲区，零额外分配。
        //   • 深度 ≥ 1（重入路径）：分配临时缓冲区，用后置 null，允许 GC。
        //     重入在业务代码中极少发生，偶发一次分配可接受。
        private static Delegate[] _dispatchBuffer = new Delegate[16];
        private static int        _dispatchDepth;

        // ── Subscribe / Unsubscribe ───────────────────────────────────────────

        /// <summary>注册处理器。重复注册静默忽略。</summary>
        public static void Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IGameEvent
        {
            int id = EventIdOf<TEvent>.Value;
            ref var slot = ref _handlers[id];
            if (slot == null) slot = new List<Delegate>(4);
            if (!slot.Contains(handler)) slot.Add(handler);
        }

        /// <summary>移除处理器。</summary>
        public static void Unsubscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IGameEvent
        {
            _handlers[EventIdOf<TEvent>.Value]?.Remove(handler);
        }

        // ── Publish ───────────────────────────────────────────────────────────

        /// <summary>
        /// 向所有当前订阅者分发事件。
        /// <paramref name="evt"/> 以 <c>in</c> 传递（按引用，不复制）。
        /// 分发前将处理器列表快照到缓冲区，允许处理器内部调用 Subscribe / Unsubscribe。
        /// 单个处理器抛出的异常不会中断其他处理器。
        /// 支持重入：处理器内再次调用 Publish 时使用独立临时缓冲区，不影响外层快照。
        /// </summary>
        public static void Publish<TEvent>(in TEvent evt)
            where TEvent : struct, IGameEvent
        {
            using var _pm = ProfilerMarkers.EventBus_Publish.Auto();

            var list = _handlers[(int)evt.EventId];
            if (list == null || list.Count == 0) return;

            int count = list.Count;
            _dispatchDepth++;

            // 常路径（深度 = 1）：使用共享静态缓冲区，零分配。
            // 重入路径（深度 > 1）：分配临时缓冲区，隔离外层快照。
            Delegate[] buffer;
            bool isReentrant = _dispatchDepth > 1;
            if (isReentrant)
            {
                buffer = new Delegate[count];
            }
            else
            {
                if (_dispatchBuffer.Length < count)
                    _dispatchBuffer = new Delegate[count * 2];
                buffer = _dispatchBuffer;
            }

            list.CopyTo(buffer, 0);

            for (int i = 0; i < count; i++)
            {
                try
                {
                    ((Action<TEvent>)buffer[i]).Invoke(evt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] {typeof(TEvent).Name} 处理器异常: {e}");
                }
                finally
                {
                    buffer[i] = null;   // 清除引用，避免 GC 根泄漏
                }
            }

            _dispatchDepth--;
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public static bool HasSubscribers<TEvent>()
            where TEvent : struct, IGameEvent
        {
            var list = _handlers[EventIdOf<TEvent>.Value];
            return list != null && list.Count > 0;
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        /// <summary>清除指定事件类型的所有处理器。</summary>
        public static void Clear<TEvent>()
            where TEvent : struct, IGameEvent
            => _handlers[EventIdOf<TEvent>.Value]?.Clear();

        /// <summary>清除所有处理器。建议在重大场景切换时调用。</summary>
        public static void ClearAll()
        {
            for (int i = 0; i < _handlers.Length; i++)
                _handlers[i]?.Clear();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// 每个事件类型的枚举 ID 静态缓存。
        /// JIT 在第一次使用时初始化，之后读取为纯字段访问，零开销。
        /// </summary>
        private static class EventIdOf<T> where T : struct, IGameEvent
        {
            /// <summary>
            /// <c>default(T)</c> 创建零值结构体（栈上），调用 EventId getter 返回
            /// 该类型的固定枚举值。整个表达式在类型首次使用时计算一次。
            /// </summary>
            public static readonly int Value = (int)default(T).EventId;
        }
    }
}
