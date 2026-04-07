using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Scripting
{
    /// <summary>
    /// 编译结果 LRU 缓存。
    ///
    /// 以源码的 SHA-256 哈希值为键，缓存 <see cref="ScriptCompileResult"/>，避免重复编译相同脚本。
    ///
    /// <para>线程安全：所有公开方法均持有内部锁。</para>
    ///
    /// 使用示例：
    /// <code>
    /// var cache = new ScriptCache(maxEntries: 32);
    ///
    /// string hash = cache.Hash(source);
    /// if (!cache.TryGet(hash, out var result))
    /// {
    ///     result = compiler.Compile(source);
    ///     cache.Set(hash, result);
    /// }
    /// </code>
    /// </summary>
    public sealed class ScriptCache
    {
        private sealed class Entry
        {
            public ScriptCompileResult Result;
            public long                AccessTick;   // 单调递增访问计数，用于 LRU 驱逐
        }

        private readonly int _maxEntries;
        private readonly Dictionary<string, Entry> _dict;
        private readonly object _lock = new object();
        private long _tick;

        // ── 统计 ──────────────────────────────────────────────────────────────
        private int _hits;
        private int _misses;
        private int _evictions;

        public int CacheSize   { get { lock (_lock) return _dict.Count; } }
        public int MaxEntries  => _maxEntries;
        public int Hits        { get { lock (_lock) return _hits; } }
        public int Misses      { get { lock (_lock) return _misses; } }
        public int Evictions   { get { lock (_lock) return _evictions; } }

        // ── 构造 ──────────────────────────────────────────────────────────────

        /// <param name="maxEntries">缓存最大条目数；超出时驱逐最久未访问的条目。</param>
        public ScriptCache(int maxEntries = 32)
        {
            if (maxEntries < 1) throw new ArgumentOutOfRangeException(nameof(maxEntries));
            _maxEntries = maxEntries;
            _dict       = new Dictionary<string, Entry>(maxEntries * 2, StringComparer.Ordinal);
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>计算源码的 SHA-256 哈希（十六进制字符串），作为缓存键。</summary>
        public static string Hash(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            var sb = new StringBuilder(64);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>尝试从缓存获取编译结果。命中时更新 LRU 访问时间戳。</summary>
        public bool TryGet(string sourceHash, out ScriptCompileResult result)
        {
            lock (_lock)
            {
                if (_dict.TryGetValue(sourceHash, out var entry))
                {
                    entry.AccessTick = ++_tick;
                    result = entry.Result;
                    _hits++;
                    return true;
                }
                result = null;
                _misses++;
                return false;
            }
        }

        /// <summary>将编译结果存入缓存（超出容量时 LRU 驱逐）。</summary>
        public void Set(string sourceHash, ScriptCompileResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            lock (_lock)
            {
                if (_dict.TryGetValue(sourceHash, out var existing))
                {
                    existing.Result      = result;
                    existing.AccessTick  = ++_tick;
                    return;
                }

                if (_dict.Count >= _maxEntries)
                    EvictLRU();

                _dict[sourceHash] = new Entry { Result = result, AccessTick = ++_tick };
            }
        }

        /// <summary>手动移除指定条目（例如源码文件已修改时）。</summary>
        public bool Invalidate(string sourceHash)
        {
            lock (_lock)
                return _dict.Remove(sourceHash);
        }

        /// <summary>清空所有缓存条目。</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _dict.Clear();
                _tick      = 0;
                _hits      = 0;
                _misses    = 0;
                _evictions = 0;
            }
        }

        /// <summary>返回统计摘要字符串，用于调试/Editor UI。</summary>
        public string GetStats()
        {
            lock (_lock)
                return $"ScriptCache: {_dict.Count}/{_maxEntries} entries | " +
                       $"Hits={_hits} Misses={_misses} Evictions={_evictions}";
        }

        // ── 私有 ──────────────────────────────────────────────────────────────

        // 注意：调用方需持锁
        private void EvictLRU()
        {
            string lruKey  = null;
            long   lruTick = long.MaxValue;
            foreach (var kv in _dict)
            {
                if (kv.Value.AccessTick < lruTick)
                {
                    lruTick = kv.Value.AccessTick;
                    lruKey  = kv.Key;
                }
            }
            if (lruKey != null)
            {
                _dict.Remove(lruKey);
                _evictions++;
            }
        }
    }
}
