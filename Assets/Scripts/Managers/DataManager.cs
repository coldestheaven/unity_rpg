using UnityEngine;
using RPG.Items;
using RPG.Quests;
using RPG.Achievements;

namespace Managers
{
    public class DataManager : Framework.Base.SingletonMonoBehaviour<DataManager>
    {
        [Header("Databases")]
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private AchievementDatabase achievementDatabase;

        public ItemDatabase ItemDatabase => itemDatabase;
        public SkillDatabase SkillDatabase => skillDatabase;
        public QuestDatabase QuestDatabase => questDatabase;
        public AchievementDatabase AchievementDatabase => achievementDatabase;

        protected override void Awake()
        {
            base.Awake();
            LoadDatabases();
        }

        private void LoadDatabases()
        {
            if (itemDatabase == null)
                itemDatabase = Resources.Load<ItemDatabase>("Databases/ItemDatabase");

            if (skillDatabase == null)
                skillDatabase = Resources.Load<SkillDatabase>("Databases/SkillDatabase");

            if (questDatabase == null)
                questDatabase = Resources.Load<QuestDatabase>("Databases/QuestDatabase");

            if (achievementDatabase == null)
                achievementDatabase = Resources.Load<AchievementDatabase>("Databases/AchievementDatabase");

            Debug.Log("Databases loaded");
        }

        public ItemData GetItem(string id) => itemDatabase?.GetItem(id);
        public QuestData GetQuest(string id) => questDatabase?.GetQuest(id);
        public AchievementData GetAchievement(string id) => achievementDatabase?.GetAchievement(id);
    }

    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "RPG/Databases/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [SerializeField] private RPG.Skills.SkillData[] skills;

        public RPG.Skills.SkillData GetSkill(string skillName)
        {
            foreach (var skill in skills)
            {
                if (skill != null && skill.skillName == skillName) return skill;
            }
            return null;
        }

        public RPG.Skills.SkillData[] GetAllSkills() => skills;
    }
}
