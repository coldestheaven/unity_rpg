using System;
using System.Collections.Generic;
using RPG.Enemy;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 敌人数据库 — 存储所有 <see cref="EnemyData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;EnemyData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Enemy Database
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "RPG/Data/Enemy Database")]
    public class EnemyDatabase : RepositoryBase<EnemyData>
    {
        [Serializable]
        public class EnemyEntry
        {
            [Tooltip("敌人唯一 ID。建议格式: enemy_goblin")]
            public string enemyId;
            public EnemyData enemyData;
        }

        [SerializeField] private EnemyEntry[] _enemies = Array.Empty<EnemyEntry>();

        protected override void PopulateDictionary(Dictionary<string, EnemyData> dict)
        {
            if (_enemies == null) return;
            int skipped = 0;
            foreach (var e in _enemies)
            {
                if (e == null || e.enemyData == null || string.IsNullOrEmpty(e.enemyId))
                { skipped++; continue; }
                dict[e.enemyId] = e.enemyData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[EnemyDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按敌人类型过滤。</summary>
        public IReadOnlyList<EnemyData> GetByType(EnemyType type)
            => Query(e => e.enemyType == type);
    }
}
