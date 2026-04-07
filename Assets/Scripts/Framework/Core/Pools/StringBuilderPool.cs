using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Core.Pools
{
    /// <summary>
    /// <see cref="StringBuilder"/> 的静态共享对象池。
    ///
    /// 字符串拼接（<c>string.Format</c>、<c>+</c>、插值）每次都产生堆分配。
    /// 通过复用 StringBuilder 并在最后一次性 <c>ToString()</c>，可将中间分配降为零。
    ///
    /// ■ 手动管理：
    /// <code>
    ///   var sb = StringBuilderPool.Get();
    ///   sb.Append("HP: ").Append(hp).Append('/').Append(maxHp);
    ///   string result = StringBuilderPool.GetStringAndRelease(sb);   // 取字符串并归还
    /// </code>
    ///
    /// ■ using 作用域（推荐）：
    /// <code>
    ///   using (StringBuilderPool.Rent(out var sb))
    ///   {
    ///       sb.Append("Lv.").Append(level);
    ///       levelText.text = sb.ToString();
    ///   }   // 自动归还
    /// </code>
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>(8);
        private const int DefaultCapacity = 64;
        private const int MaxPoolSize     = 32;
        private const int MaxRetainCapacity = 1024;  // 超大 SB 不归还，防止内存长期占用

        // ── 核心 API ─────────────────────────────────────────────────────────

        /// <summary>从池中取出一个已清空的 <see cref="StringBuilder"/>（容量默认 64）。</summary>
        public static StringBuilder Get()
        {
            lock (_pool)
                return _pool.Count > 0 ? _pool.Pop() : new StringBuilder(DefaultCapacity);
        }

        /// <summary>
        /// 调用 <see cref="StringBuilder.ToString()"/>，然后自动归还到池中。
        /// 这是最常见的使用收尾操作，避免忘记 Release。
        /// </summary>
        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            return result;
        }

        /// <summary>
        /// 归还 StringBuilder 到池中。归还后不得再访问该实例。
        /// </summary>
        public static void Release(StringBuilder sb)
        {
            if (sb == null) return;
            // 容量过大的 SB（大文本生成场景）不缓存，防止长期占用大块内存
            if (sb.Capacity > MaxRetainCapacity) return;
            sb.Clear();
            lock (_pool)
            {
                if (_pool.Count < MaxPoolSize)
                    _pool.Push(sb);
            }
        }

        /// <summary>
        /// 以 using 作用域方式借用 StringBuilder，作用域结束时自动归还。
        /// </summary>
        public static PooledStringBuilder Rent(out StringBuilder sb)
        {
            sb = Get();
            return new PooledStringBuilder(sb);
        }

        // ── 预分配 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 预先向池中压入 <paramref name="count"/> 个空 StringBuilder 实例。
        /// 适合在 Loading 界面调用，避免游戏中首次 Get 时触发 GC 分配。
        /// </summary>
        public static void Warmup(int count)
        {
            lock (_pool)
            {
                int toAdd = System.Math.Min(count, MaxPoolSize - _pool.Count);
                for (int i = 0; i < toAdd; i++)
                    _pool.Push(new StringBuilder(DefaultCapacity));
            }
        }

        // ── 调试 ─────────────────────────────────────────────────────────────

        /// <summary>当前池中空闲的 StringBuilder 数量。</summary>
        public static int CountInactive
        {
            get { lock (_pool) return _pool.Count; }
        }
    }

    /// <summary>
    /// <see cref="StringBuilderPool.Rent"/> 返回的作用域令牌。
    /// </summary>
    public readonly struct PooledStringBuilder : IDisposable
    {
        private readonly StringBuilder _sb;
        internal PooledStringBuilder(StringBuilder sb) => _sb = sb;
        public void Dispose() => StringBuilderPool.Release(_sb);
    }
}
