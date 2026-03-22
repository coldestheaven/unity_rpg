using UnityEngine;
using System.Collections.Generic;

namespace RPG.Quests
{
    /// <summary>
    /// 任务数据库 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "RPG/Data/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        [System.Serializable]
        public class QuestEntry
        {
            public string questId;
            public QuestData questData;
        }

        public QuestEntry[] quests;

        private Dictionary<string, QuestData> questDictionary;

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Initialize()
        {
            questDictionary = new Dictionary<string, QuestData>();

            if (quests != null)
            {
                foreach (var entry in quests)
                {
                    if (entry != null && entry.questData != null && !string.IsNullOrEmpty(entry.questId))
                    {
                        questDictionary[entry.questId] = entry.questData;
                    }
                }
            }

            Debug.Log($"QuestDatabase initialized with {questDictionary.Count} quests");
        }

        /// <summary>
        /// 获取任务数据
        /// </summary>
        public QuestData GetQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            // 确保字典已初始化
            if (questDictionary == null)
            {
                Initialize();
            }

            questDictionary.TryGetValue(questId, out QuestData questData);
            return questData;
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public QuestData[] GetAllQuests()
        {
            if (questDictionary == null)
            {
                Initialize();
            }

            return new List<QuestData>(questDictionary.Values).ToArray();
        }

        /// <summary>
        /// 根据类型获取任务
        /// </summary>
        public QuestData[] GetQuestsByType(QuestType type)
        {
            List<QuestData> result = new List<QuestData>();

            foreach (var quest in questDictionary.Values)
            {
                if (quest.questType == type)
                {
                    result.Add(quest);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 获取主线任务
        /// </summary>
        public QuestData[] GetMainQuests()
        {
            return GetQuestsByType(QuestType.Main);
        }

        /// <summary>
        /// 获取支线任务
        /// </summary>
        public QuestData[] GetSideQuests()
        {
            return GetQuestsByType(QuestType.Side);
        }

        /// <summary>
        /// 添加任务到数据库
        /// </summary>
        public void AddQuest(string questId, QuestData questData)
        {
            if (questDictionary == null)
            {
                Initialize();
            }

            questDictionary[questId] = questData;

            // 同时更新数组(运行时)
            List<QuestEntry> entryList = new List<QuestEntry>(quests);
            entryList.Add(new QuestEntry { questId = questId, questData = questData });
            quests = entryList.ToArray();
        }

        /// <summary>
        /// 检查任务是否存在
        /// </summary>
        public bool ContainsQuest(string questId)
        {
            return questDictionary.ContainsKey(questId);
        }
    }
}
