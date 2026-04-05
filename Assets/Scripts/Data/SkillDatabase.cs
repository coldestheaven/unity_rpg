using System;
using System.Collections.Generic;
using Framework.Interfaces;
using RPG.Skills;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 技能数据库 — 存储所有 <see cref="SkillData"/> 资产，实现 <see cref="IRepository{T}"/>。
    ///
    /// 创建: Assets/Create → RPG/Data/Skill Database
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "RPG/Data/Skill Database")]
    public class SkillDatabase : ScriptableObject, IRepository<SkillData>
    {
        [Serializable]
        public class SkillEntry
        {
            [Tooltip("技能唯一 ID，用于 GetById 查询。建议格式: skill_fireball")]
            public string skillId;
            public SkillData skillData;
        }

        [SerializeField] private SkillEntry[] _skills = Array.Empty<SkillEntry>();

        private Dictionary<string, SkillData> _dict;

        // ── 初始化 ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            _dict = new Dictionary<string, SkillData>(_skills.Length);
            int skipped = 0;
            foreach (var entry in _skills)
            {
                if (entry == null || entry.skillData == null || string.IsNullOrEmpty(entry.skillId))
                { skipped++; continue; }
                _dict[entry.skillId] = entry.skillData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[SkillDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── IRepository<SkillData> ────────────────────────────────────────────

        public SkillData GetById(string id)
        {
            EnsureReady();
            return _dict.TryGetValue(id ?? "", out var v) ? v : null;
        }

        public bool Exists(string id)
        {
            EnsureReady();
            return _dict.ContainsKey(id ?? "");
        }

        public IReadOnlyList<SkillData> GetAll()
        {
            EnsureReady();
            return new List<SkillData>(_dict.Values);
        }

        public int Count { get { EnsureReady(); return _dict.Count; } }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按技能类型过滤。</summary>
        public IReadOnlyList<SkillData> GetByType(SkillType type)
        {
            EnsureReady();
            var result = new List<SkillData>();
            foreach (var s in _dict.Values)
                if (s.skillType == type) result.Add(s);
            return result;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void EnsureReady()
        {
            if (_dict == null) Initialize();
        }
    }
}
