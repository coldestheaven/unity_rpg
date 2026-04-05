using UnityEngine;
using System;
using System.Collections.Generic;
using RPG.Core;
using Framework.Events;

namespace RPG.Quests
{
    public class QuestManager : Singleton<QuestManager>
    {
        [Header("任务数据库")]
        [SerializeField] private QuestDatabase questDatabase;

        private Dictionary<string, QuestInstance> activeQuests;
        private Dictionary<string, QuestInstance> completedQuests;
        private Dictionary<string, QuestInstance> failedQuests;

        public int ActiveQuestCount    => activeQuests.Count;
        public int CompletedQuestCount => completedQuests.Count;
        public QuestInstance[] ActiveQuests => new List<QuestInstance>(activeQuests.Values).ToArray();

        public event Action<QuestInstance>                  OnQuestStarted;
        public event Action<QuestInstance>                  OnQuestCompleted;
        public event Action<QuestInstance>                  OnQuestFailed;
        public event Action<QuestInstance, QuestObjective>  OnObjectiveProgressChanged;

        protected override void Awake()
        {
            base.Awake();
            InitializeQuestSystem();
            SubscribeToEvents();
        }

        private void InitializeQuestSystem()
        {
            activeQuests    = new Dictionary<string, QuestInstance>();
            completedQuests = new Dictionary<string, QuestInstance>();
            failedQuests    = new Dictionary<string, QuestInstance>();

            if (questDatabase == null)
                questDatabase = Resources.Load<QuestDatabase>("QuestDatabase");
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        #region Quest Management

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

            QuestInstance questInstance = new QuestInstance(questData);
            if (!questInstance.CanStart())
            {
                Debug.LogWarning($"Cannot start quest: {questId}");
                return false;
            }

            questInstance.StartQuest();
            activeQuests[questId] = questInstance;

            OnQuestStarted?.Invoke(questInstance);
            EventBus.Publish(new QuestStartedEvent(questId, questData.questName));

            Debug.Log($"Quest started: {questData.questName}");
            return true;
        }

        public bool CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out QuestInstance questInstance))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            questInstance.CompleteQuest();
            activeQuests.Remove(questId);
            completedQuests[questId] = questInstance;

            GiveQuestRewards(questInstance.QuestData);
            OnQuestCompleted?.Invoke(questInstance);
            CheckForFollowUpQuests(questId);
            return true;
        }

        public bool AbandonQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out QuestInstance questInstance))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            questInstance.AbandonQuest();
            activeQuests.Remove(questId);
            Debug.Log($"Quest abandoned: {questId}");
            return true;
        }

        public bool FailQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out QuestInstance questInstance))
            {
                Debug.LogWarning($"Quest {questId} is not active");
                return false;
            }

            questInstance.FailQuest();
            activeQuests.Remove(questId);
            failedQuests[questId] = questInstance;

            OnQuestFailed?.Invoke(questInstance);
            return true;
        }

        #endregion

        #region Quest Progress

        public void UpdateObjectiveProgress(string questId, string objectiveId, int amount = 1)
        {
            if (activeQuests.TryGetValue(questId, out QuestInstance questInstance))
            {
                questInstance.UpdateObjectiveProgress(objectiveId, amount);
                OnObjectiveProgressChanged?.Invoke(questInstance, questInstance.QuestData.GetObjectiveById(objectiveId));
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            CheckQuestObjectives(QuestObjectiveType.KillEnemy, e.EnemyId);
        }

        private void OnItemPickedUp(ItemPickedUpEvent e)
        {
            CheckQuestObjectives(QuestObjectiveType.CollectItem, e.ItemName);
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
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
            if (questData.experienceReward > 0)
                PlayerProgressManager.Instance?.AddExperience(questData.experienceReward);

            if (questData.goldReward > 0)
                PlayerProgressManager.Instance?.AddGold(questData.goldReward);

            if (questData.itemRewards != null)
            {
                foreach (var reward in questData.itemRewards)
                {
                    if (reward.item != null && reward.quantity > 0)
                        RPG.Items.ItemSystem.Instance?.AddItem(reward.item, reward.quantity);
                }
            }

            EventBus.Publish(new QuestRewardsClaimedEvent(questData.questId, questData.experienceReward, questData.goldReward));

            Debug.Log($"Rewards given for quest: {questData.questName}");
        }

        #endregion

        #region Quest Queries

        public QuestInstance GetQuestInstance(string questId)
        {
            if (activeQuests.TryGetValue(questId, out var q))    return q;
            if (completedQuests.TryGetValue(questId, out var c)) return c;
            if (failedQuests.TryGetValue(questId, out var f))    return f;
            return null;
        }

        public bool IsQuestActive(string questId)    => activeQuests.ContainsKey(questId);
        public bool IsQuestCompleted(string questId) => completedQuests.ContainsKey(questId);

        public QuestData[] GetAvailableQuests()
        {
            var available = new List<QuestData>();
            if (questDatabase != null)
            {
                foreach (var qd in questDatabase.GetAllQuests())
                    if (IsQuestAvailable(qd)) available.Add(qd);
            }
            return available.ToArray();
        }

        private bool IsQuestAvailable(QuestData questData)
        {
            if (activeQuests.ContainsKey(questData.questId) || completedQuests.ContainsKey(questData.questId))
                return false;

            if (questData.prerequisiteQuests != null)
            {
                foreach (var prereq in questData.prerequisiteQuests)
                    if (!completedQuests.ContainsKey(prereq)) return false;
            }

            return true;
        }

        private void CheckForFollowUpQuests(string completedQuestId)
        {
            if (questDatabase == null) return;

            foreach (var qd in questDatabase.GetAllQuests())
            {
                if (qd.prerequisiteQuests == null) continue;
                foreach (var prereq in qd.prerequisiteQuests)
                {
                    if (prereq == completedQuestId)
                    {
                        EventBus.Publish(new QuestAvailableEvent(qd.questId, qd.questName));
                        Debug.Log($"New quest available: {qd.questName}");
                    }
                }
            }
        }

        #endregion

        #region Save/Load

        public void SaveQuestData()   => Debug.Log("Quest data saved");
        public void LoadQuestData()   => Debug.Log("Quest data loaded");

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
    }
}
