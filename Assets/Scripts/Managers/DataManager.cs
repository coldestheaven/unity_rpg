using UnityEngine;

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
            {
                itemDatabase = Resources.Load<ItemDatabase>("Databases/ItemDatabase");
            }
            if (skillDatabase == null)
            {
                skillDatabase = Resources.Load<SkillDatabase>("Databases/SkillDatabase");
            }
            if (questDatabase == null)
            {
                questDatabase = Resources.Load<QuestDatabase>("Databases/QuestDatabase");
            }
            if (achievementDatabase == null)
            {
                achievementDatabase = Resources.Load<AchievementDatabase>("Databases/AchievementDatabase");
            }

            Debug.Log("Databases loaded");
        }

        public T GetData<T>(string id) where T : Framework.Base.ScriptableObjectBase
        {
            if (typeof(T) == typeof(ItemData))
            {
                return itemDatabase.GetItem(id) as T;
            }
            else if (typeof(T) == typeof(SkillData))
            {
                return skillDatabase.GetSkill(id) as T;
            }
            else if (typeof(T) == typeof(QuestData))
            {
                return questDatabase.GetQuest(id) as T;
            }
            else if (typeof(T) == typeof(AchievementData))
            {
                return achievementDatabase.GetAchievement(id) as T;
            }

            return null;
        }
    }

    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RPG/Databases/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] items;

        public ItemData GetItem(string id)
        {
            foreach (var item in items)
            {
                if (item.ID == id) return item;
            }
            return null;
        }

        public ItemData[] GetAllItems() => items;
    }

    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "RPG/Databases/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [SerializeField] private SkillData[] skills;

        public SkillData GetSkill(string id)
        {
            foreach (var skill in skills)
            {
                if (skill.ID == id) return skill;
            }
            return null;
        }

        public SkillData[] GetAllSkills() => skills;
    }

    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "RPG/Databases/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        [SerializeField] private QuestData[] quests;

        public QuestData GetQuest(string id)
        {
            foreach (var quest in quests)
            {
                if (quest.ID == id) return quest;
            }
            return null;
        }

        public QuestData[] GetAllQuests() => quests;
    }

    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "RPG/Databases/Achievement Database")]
    public class AchievementDatabase : ScriptableObject
    {
        [SerializeField] private AchievementData[] achievements;

        public AchievementData GetAchievement(string id)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.ID == id) return achievement;
            }
            return null;
        }

        public AchievementData[] GetAllAchievements() => achievements;
    }
}

// Placeholder data classes
public class ItemData : Framework.Base.ScriptableObjectBase { }
public class SkillData : Framework.Base.ScriptableObjectBase { }
public class QuestData : Framework.Base.ScriptableObjectBase { }
public class AchievementData : Framework.Base.ScriptableObjectBase { }
