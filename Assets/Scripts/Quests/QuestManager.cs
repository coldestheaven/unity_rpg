using UnityEngine;
using System;
using System.Collections.Generic;
using RPG.Core;

namespace RPG.Quests
{
    /// <summary>
    /// 任务管理器
    /// </summary>
    public class QuestManager : Singleton<QuestManager>
    {
        [Header("任务数据库")]
        public QuestDatabase questDatabase;

        private Dictionary<string, QuestInstance> activeQuests;
        private Dictionary<string, QuestInstance> completedQuests;
        private Dictionary<string, QuestInstance> failedQuests;

        public int ActiveQuestCount => activeQuests.Count;
        public int CompletedQuestCount => completedQuests.Count;
        public QuestInstance[] ActiveQuests => new List<QuestInstance>(activeQuests.Values).ToArray();

        public event Action<QuestInstance> OnQuestStarted;
        public event Action<QuestInstance> OnQuestCompleted;
        public event Action<QuestInstance> OnQuestFailed;
        public event Action<QuestInstance, QuestObjective> OnObjectiveProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            InitializeQuestSystem();
            SubscribeToEvents();
        }

        private void InitializeQuestSystem()
        {
            activeQuests = new Dictionary<string, QuestInstance>();
            completedQuests = new Dictionary<string, QuestInstance>();
            failedQuests = new Dictionary<string, QuestInstance>();

            if (questDatabase == null)
            {
                questDatabase = Resources.Load<QuestDatabase>("QuestDatabase");
            }
        }

