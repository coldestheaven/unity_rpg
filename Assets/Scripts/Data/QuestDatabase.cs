using System;
using System.Collections.Generic;
using RPG.Data;
using UnityEngine;

namespace RPG.Quests
{
    /// <summary>
    /// 任务数据库 — 存储所有 <see cref="QuestData"/> 资产。
    /// 继承 <see cref="RepositoryBase{T}"/>，自动获得完整的 IRepository&lt;QuestData&gt; 实现。
    ///
    /// 创建: Assets/Create → RPG/Data/Quest Database
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "RPG/Data/Quest Database")]
    public class QuestDatabase : RepositoryBase<QuestData>
    {
        [Serializable]
        public class QuestEntry
        {
            [Tooltip("任务唯一 ID。建议格式: quest_main_01")]
            public string questId;
            public QuestData questData;
        }

        [SerializeField] private QuestEntry[] quests = Array.Empty<QuestEntry>();

        protected override void PopulateDictionary(Dictionary<string, QuestData> dict)
        {
            if (quests == null) return;
            int skipped = 0;
            foreach (var e in quests)
            {
                if (e == null || e.questData == null || string.IsNullOrEmpty(e.questId))
                { skipped++; continue; }
                dict[e.questId] = e.questData;
            }
            if (skipped > 0)
                Debug.LogWarning($"[QuestDatabase] {skipped} 条记录缺少 ID 或数据，已跳过。");
        }

        // ── 额外查询 ──────────────────────────────────────────────────────────

        /// <summary>按任务类型过滤。</summary>
        public IReadOnlyList<QuestData> GetByType(QuestType type) => Query(q => q.questType == type);

        /// <summary>获取所有主线任务。</summary>
        public IReadOnlyList<QuestData> GetMainQuests()  => GetByType(QuestType.Main);

        /// <summary>获取所有支线任务。</summary>
        public IReadOnlyList<QuestData> GetSideQuests()  => GetByType(QuestType.Side);
    }
}
