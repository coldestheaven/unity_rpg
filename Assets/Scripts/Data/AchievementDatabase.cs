using UnityEngine;
using System.Collections.Generic;

namespace RPG.Achievements
{
    /// <summary>
    /// 成就数据库 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "RPG/Data/Achievement Database")]
    public class AchievementDatabase : ScriptableObject
    {
        [System.Serializable]
        public class AchievementEntry
        {
            public string achievementId;
            public AchievementData achievementData;
        }

        public AchievementEntry[] achievements;

        private Dictionary<string, AchievementData> achievementDictionary;

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Initialize()
        {
            achievementDictionary = new Dictionary<string, AchievementData>();

            if (achievements != null)
            {
                foreach (var entry in achievements)
                {
                    if (entry != null && entry.achievementData != null && !string.IsNullOrEmpty(entry.achievementId))
                    {
                        achievementDictionary[entry.achievementId] = entry.achievementData;
                    }
                }
            }

            Debug.Log($"AchievementDatabase initialized with {achievementDictionary.Count} achievements");
        }

        /// <summary>
        /// 获取成就数据
        /// </summary>
        public AchievementData GetAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                return null;
            }

            // 确保字典已初始化
            if (achievementDictionary == null)
            {
                Initialize();
            }

            achievementDictionary.TryGetValue(achievementId, out AchievementData achievementData);
            return achievementData;
        }

        /// <summary>
        /// 获取所有成就
        /// </summary>
        public AchievementData[] GetAllAchievements()
        {
            if (achievementDictionary == null)
            {
                Initialize();
            }

            return new List<AchievementData>(achievementDictionary.Values).ToArray();
        }

        /// <summary>
        /// 根据类型获取成就
        /// </summary>
        public AchievementData[] GetAchievementsByType(AchievementType type)
        {
            List<AchievementData> result = new List<AchievementData>();

            foreach (var achievement in achievementDictionary.Values)
            {
                if (achievement.achievementType == type)
                {
                    result.Add(achievement);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 获取未隐藏的成就
        /// </summary>
        public AchievementData[] GetVisibleAchievements()
        {
            List<AchievementData> result = new List<AchievementData>();

            foreach (var achievement in achievementDictionary.Values)
            {
                if (!achievement.isHidden)
                {
                    result.Add(achievement);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 添加成就到数据库
        /// </summary>
        public void AddAchievement(string achievementId, AchievementData achievementData)
        {
            if (achievementDictionary == null)
            {
                Initialize();
            }

            achievementDictionary[achievementId] = achievementData;

            // 同时更新数组(运行时)
            List<AchievementEntry> entryList = new List<AchievementEntry>(achievements);
            entryList.Add(new AchievementEntry { achievementId = achievementId, achievementData = achievementData });
            achievements = entryList.ToArray();
        }

        /// <summary>
        /// 检查成就是否存在
        /// </summary>
        public bool ContainsAchievement(string achievementId)
        {
            return achievementDictionary.ContainsKey(achievementId);
        }
    }
}
