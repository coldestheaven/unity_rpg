using System;
using System.Collections.Generic;
using RPG.Buff;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Buff 数据库 — 存储所有 <see cref="BuffData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;BuffData&gt; 实现。
    ///
    /// ID 解析顺序: overrideId → BuffData.buffId → 资产名称。
    ///
    /// 创建: Assets/Create → RPG/Data/Buff Database
    /// </summary>
    [CreateAssetMenu(fileName = "BuffDatabase", menuName = "RPG/Data/Buff Database")]
    public class BuffDatabase : RepositoryBase<BuffData>
    {
        [Serializable]
        public class BuffEntry
        {
            [Tooltip("Buff 唯一 ID（留空时自动使用 BuffData.buffId，再留空则用资产名）。")]
            public string overrideId;
            public BuffData buffData;
        }

        [SerializeField] private BuffEntry[] _buffs = Array.Empty<BuffEntry>();

        protected override void PopulateDictionary(Dictionary<string, BuffData> dict)
        {
            if (_buffs == null) return;
            int skipped = 0;
            foreach (var e in _buffs)
            {
                if (e == null || e.buffData == null) { skipped++; continue; }

                // ID 解析优先级: overrideId → buffData.buffId → 资产名称
                string id = !string.IsNullOrEmpty(e.overrideId)      ? e.overrideId
                          : !string.IsNullOrEmpty(e.buffData.buffId)  ? e.buffData.buffId
                          : e.buffData.name;

                if (string.IsNullOrEmpty(id)) { skipped++; continue; }
                dict[id] = e.buffData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[BuffDatabase] {skipped} 条记录缺少有效 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按 Buff 分类过滤（Buff / Debuff / Neutral）。</summary>
        public IReadOnlyList<BuffData> GetByCategory(BuffCategory category)
            => Query(b => b.category == category);
    }
}
