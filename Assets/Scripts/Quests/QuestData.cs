using UnityEngine;
using System;
using Framework.Events;

namespace RPG.Quests
{
    /// <summary>
    /// 任务状态
    /// </summary>
    public enum QuestState
    {
        NotStarted,     // 未开始
        Available,       // 可接取
        InProgress,     // 进行中
        Completed,      // 已完成
        Failed,         // 失败
        Abandoned       // 已放弃
    }

    /// <summary>
    /// 任务类型
    /// </summary>
    public enum QuestType
    {
        Main,           // 主线任务
        Side,           // 支线任务
        Daily,          // 每日任务
        Weekly,         // 每周任务
        Repeatable,     // 可重复任务
        Event           // 活动任务
    }

    /// <summary>
    /// 任务目标类型
    /// </summary>
    public enum QuestObjectiveType
    {
        KillEnemy,      // 击杀敌人
        CollectItem,    // 收集物品
        TalkToNPC,      // 对话
        ReachLocation,  // 到达地点
        DefeatBoss,     // 击败Boss
        EscortNPC,      // 护送NPC
        Survive,        // 生存
        CraftItem,      // 制作物品
        UseItem         // 使用物品
    }

    /// <summary>
    /// 任务数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "RPG/Quests/Quest")]
    public class QuestData : ScriptableObject
    {
        [Header("基本信息")]
        public string questId;
        public string questName;
        [TextArea]
        public string description;
        public Sprite icon;
        public QuestType questType;

        [Header("任务目标")]
        public QuestObjective[] objectives;

        [Header("奖励")]
        public int experienceReward;
        public int goldReward;
        public QuestReward[] itemRewards;

        [Header("要求")]
        public int requiredLevel = 1;
        public string[] prerequisiteQuests;
        public QuestData[] prerequisiteQuestData;

        [Header("时间限制")]
        public bool hasTimeLimit = false;
        public int timeLimitMinutes = 0;

        [Header("可重复性")]
        public bool isRepeatable = false;
        public int maxRepeatCount = 1;
        public int repeatCooldownHours = 24;

        public QuestObjective GetObjectiveById(string objectiveId)
        {
            foreach (var objective in objectives)
            {
                if (objective.objectiveId == objectiveId)
                {
                    return objective;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 任务目标
    /// </summary>
    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public QuestObjectiveType objectiveType;

        [Header("目标参数")]
        public int targetAmount = 1;
        public string targetId;           // 目标ID(敌人ID、物品ID等)
        public string targetName;
        public string locationName;

        [Header("条件")]
        public bool optional = false;
        public int order = 0;             // 执行顺序

        public int CurrentProgress { get; set; }
        public bool IsCompleted => CurrentProgress >= targetAmount;
        public float ProgressPercentage => targetAmount > 0 ? (float)CurrentProgress / targetAmount : 0f;

        public void AddProgress(int amount = 1)
        {
            CurrentProgress = Mathf.Min(CurrentProgress + amount, targetAmount);
        }

        public void ResetProgress()
        {
            CurrentProgress = 0;
        }
    }

    /// <summary>
    /// 任务奖励
    /// </summary>
    [System.Serializable]
    public class QuestReward
    {
        public RPG.Items.ItemData item;
        public int quantity = 1;
    }

    /// <summary>
    /// 任务实例 - 运行时任务状态
    /// </summary>
    public class QuestInstance
    {
        public QuestData QuestData { get; private set; }
        public QuestState State { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime? CompleteTime { get; private set; }
        public int RepeatCount { get; private set; }
        public DateTime? LastCompleteTime { get; private set; }

        public event Action<QuestState> OnStateChanged;
        public event Action<QuestObjective> OnObjectiveProgressChanged;
        public event Action OnQuestCompleted;

        public QuestInstance(QuestData questData)
        {
            QuestData = questData;
            State = QuestState.NotStarted;
            StartTime = DateTime.Now;
            ResetObjectives();
        }

        public bool CanStart()
        {
            if (State != QuestState.NotStarted && State != QuestState.Available)
            {
                return false;
            }

            // 检查等级要求
            // TODO: 从玩家系统获取当前等级

            // 检查重复冷却
            if (LastCompleteTime.HasValue && QuestData.isRepeatable)
            {
                TimeSpan cooldown = TimeSpan.FromHours(QuestData.repeatCooldownHours);
                if (DateTime.Now - LastCompleteTime.Value < cooldown)
                {
                    return false;
                }
            }

            return true;
        }

        public void StartQuest()
        {
            if (!CanStart()) return;

            State = QuestState.InProgress;
            StartTime = DateTime.Now;
            ResetObjectives();

            OnStateChanged?.Invoke(State);
            Debug.Log($"Quest started: {QuestData.questName}");
        }

        public void CompleteQuest()
        {
            if (State != QuestState.InProgress) return;

            // 检查所有必需目标是否完成
            bool allRequiredCompleted = true;
            foreach (var objective in QuestData.objectives)
            {
                if (!objective.optional && !objective.IsCompleted)
                {
                    allRequiredCompleted = false;
                    break;
                }
            }

            if (!allRequiredCompleted)
            {
                Debug.LogWarning("Cannot complete quest: Not all objectives completed");
                return;
            }

            State = QuestState.Completed;
            CompleteTime = DateTime.Now;
            RepeatCount++;
            LastCompleteTime = DateTime.Now;

            OnStateChanged?.Invoke(State);
            OnQuestCompleted?.Invoke();

            Framework.Events.EventBus.Publish(new Framework.Events.QuestCompletedEvent
            {
                QuestId   = QuestData.questId,
                QuestName = QuestData.questName
            });

            Debug.Log($"Quest completed: {QuestData.questName}");
        }

        public void FailQuest()
        {
            if (State != QuestState.InProgress) return;

            State = QuestState.Failed;
            CompleteTime = DateTime.Now;

            OnStateChanged?.Invoke(State);
            Debug.Log($"Quest failed: {QuestData.questName}");
        }

        public void AbandonQuest()
        {
            if (State != QuestState.InProgress) return;

            State = QuestState.Abandoned;

            OnStateChanged?.Invoke(State);
            Debug.Log($"Quest abandoned: {QuestData.questName}");
        }

        public bool IsAllObjectivesCompleted()
        {
            foreach (var objective in QuestData.objectives)
            {
                if (!objective.optional && !objective.IsCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        public void UpdateObjectiveProgress(string objectiveId, int amount = 1)
        {
            foreach (var objective in QuestData.objectives)
            {
                if (objective.objectiveId == objectiveId)
                {
                    objective.AddProgress(amount);
                    OnObjectiveProgressChanged?.Invoke(objective);

                    // 检查是否所有目标都完成了
                    if (IsAllObjectivesCompleted())
                    {
                        Framework.Events.EventBus.Publish(new Framework.Events.QuestObjectivesCompletedEvent
                        {
                            QuestId = QuestData.questId
                        });
                    }

                    break;
                }
            }
        }

        private void ResetObjectives()
        {
            foreach (var objective in QuestData.objectives)
            {
                objective.ResetProgress();
            }
        }
    }

}
