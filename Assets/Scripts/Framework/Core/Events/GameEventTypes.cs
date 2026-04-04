using UnityEngine;

namespace Framework.Events
{
    /// <summary>
    /// Marker interface for all typed game events used with <see cref="EventBus"/>.
    /// Implement this interface on any event payload class or struct.
    /// </summary>
    public interface IGameEvent { }

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
        public string EnemyName;
        public Vector3 Position;
        public int XpReward;
        public int GoldReward;
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

    // ──────────────────────────────────────────────────────────────────────────
    // Skills
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class SkillUsedEvent : IGameEvent
    {
        public string SkillName;
        public int SlotIndex;
        public int Level;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Quests / Achievements
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class QuestCompletedEvent : IGameEvent
    {
        public string QuestId;
    }

    public sealed class AchievementUnlockedEvent : IGameEvent
    {
        public string AchievementId;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Game state
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class GameStateChangedEvent : IGameEvent
    {
        public string OldState;
        public string NewState;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combat
    // ──────────────────────────────────────────────────────────────────────────

    public sealed class HealthChangedEvent : IGameEvent
    {
        public float CurrentHealth;
        public float MaxHealth;
        public GameObject Entity;
    }
}
