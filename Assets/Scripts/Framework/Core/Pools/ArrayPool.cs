using System;

namespace Framework.Core.Pools
{
    /// <summary>
    /// 基于 2 的幂次分桶的数组对象池（等同于 <c>System.Buffers.ArrayPool&lt;T&gt;</c> 但可用于
    /// 不支持 .NET Standard 2.1 的旧版 Unity 项目）。
    ///
    /// ■ 分桶规则：长度向上取整到最近的 2 的幂次（最小 16，最大 ~1M）。
    ///   例：Rent(100) 返回长度为 128 的数组（实际可用 ≥ 100）。
    ///
    /// ■ 手动管理：
    /// <code>
    ///   int[] buf = ArrayPool&lt;int&gt;.Shared.Rent(count);
    ///   // 使用 buf[0..count-1]...
    ///   ArrayPool&lt;int&gt;.Shared.Return(buf);
    /// </code>
    ///
    /// ■ using 作用域（推荐）：
    /// <code>
    ///   using (ArrayPool&lt;float&gt;.Shared.Rent(256, out float[] buf))
    ///   {
    ///       // buf.Length >= 256
    ///   }   // 自动归还
    /// </code>
    ///
    /// ■ 注意：归还前请勿清空数组内容（除非有安全需要），
    ///   取出的数组可能含有上次使用残留的值，使用前自行初始化需要的范围。
    /// </summary>
    public sealed class ArrayPool<T>
    {
        /// <summary>全局共享实例（无锁设计，主线程安全）。</summary>
        public static readonly ArrayPool<T> Shared = new ArrayPool<T>();

        // 分桶参数
        private const int MinBucketSize    = 16;    // 桶0的数组长度
        private const int MaxBuckets       = 18;    // 桶17 = 16 * 2^17 = 2097152
        private const int MaxArraysPerBucket = 16;  // 每个桶最多缓存的数组数量

        // Stack 比 Queue 对 CPU 缓存更友好（LIFO，刚归还的数组还在 L1/L2 缓存里）
        private readonly Stack<T[]>[] _buckets = new Stack<T[]>[MaxBuckets];

        private ArrayPool() { }

        // ── 核心 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// 借出长度 <b>≥ minimumLength</b> 的数组。
        /// 超出最大桶范围时直接 <c>new</c>（不缓存）。
        /// </summary>
        public T[] Rent(int minimumLength)
        {
            if (minimumLength < 0) throw new ArgumentOutOfRangeException(nameof(minimumLength));
            if (minimumLength == 0) return Array.Empty<T>();

            int bucketIdx = SelectBucket(minimumLength);
            if (bucketIdx < MaxBuckets)
            {
                var bucket = _buckets[bucketIdx];
                if (bucket != null && bucket.Count > 0)
                    return bucket.Pop();

                return new T[BucketLength(bucketIdx)];
            }

            // 超大数组：直接分配，不入池
            return new T[minimumLength];
        }

        /// <summary>
        /// 借出数组并返回 using 作用域令牌（推荐方式）。
        /// </summary>
        /// <param name="minimumLength">所需最小长度。</param>
        /// <param name="array">借出的数组，长度 ≥ minimumLength。</param>
        public PooledArray<T> Rent(int minimumLength, out T[] array)
        {
            array = Rent(minimumLength);
            return new PooledArray<T>(this, array);
        }

        /// <summary>
        /// 归还数组。归还后不得再持有或访问该数组引用。
        /// </summary>
        /// <param name="array">要归还的数组（必须是由本池 <see cref="Rent"/> 借出的）。</param>
        /// <param name="clearArray">
        ///   归还前是否清零数组内容。
        ///   含有托管引用（class 类型元素）时建议设为 <c>true</c>，防止 GC 根泄漏。
        /// </param>
        public void Return(T[] array, bool clearArray = false)
        {
            if (array == null || array.Length == 0) return;

            if (clearArray)
                Array.Clear(array, 0, array.Length);

            int bucketIdx = SelectBucket(array.Length);
            if (bucketIdx >= MaxBuckets) return;  // 超大数组不入池

            var bucket = _buckets[bucketIdx] ??= new Stack<T[]>(MaxArraysPerBucket);
            if (bucket.Count < MaxArraysPerBucket)
                bucket.Push(array);
            // 池满则直接丢弃，由 GC 回收
        }

        // ── 预分配 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 预先向对应桶中压入 <paramref name="count"/> 个长度 ≥ <paramref name="minimumLength"/>
        /// 的数组实例。适合在 Loading 界面调用，避免游戏中首次 Rent 触发 GC。
        /// </summary>
        /// <param name="minimumLength">数组的最小长度（会向上取整到桶的实际长度）。</param>
        /// <param name="count">要预分配的数量（超过 MaxArraysPerBucket 的部分会被忽略）。</param>
        public void Warmup(int minimumLength, int count)
        {
            if (minimumLength <= 0 || count <= 0) return;

            int bucketIdx = SelectBucket(minimumLength);
            if (bucketIdx >= MaxBuckets) return;   // 超大数组不预热

            var bucket = _buckets[bucketIdx] ??= new Stack<T[]>(MaxArraysPerBucket);
            int toAdd  = Math.Min(count, MaxArraysPerBucket - bucket.Count);
            int len    = BucketLength(bucketIdx);

            for (int i = 0; i < toAdd; i++)
                bucket.Push(new T[len]);
        }

        // ── 调试 ─────────────────────────────────────────────────────────────

        /// <summary>打印各桶空闲数量（仅供调试）。</summary>
        public string GetStats()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < MaxBuckets; i++)
            {
                int count = _buckets[i]?.Count ?? 0;
                if (count > 0)
                    sb.Append($"  Bucket[{i}] len={BucketLength(i),7}: {count} idle\n");
            }
            return sb.Length > 0 ? sb.ToString() : "  (空)";
        }

        // ── 内部计算 ──────────────────────────────────────────────────────────

        // 将 length 映射到桶下标（向上取整到 MinBucketSize * 2^bucket）
        private static int SelectBucket(int length)
        {
            // length <= MinBucketSize → bucket 0
            // length <= MinBucketSize * 2 → bucket 1 ...
            int normalised = Math.Max(length, MinBucketSize);
            int bits = 0;
            int v = normalised - 1;     // -1 使恰好 2 幂次时不上推一级
            while (v >= MinBucketSize)
            {
                v >>= 1;
                bits++;
            }
            return bits;
        }

        private static int BucketLength(int bucket) => MinBucketSize << bucket;
    }

    /// <summary>
    /// <see cref="ArrayPool{T}.Rent(int, out T[])"/> 返回的作用域令牌。
    /// </summary>
    public readonly struct PooledArray<T> : IDisposable
    {
        private readonly ArrayPool<T> _pool;
        private readonly T[]          _array;

        internal PooledArray(ArrayPool<T> pool, T[] array) { _pool = pool; _array = array; }

        public void Dispose() => _pool?.Return(_array);
    }
}
