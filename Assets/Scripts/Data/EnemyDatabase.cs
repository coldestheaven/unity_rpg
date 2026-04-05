using System;
using System.Collections.Generic;
using Framework.Interfaces;
using RPG.Enemy;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 敌人数据库 — 存储所有 <see cref="EnemyData"/> 资产，实现 <see cref="IRepository{T}"/>。
    ///
    /// 创建: Assets/Create → RPG/Data/Enemy Database
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "RPG/Data/Enemy Database")]
    public class EnemyDatabase : ScriptableObject, IRepository<EnemyData>
    {
        [Serializable]
        public class EnemyEntry
        {
            [Tooltip("敌人唯一 ID，用于 GetById 查询。建议格式: enemy_goblin")]
            public string enemyId;
            public EnemyData enemyData;
        }

        [SerializeField] private EnemyEntry[] _enemies = Array.Empty<EnemyEntry>();

        private Dictionary<string, EnemyData> _dict;

        // ── 初始化 ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            _dict = new Dictionary<string, EnemyData>(_enemies.Length);
            int skipped = 0;
            foreach (var entry in _enemies)
            {
                if (entry == null || entry.enemyData == null || string.IsNullOrEmpty(entry.enemyId))
                { skipped++; continue; }
                _dict[entry.enemyId] = entry.enemyData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[EnemyDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── IRepository<EnemyData> ────────────────────────────────────────────

        public EnemyData GetById(string id)
        {
            EnsureReady();
            return _dict.TryGetValue(id ?? "", out var v) ? v : null;
        }

        public bool Exists(string id)
        {
            EnsureReady();
            return _dict.ContainsKey(id ?? "");
        }

        public IReadOnlyList<EnemyData> GetAll()
        {
            EnsureReady();
            return new List<EnemyData>(_dict.Values);
        }

        public int Count { get { EnsureReady(); return _dict.Count; } }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按敌人类型过滤。</summary>
        public IReadOnlyList<EnemyData> GetByType(EnemyType type)
        {
            EnsureReady();
            var result = new List<EnemyData>();
            foreach (var e in _dict.Values)
                if (e.enemyType == type) result.Add(e);
            return result;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void EnsureReady()
        {
            if (_dict == null) Initialize();
        }
    }
}
