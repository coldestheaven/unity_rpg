using UnityEngine;

namespace Framework.Events
{
    /// <summary>
    /// 所有游戏事件实现此接口。
    /// EventId 返回常量，EventBus 用它做数组索引，无虚调用开销。
    /// </summary>
    public interface IGameEvent
    {
        GameEventId EventId { get; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Game State
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct GameStartedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameStarted;
    }

    public readonly struct GameEndedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameEnded;
    }

    public readonly struct GameVictoryEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameVictory;
    }

    public readonly struct GamePausedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GamePaused;
    }

    public readonly struct GameResumedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameResumed;
    }

    public readonly struct ReturnToMainMenuEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ReturnToMainMenu;
    }

    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameStateChanged;
        public readonly string OldState;
        public readonly string NewState;
        public GameStateChangedEvent(string old, string next) { OldState = old; NewState = next; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Save / Load
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct GameSavedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameSaved;
        public readonly int    Slot;
        public readonly string SaveTime;
        public GameSavedEvent(int slot, string time) { Slot = slot; SaveTime = time; }
    }

    public readonly struct GameLoadedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GameLoaded;
        public readonly int Slot;
        public GameLoadedEvent(int slot) { Slot = slot; }
    }

    public readonly struct SaveDeletedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.SaveDeleted;
        public readonly int Slot;
        public SaveDeletedEvent(int slot) { Slot = slot; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Player
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct PlayerDiedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerDied;
        public readonly Vector3 Position;
        public PlayerDiedEvent(Vector3 pos) { Position = pos; }
    }

    public readonly struct PlayerLevelUpEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerLevelUp;
        public readonly int   OldLevel;
        public readonly int   NewLevel;
        public readonly float NewXPToNextLevel;
        public PlayerLevelUpEvent(int old, int next, float xpToNext)
        {
            OldLevel = old; NewLevel = next; NewXPToNextLevel = xpToNext;
        }
    }

    public readonly struct PlayerXPGainedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerXPGained;
        public readonly float Amount;
        public readonly float CurrentXP;
        public readonly float XPToNextLevel;
        public PlayerXPGainedEvent(float amount, float current, float toNext)
        {
            Amount = amount; CurrentXP = current; XPToNextLevel = toNext;
        }
    }

    public readonly struct PlayerJumpedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerJumped;
    }

    public readonly struct PlayerAttackedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerAttacked;
        public readonly int Damage;
        public PlayerAttackedEvent(int dmg) { Damage = dmg; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Currency
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct GoldChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.GoldChanged;
        public readonly int CurrentGold;
        public readonly int Delta;
        public GoldChangedEvent(int current, int delta) { CurrentGold = current; Delta = delta; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Enemy
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct EnemyDiedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.EnemyDied;
        public readonly string  EnemyId;
        public readonly string  EnemyName;
        public readonly Vector3 Position;
        public readonly int     XpReward;
        public readonly int     GoldReward;
        public EnemyDiedEvent(string id, string name, Vector3 pos, int xp, int gold)
        {
            EnemyId = id; EnemyName = name; Position = pos; XpReward = xp; GoldReward = gold;
        }
    }

    public readonly struct EnemyKilledEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.EnemyKilled;
        public readonly string  EnemyId;
        public readonly Vector3 Position;
        public EnemyKilledEvent(string id, Vector3 pos) { EnemyId = id; Position = pos; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Items
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct ItemPickedUpEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemPickedUp;
        public readonly string  ItemId;
        public readonly string  ItemName;
        public readonly int     Quantity;
        public readonly Vector3 Position;
        public ItemPickedUpEvent(string id, string name, int qty, Vector3 pos)
        {
            ItemId = id; ItemName = name; Quantity = qty; Position = pos;
        }
    }

    public readonly struct ItemPickupFailedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemPickupFailed;
        public readonly string ItemId;
        public readonly string ItemName;
        public readonly string Reason;
        public ItemPickupFailedEvent(string id, string name, string reason)
        {
            ItemId = id; ItemName = name; Reason = reason;
        }
    }

    public readonly struct ItemUsedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemUsed;
        public readonly string ItemId;
        public readonly string ItemName;
        public ItemUsedEvent(string id, string name) { ItemId = id; ItemName = name; }
    }

    public readonly struct ItemEquippedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemEquipped;
        public readonly string ItemId;
        public readonly string SlotName;
        public readonly bool   IsEquipped;
        public ItemEquippedEvent(string id, string slot, bool equipped)
        {
            ItemId = id; SlotName = slot; IsEquipped = equipped;
        }
    }

    public readonly struct ItemDroppedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemDropped;
        public readonly string  ItemId;
        public readonly string  ItemName;
        public readonly int     Quantity;
        public readonly Vector3 Position;
        public ItemDroppedEvent(string id, string name, int qty, Vector3 pos)
        {
            ItemId = id; ItemName = name; Quantity = qty; Position = pos;
        }
    }

    public readonly struct ItemBoughtEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemBought;
        public readonly string ItemId;
        public readonly string ItemName;
        public readonly int    Quantity;
        public readonly int    TotalCost;
        public ItemBoughtEvent(string id, string name, int qty, int cost)
        {
            ItemId = id; ItemName = name; Quantity = qty; TotalCost = cost;
        }
    }

    public readonly struct ItemSoldEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.ItemSold;
        public readonly string ItemId;
        public readonly string ItemName;
        public readonly int    Quantity;
        public readonly int    TotalGold;
        public ItemSoldEvent(string id, string name, int qty, int gold)
        {
            ItemId = id; ItemName = name; Quantity = qty; TotalGold = gold;
        }
    }

    public readonly struct InventoryChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.InventoryChanged;
    }

    public readonly struct QuestItemCollectedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestItemCollected;
        public readonly string ItemId;
        public readonly string QuestId;
        public QuestItemCollectedEvent(string item, string quest) { ItemId = item; QuestId = quest; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Skills
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct SkillUsedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.SkillUsed;
        public readonly string SkillId;
        public readonly string SkillName;
        public readonly int    SlotIndex;
        public readonly int    Level;
        public readonly bool   IsUltimate;
        public SkillUsedEvent(string id, string name, int slot, int level, bool ultimate = false)
        {
            SkillId = id; SkillName = name; SlotIndex = slot; Level = level; IsUltimate = ultimate;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Quests
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct QuestStartedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestStarted;
        public readonly string QuestId;
        public readonly string QuestName;
        public QuestStartedEvent(string id, string name) { QuestId = id; QuestName = name; }
    }

    public readonly struct QuestCompletedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestCompleted;
        public readonly string QuestId;
        public readonly string QuestName;
        public QuestCompletedEvent(string id, string name) { QuestId = id; QuestName = name; }
    }

    public readonly struct QuestObjectivesCompletedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestObjectivesCompleted;
        public readonly string QuestId;
        public QuestObjectivesCompletedEvent(string id) { QuestId = id; }
    }

    public readonly struct QuestAvailableEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestAvailable;
        public readonly string QuestId;
        public readonly string QuestName;
        public QuestAvailableEvent(string id, string name) { QuestId = id; QuestName = name; }
    }

    public readonly struct QuestRewardsClaimedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.QuestRewardsClaimed;
        public readonly string QuestId;
        public readonly int    Experience;
        public readonly int    Gold;
        public QuestRewardsClaimedEvent(string id, int xp, int gold)
        {
            QuestId = id; Experience = xp; Gold = gold;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Achievements
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct AchievementUnlockedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.AchievementUnlocked;
        public readonly string AchievementId;
        public readonly string AchievementName;
        public AchievementUnlockedEvent(string id, string name)
        {
            AchievementId = id; AchievementName = name;
        }
    }

    public readonly struct AchievementRewardsClaimedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.AchievementRewardsClaimed;
        public readonly string AchievementId;
        public AchievementRewardsClaimedEvent(string id) { AchievementId = id; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combat / Health
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct HealthChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.HealthChanged;
        public readonly float      CurrentHealth;
        public readonly float      MaxHealth;
        public readonly GameObject Entity;
        public HealthChangedEvent(float current, float max, GameObject entity)
        {
            CurrentHealth = current; MaxHealth = max; Entity = entity;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Player Vitals (UI-facing, raised on main thread)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>玩家当前/最大 HP 发生变化时发布（主线程）。</summary>
    public readonly struct PlayerHealthChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerHealthChanged;
        public readonly float CurrentHealth;
        public readonly float MaxHealth;
        public PlayerHealthChangedEvent(float current, float max)
        {
            CurrentHealth = current; MaxHealth = max;
        }
    }

    /// <summary>玩家当前/最大 MP 发生变化时发布（主线程）。</summary>
    public readonly struct PlayerManaChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerManaChanged;
        public readonly float CurrentMana;
        public readonly float MaxMana;
        public PlayerManaChangedEvent(float current, float max)
        {
            CurrentMana = current; MaxMana = max;
        }
    }

    /// <summary>某技能槽冷却剩余秒数变化时发布（主线程）。</summary>
    public readonly struct PlayerSkillCooldownChangedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PlayerSkillCooldownChanged;
        /// <summary>技能槽索引（0-based）。</summary>
        public readonly int   SlotIndex;
        /// <summary>剩余冷却时间（秒）；0 表示已就绪。</summary>
        public readonly float RemainingSeconds;
        /// <summary>总冷却时间（秒），用于计算进度百分比。</summary>
        public readonly float TotalCooldown;
        public PlayerSkillCooldownChangedEvent(int slot, float remaining, float total)
        {
            SlotIndex = slot; RemainingSeconds = remaining; TotalCooldown = total;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scene
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct SceneLoadStartedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.SceneLoadStarted;
        public readonly string SceneName;
        public readonly string TransitionType;
        public SceneLoadStartedEvent(string scene, string transition = "fade")
        {
            SceneName = scene; TransitionType = transition;
        }
    }

    public readonly struct SceneLoadCompletedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.SceneLoadCompleted;
        public readonly string SceneName;
        public SceneLoadCompletedEvent(string scene) { SceneName = scene; }
    }

    public readonly struct SceneUnloadedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.SceneUnloaded;
        public readonly string SceneName;
        public SceneUnloadedEvent(string scene) { SceneName = scene; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pickup
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct PickupSpawnedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PickupSpawned;
        public readonly string ItemId;
        public readonly int    Quantity;
        public readonly UnityEngine.Vector3 Position;
        public PickupSpawnedEvent(string id, int qty, UnityEngine.Vector3 pos)
        {
            ItemId = id; Quantity = qty; Position = pos;
        }
    }

    public readonly struct PickupCollectedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.PickupCollected;
        public readonly string ItemId;
        public readonly string ItemName;
        public readonly int    Quantity;
        public PickupCollectedEvent(string id, string name, int qty)
        {
            ItemId = id; ItemName = name; Quantity = qty;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Buildings
    // ──────────────────────────────────────────────────────────────────────────

    public readonly struct BuildingPlacedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.BuildingPlaced;
        public readonly string BuildingId;
        public readonly string InstanceId;
        public readonly UnityEngine.Vector3 Position;
        public BuildingPlacedEvent(string buildingId, string instanceId, UnityEngine.Vector3 pos)
        {
            BuildingId = buildingId; InstanceId = instanceId; Position = pos;
        }
    }

    public readonly struct BuildingDemolishedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.BuildingDemolished;
        public readonly string BuildingId;
        public readonly string InstanceId;
        public BuildingDemolishedEvent(string buildingId, string instanceId)
        {
            BuildingId = buildingId; InstanceId = instanceId;
        }
    }

    public readonly struct BuildingPlacementStartedEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.BuildingPlacementStarted;
        public readonly string BuildingId;
        public BuildingPlacementStartedEvent(string id) { BuildingId = id; }
    }

    public readonly struct BuildingPlacementCancelledEvent : IGameEvent
    {
        public GameEventId EventId => GameEventId.BuildingPlacementCancelled;
    }
}
