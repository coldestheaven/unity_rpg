using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Building
{
    /// <summary>
    /// 建筑数据库 — 存储所有 <see cref="BuildingData"/> 资产。
    ///
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;BuildingData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Building Database
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "RPG/Data/Building Database")]
    public sealed class BuildingDatabase : RepositoryBase<BuildingData>
    {
        [Serializable]
        public sealed class BuildingEntry
        {
            [Tooltip("建筑唯一 ID，与 BuildingData.buildingId 保持一致。")]
            public string      buildingId;
            public BuildingData data;
        }

        [SerializeField] private BuildingEntry[] _buildings = Array.Empty<BuildingEntry>();

        protected override void PopulateDictionary(Dictionary<string, BuildingData> dict)
        {
            if (_buildings == null) return;
            int skipped = 0;
            foreach (var e in _buildings)
            {
                if (e == null || e.data == null || string.IsNullOrEmpty(e.buildingId))
                { skipped++; continue; }
                dict[e.buildingId] = e.data;
            }
            if (skipped > 0)
                Debug.LogWarning($"[BuildingDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        /// <summary>按分类过滤。</summary>
        public System.Collections.Generic.IReadOnlyList<BuildingData> GetByCategory(BuildingCategory cat)
            => Query(b => b.category == cat);
    }
}
