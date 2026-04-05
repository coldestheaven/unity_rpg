using System;
using System.Collections.Generic;
using RPG.Data;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 物品数据库 — 存储所有 <see cref="ItemData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;ItemData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Item Database
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RPG/Data/Item Database")]
    public class ItemDatabase : RepositoryBase<ItemData>
    {
        [Serializable]
        public class ItemEntry
        {
            [Tooltip("物品唯一 ID。建议格式: item_sword")]
            public string itemId;
            public ItemData itemData;
        }

        [SerializeField] private ItemEntry[] items = Array.Empty<ItemEntry>();

        protected override void PopulateDictionary(Dictionary<string, ItemData> dict)
        {
            if (items == null) return;
            int skipped = 0;
            foreach (var e in items)
            {
                if (e == null || e.itemData == null || string.IsNullOrEmpty(e.itemId))
                { skipped++; continue; }
                dict[e.itemId] = e.itemData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[ItemDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按物品类型过滤。</summary>
        public IReadOnlyList<ItemData> GetByType(ItemType type)
            => Query(i => i.itemType == type);

        /// <summary>按标签过滤。</summary>
        public IReadOnlyList<ItemData> GetByTag(string tag)
            => Query(i => i.HasTag(tag));
    }
}
