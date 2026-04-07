using System;
using System.Collections.Generic;

namespace Framework.Core.Pools
{
    /// <summary>
    /// <see cref="List{T}"/> 的静态共享对象池。
    ///
    /// 每次 foreach / LINQ 在堆上产生一个 <c>List</c>，高频调用时 GC 压力显著。
    /// 通过复用已清空的 List 实例，消除这类堆分配。
    ///
    /// ■ 手动管理：
    /// <code>
    ///   var enemies = ListPool&lt;Enemy&gt;.Get();
    ///   Physics2D.OverlapCircleNonAlloc(...);
    ///   // 使用 enemies...
    ///   ListPool&lt;Enemy&gt;.Release(enemies);   // 必须调用，否则泄漏池容量
    /// </code>
    ///
    /// ■ using 作用域（推荐）：
    /// <code>
    ///   using (ListPool&lt;Enemy&gt;.Rent(out var enemies))
    ///   {
    ///       foreach (var e in enemies) { ... }
    ///   }   // 自动释放，无需手动 Release
    /// </code>
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool  = new Stack<List<T>>(8);
        private const           int            MaxPoolSize = 32;

        // ── 核心 API ─────────────────────────────────────────────────────────

        /// <summary>从池中取出一个已清空的 <see cref="List{T}"/>。</summary>
        public static List<T> Get()
        {
            lock (_pool)
                return _pool.Count > 0 ? _pool.Pop() : new List<T>(16);
        }

        /// <summary>
        /// 将 List 清空并归还池中。
        /// 归还后不得再访问该 List 实例。
        /// </summary>
        public static void Release(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            lock (_pool)
            {
                if (_pool.Count < MaxPoolSize)
                    _pool.Push(list);
            }
        }

        /// <summary>
        /// 以 using 作用域方式借用 List，作用域结束时自动归还。
        /// </summary>
        /// <param name="list">借出的 List 实例（已清空）。</param>
        /// <returns>实现 <see cref="IDisposable"/> 的作用域令牌。</returns>
        public static PooledList<T> Rent(out List<T> list)
        {
            list = Get();
            return new PooledList<T>(list);
        }

        // ── 调试 ─────────────────────────────────────────────────────────────

        /// <summary>当前池中空闲的 List 数量。</summary>
        public static int CountInactive
        {
            get { lock (_pool) return _pool.Count; }
        }
    }

    /// <summary>
    /// <see cref="ListPool{T}.Rent"/> 返回的作用域令牌。
    /// using 块结束时自动调用 <see cref="ListPool{T}.Release"/>。
    /// </summary>
    public readonly struct PooledList<T> : IDisposable
    {
        private readonly List<T> _list;
        internal PooledList(List<T> list) => _list = list;
        public void Dispose() => ListPool<T>.Release(_list);
    }
}