        private void SubscribeToEvents()
        {
            // 订阅游戏事件以自动更新任务进度
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("EnemyKilled", OnEnemyKilled);
                EventManager.Instance.AddListener("ItemPickedUp", OnItemPickedUp);
                EventManager.Instance.AddListener("PlayerDied", OnPlayerDied);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("EnemyKilled", OnEnemyKilled);
                EventManager.Instance.RemoveListener("ItemPickedUp", OnItemPickedUp);
                EventManager.Instance.RemoveListener("PlayerDied", OnPlayerDied);
            }
        }

        #region Quest Management

        /// <summary>
        /// 接取任务
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"Quest {questId} is already active");
                return false;
            }

            QuestData questData = questDatabase?.GetQuest(questId);
            if (questData == null)
            {
                Debug.LogError($"Quest data not found: {questId}");
                return false;
            }

            // 创建任务实例
            QuestInstance questInstance = new QuestInstance(questData);

            // 检查是否可以开始任务
            if (!questInstance.CanStart())
            {
                Debug.LogWarning($"Cannot start quest: {questId}");
                return false;
            }

            // 开始任务
            questInstance.StartQuest();
            activeQuests[questId] = questInstance;

            OnQuestStarted?.Invoke(questInstance);

            EventManager.Instance?.TriggerEvent("QuestStarted", new QuestEventArgs
            {
                questId = questId,
                questName = questData.questName,
                questType = questData.questType
            });

            Debug.Log($"Quest started: {questData.questName}");
            return true;
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            QuestInstance questInstance = activeQuests[questId];
            questInstance.CompleteQuest();

            // 移动到已完成列表
            activeQuests.Remove(questId);
            completedQuests[questId] = questInstance;

            // 给予奖励
            GiveQuestRewards(questInstance.QuestData);

            OnQuestCompleted?.Invoke(questInstance);

            // 检查是否有后续任务
            CheckForFollowUpQuests(questId);

            return true;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public bool AbandonQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            QuestInstance questInstance = activeQuests[questId];
            questInstance.AbandonQuest();

            activeQuests.Remove(questId);

            Debug.Log($"Quest abandoned: {questId}");
            return true;
        }

        /// <summary>
        /// 任务失败
        /// </summary>
        public bool FailQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            QuestInstance questInstance = activeQuests[questId];
            questInstance.FailQuest();

            activeQuests.Remove(questId);
            failedQuests[questId] = questInstance;

            OnQuestFailed?.Invoke(questInstance);

            return true;
        }

        #endregion

        #region Quest Progress

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public void UpdateObjectiveProgress(string questId, string objectiveId, int amount = 1)
        {
            if (activeQuests.TryGetValue(questId, out QuestInstance questInstance))
            {
                questInstance.UpdateObjectiveProgress(objectiveId, amount);
                OnObjectiveProgressChanged?.Invoke(questInstance, questInstance.QuestData.GetObjectiveById(objectiveId));
            }
        }

        /// <summary>
        /// 通过事件自动更新任务进度
        /// </summary>
        private void OnEnemyKilled(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is string enemyId)
            {
                CheckQuestObjectives(QuestObjectiveType.KillEnemy, enemyId);
            }
        }

        private void OnItemPickedUp(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is RPG.Items.ItemPickupEventArgs itemArgs)
            {
                CheckQuestObjectives(QuestObjectiveType.CollectItem, itemArgs.itemName);
            }
        }

        private void OnPlayerDied(object[] args)
        {
            // 检查是否有生存任务失败
            foreach (var questInstance in activeQuests.Values)
            {
                foreach (var objective in questInstance.QuestData.objectives)
                {
                    if (objective.objectiveType == QuestObjectiveType.Survive)
                    {
                        questInstance.FailQuest();
                        break;
                    }
                }
            }
        }

        private void CheckQuestObjectives(QuestObjectiveType objectiveType, string targetId)
        {
            foreach (var questInstance in activeQuests.Values)
            {
                foreach (var objective in questInstance.QuestData.objectives)
                {
                    if (objective.objectiveType == objectiveType &&
                        (objective.targetId == targetId || objective.targetId == ""))
                    {
                        questInstance.UpdateObjectiveProgress(objective.objectiveId);
                    }
                }
            }
        }

        #endregion

        #region Quest Rewards

        private void GiveQuestRewards(QuestData questData)
        {
            // 经验奖励
            if (questData.experienceReward > 0)
            {
                PlayerProgressManager.Instance?.AddExperience(questData.experienceReward);
            }

            // 金币奖励
            if (questData.goldReward > 0)
            {
                PlayerProgressManager.Instance?.AddGold(questData.goldReward);
            }

            // 物品奖励
            if (questData.itemRewards != null)
            {
                foreach (var reward in questData.itemRewards)
                {
                    if (reward.item != null && reward.quantity > 0)
                    {
                        RPG.Items.ItemSystem.Instance?.AddItem(reward.item, reward.quantity);
                    }
                }
            }

            EventManager.Instance?.TriggerEvent("QuestRewardsClaimed", new QuestRewardEventArgs
            {
                questId = questData.questId,
                experience = questData.experienceReward,
                gold = questData.goldReward
            });

            Debug.Log($"Rewards given for quest: {questData.questName}");
        }

        #endregion

        #region Quest Queries

        /// <summary>
        /// 获取任务实例
        /// </summary>
        public QuestInstance GetQuestInstance(string questId)
        {
            if (activeQuests.TryGetValue(questId, out QuestInstance activeQuest))
            {
                return activeQuest;
            }

            if (completedQuests.TryGetValue(questId, out QuestInstance completedQuest))
            {
                return completedQuest;
            }

            if (failedQuests.TryGetValue(questId, out QuestInstance failedQuest))
            {
                return failedQuest;
            }

            return null;
        }

        /// <summary>
        /// 检查任务是否活跃
        /// </summary>
        public bool IsQuestActive(string questId)
        {
            return activeQuests.ContainsKey(questId);
        }

        /// <summary>
        /// 检查任务是否已完成
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return completedQuests.ContainsKey(questId);
        }

        /// <summary>
        /// 获取可接取的任务列表
        /// </summary>
        public QuestData[] GetAvailableQuests()
        {
            List<QuestData> availableQuests = new List<QuestData>();

            if (questDatabase != null)
            {
                QuestData[] allQuests = questDatabase.GetAllQuests();

                foreach (var questData in allQuests)
                {
                    if (IsQuestAvailable(questData))
                    {
                        availableQuests.Add(questData);
                    }
                }
            }

            return availableQuests.ToArray();
        }

        private bool IsQuestAvailable(QuestData questData)
        {
            // 已经在活跃或已完成
            if (activeQuests.ContainsKey(questData.questId) ||
                completedQuests.ContainsKey(questData.questId))
            {
                return false;
            }

            // 检查前置任务
            if (questData.prerequisiteQuests != null)
            {
                foreach (var prerequisiteId in questData.prerequisiteQuests)
                {
                    if (!completedQuests.ContainsKey(prerequisiteId))
                    {
                        return false;
                    }
                }
            }

            // 检查等级要求
            // TODO: 从玩家系统获取当前等级

            return true;
        }

        private void CheckForFollowUpQuests(string completedQuestId)
        {
            if (questDatabase == null) return;

            QuestData[] allQuests = questDatabase.GetAllQuests();

            foreach (var questData in allQuests)
            {
                if (questData.prerequisiteQuests != null)
                {
                    foreach (var prerequisiteId in questData.prerequisiteQuests)
                    {
                        if (prerequisiteId == completedQuestId)
                        {
                            EventManager.Instance?.TriggerEvent("QuestAvailable", new QuestEventArgs
                            {
                                questId = questData.questId,
                                questName = questData.questName,
                                questType = questData.questType
                            });

                            Debug.Log($"New quest available: {questData.questName}");
                        }
                    }
                }
            }
        }

        #endregion

        #region Save/Load

        public void SaveQuestData()
        {
            // TODO: 实现保存逻辑
            Debug.Log("Quest data saved");
        }

        public void LoadQuestData()
        {
            // TODO: 实现加载逻辑
            Debug.Log("Quest data loaded");
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
    }

    [System.Serializable]
    public class QuestRewardEventArgs
    {
        public string questId;
        public int experience;
        public int gold;
    }
}
