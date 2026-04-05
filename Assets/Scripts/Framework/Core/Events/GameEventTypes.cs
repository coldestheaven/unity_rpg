using UnityEngine;

namespace Framework.Events
{
    /// <summary>
    /// Marker interface for all typed game events used with <see cref="EventBus"/>.
    /// </summary>
    public interface IGameEvent { }

    // ──────────────────────────────────────────────────────────────────────────
    // Game State
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class GameStartedEvent : IGameEvent { }

    public sealed class GameEndedEvent : IGameEvent { }

    public sealed class GameVictoryEvent : IGameEvent { }

    public sealed class GamePausedEvent : IGameEvent { }

    public sealed class GameResumedEvent : IGameEvent { }

    public sealed class ReturnToMainMenuEvent : IGameEvent { }

    public sealed class GameStateChangedEvent : IGameEvent
    {
        public string OldState;
        public string NewState;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Save / Load
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class GameSavedEvent : IGameEvent
    {
        public int Slot;
        public string SaveTime;
    }

    public sealed class GameLoadedEvent : IGameEvent
    {
        public int Slot;
        public string SaveTime;
    }

    public sealed class SaveDeletedEvent : IGameEvent
    {
        public int Slot;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Player
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class PlayerDiedEvent : IGameEvent
    {
        public Vector3 Position;
    }

    public sealed class PlayerLevelUpEvent : IGameEvent
    {
        public int OldLevel;
        public int NewLevel;
        public float NewXPToNextLevel;
    }

    public sealed class PlayerXPGainedEvent : IGameEvent
    {
        public float Amount;
        public float CurrentXP;
        public float XPToNextLevel;
    }

    public sealed class PlayerJumpedEvent : IGameEvent { }

    public sealed class PlayerAttackedEvent : IGameEvent
    {
        public int Damage;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Gold / Currency
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class GoldChangedEvent : IGameEvent
    {
        public int CurrentGold;
        public int Delta;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Enemy
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class EnemyDiedEvent : IGameEvent
    {
        public string EnemyId;
        public string EnemyName;
        public Vector3 Position;
        public int XpReward;
        public int GoldReward;
    }

    public sealed class EnemyKilledEvent : IGameEvent
    {
        public string EnemyId;
        public Vector3 Position;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Items
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class ItemPickedUpEvent : IGameEvent
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public Vector3 Position;
    }

    public sealed class ItemPickupFailedEvent : IGameEvent
    {
        public string ItemId;
        public string ItemName;
        public string Reason;
    }

    public sealed class ItemUsedEvent : IGameEvent
    {
        public string ItemId;
        public string ItemName;
    }

    public sealed class ItemEquippedEvent : IGameEvent
    {
        public string ItemId;
        public string SlotName;
        public bool IsEquipped;
    }

    public sealed class InventoryChangedEvent : IGameEvent { }

    public sealed class QuestItemCollectedEvent : IGameEvent
    {
        public string ItemId;
        public string QuestId;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Skills
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class SkillUsedEvent : IGameEvent
    {
        public string SkillName;
        public string SkillId;
        public int SlotIndex;
        public int Level;
        public bool IsUltimate;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Quests
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class QuestStartedEvent : IGameEvent
    {
        public string QuestId;
        public string QuestName;
    }

    public sealed class QuestCompletedEvent : IGameEvent
    {
        public string QuestId;
        public string QuestName;
    }

    public sealed class QuestObjectivesCompletedEvent : IGameEvent
    {
        public string QuestId;
    }

    public sealed class QuestAvailableEvent : IGameEvent
    {
        public string QuestId;
        public string QuestName;
    }

    public sealed class QuestRewardsClaimedEvent : IGameEvent
    {
        public string QuestId;
        public int Experience;
        public int Gold;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Achievements
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class AchievementUnlockedEvent : IGameEvent
    {
        public string AchievementId;
        public string AchievementName;
    }

    public sealed class AchievementRewardsClaimedEvent : IGameEvent
    {
        public string AchievementId;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combat / Health
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class HealthChangedEvent : IGameEvent
    {
        public float CurrentHealth;
        public float MaxHealth;
        public GameObject Entity;
    }
}
