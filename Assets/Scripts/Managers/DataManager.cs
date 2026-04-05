using System;
using RPG.Items;
using RPG.Quests;
using RPG.Achievements;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// [已废弃] Managers.DataManager 兼容层 — 委托给 <see cref="RPG.Data.GameDataService"/>。
    ///
    /// 新代码请直接使用:
    ///   <code>RPG.Data.GameDataService.Instance.Skills.GetById("skill_fireball")</code>
    ///
    /// 此类保留仅为向后兼容，后续版本将移除。
    /// </summary>
    [Obsolete("使用 RPG.Data.GameDataService.Instance 替代。此类将在未来版本移除。")]
    public class DataManager : Framework.Base.SingletonMonoBehaviour<DataManager>
    {
        [Header("数据库（已废弃，GameDataService 接管后可留空）")]
        [SerializeField] private ItemDatabase        itemDatabase;
        [SerializeField] private SkillDatabase       skillDatabase;
        [SerializeField] private QuestDatabase       questDatabase;
        [SerializeField] private AchievementDatabase achievementDatabase;

        // Expose as typed properties for any remaining scene wiring
        public ItemDatabase        ItemDatabase        => itemDatabase;
        public SkillDatabase       SkillDatabase       => skillDatabase;
        public QuestDatabase       QuestDatabase       => questDatabase;
        public AchievementDatabase AchievementDatabase => achievementDatabase;

        protected override void Awake()
        {
            base.Awake();
        }

        // ── Facade (backward compat) ──────────────────────────────────────────

        public ItemData        GetItem(string id)  => RPG.Data.GameDataService.Instance?.GetItem(id)
                                                    ?? itemDatabase?.GetItem(id);
        public QuestData       GetQuest(string id) => RPG.Data.GameDataService.Instance?.GetQuest(id)
                                                    ?? questDatabase?.GetQuest(id);
        public AchievementData GetAchievement(string id)
            => RPG.Data.GameDataService.Instance?.Achievements?.GetById(id)
             ?? achievementDatabase?.GetAchievement(id);
    }

    // ── Legacy SkillDatabase (name-based lookup, kept for scene references) ───

    /// <summary>[已废弃] 使用 <see cref="RPG.Data.SkillDatabase"/> 替代。</summary>
    [Obsolete("使用 RPG.Data.SkillDatabase 替代。")]
    [CreateAssetMenu(fileName = "LegacySkillDatabase", menuName = "RPG/Databases/[Legacy] Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [SerializeField] private RPG.Skills.SkillData[] skills;

        public RPG.Skills.SkillData GetSkill(string skillName)
        {
            if (skills == null) return null;
            foreach (var s in skills)
                if (s != null && s.skillName == skillName) return s;
            return null;
        }

        public RPG.Skills.SkillData[] GetAllSkills() => skills ?? Array.Empty<RPG.Skills.SkillData>();
    }
}
