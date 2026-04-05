using System;
using System.Collections.Generic;
using RPG.Skills;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 技能数据库 — 存储所有 <see cref="SkillData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;SkillData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Skill Database
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "RPG/Data/Skill Database")]
    public class SkillDatabase : RepositoryBase<SkillData>
    {
        [Serializable]
        public class SkillEntry
        {
            [Tooltip("技能唯一 ID。建议格式: skill_fireball")]
            public string skillId;
            public SkillData skillData;
        }

        [SerializeField] private SkillEntry[] _skills = Array.Empty<SkillEntry>();

        protected override void PopulateDictionary(Dictionary<string, SkillData> dict)
        {
            if (_skills == null) return;
            int skipped = 0;
            foreach (var e in _skills)
            {
                if (e == null || e.skillData == null || string.IsNullOrEmpty(e.skillId))
                { skipped++; continue; }
                dict[e.skillId] = e.skillData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[SkillDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按技能类型过滤。</summary>
        public System.Collections.Generic.IReadOnlyList<SkillData> GetByType(SkillType type)
            => Query(s => s.skillType == type);
    }
}
