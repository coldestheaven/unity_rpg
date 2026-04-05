using System;
using System.Collections.Generic;
using RPG.Data;
using UnityEngine;

namespace RPG.Achievements
{
    /// <summary>
    /// 成就数据库 — 存储所有 <see cref="AchievementData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;AchievementData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Achievement Database
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "RPG/Data/Achievement Database")]
    public class AchievementDatabase : RepositoryBase<AchievementData>
    {
        [Serializable]
        public class AchievementEntry
        {
            [Tooltip("成就唯一 ID。建议格式: ach_first_kill")]
            public string achievementId;
            public AchievementData achievementData;
        }

        [SerializeField] private AchievementEntry[] achievements = Array.Empty<AchievementEntry>();

        protected override void PopulateDictionary(Dictionary<string, AchievementData> dict)
        {
            if (achievements == null) return;
            int skipped = 0;
            foreach (var e in achievements)
            {
                if (e == null || e.achievementData == null || string.IsNullOrEmpty(e.achievementId))
                { skipped++; continue; }
                dict[e.achievementId] = e.achievementData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[AchievementDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按成就类型过滤。</summary>
        public IReadOnlyList<AchievementData> GetByType(AchievementType type)
            => Query(a => a.achievementType == type);
    }
}
