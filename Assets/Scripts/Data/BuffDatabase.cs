using System;
using System.Collections.Generic;
using Framework.Interfaces;
using RPG.Buff;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Buff 数据库 — 存储所有 <see cref="BuffData"/> 资产，实现 <see cref="IRepository{T}"/>。
    ///
    /// 创建: Assets/Create → RPG/Data/Buff Database
    /// </summary>
    [CreateAssetMenu(fileName = "BuffDatabase", menuName = "RPG/Data/Buff Database")]
    public class BuffDatabase : ScriptableObject, IRepository<BuffData>
    {
        [Serializable]
        public class BuffEntry
        {
            [Tooltip("Buff 唯一 ID。留空时自动使用 BuffData.buffId 字段。")]
            public string overrideId;
            public BuffData buffData;
        }

        [SerializeField] private BuffEntry[] _buffs = Array.Empty<BuffEntry>();

        private Dictionary<string, BuffData> _dict;

        // ── 初始化 ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            _dict = new Dictionary<string, BuffData>(_buffs.Length);
            int skipped = 0;
            foreach (var entry in _buffs)
            {
                if (entry == null || entry.buffData == null) { skipped++; continue; }

                // Entry override ID → BuffData.buffId → asset name (fallback chain)
                string id = !string.IsNullOrEmpty(entry.overrideId) ? entry.overrideId
                          : !string.IsNullOrEmpty(entry.buffData.buffId) ? entry.buffData.buffId
                          : entry.buffData.name;

                if (string.IsNullOrEmpty(id)) { skipped++; continue; }
                _dict[id] = entry.buffData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[BuffDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── IRepository<BuffData> ─────────────────────────────────────────────

        public BuffData GetById(string id)
        {
            EnsureReady();
            return _dict.TryGetValue(id ?? "", out var v) ? v : null;
        }

        public bool Exists(string id)
        {
            EnsureReady();
            return _dict.ContainsKey(id ?? "");
        }

        public IReadOnlyList<BuffData> GetAll()
        {
            EnsureReady();
            return new List<BuffData>(_dict.Values);
        }

        public int Count { get { EnsureReady(); return _dict.Count; } }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按分类过滤（Buff / Debuff / Neutral）。</summary>
        public IReadOnlyList<BuffData> GetByCategory(BuffCategory category)
        {
            EnsureReady();
            var result = new List<BuffData>();
            foreach (var b in _dict.Values)
                if (b.category == category) result.Add(b);
            return result;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void EnsureReady()
        {
            if (_dict == null) Initialize();
        }
    }
}
