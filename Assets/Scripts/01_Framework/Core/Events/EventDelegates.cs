using UnityEngine;

namespace Framework.Events
{
    public static class GameEvents
    {
        public const string PLAYER_DIED = "PlayerDied";
        public const string PLAYER_LEVEL_UP = "PlayerLevelUp";
        public const string PLAYER_JUMPED = "PlayerJumped";
        public const string PLAYER_ATTACKED = "PlayerAttacked";
        public const string ENEMY_DIED = "EnemyDied";
        public const string ITEM_PICKED_UP = "ItemPickedUp";
        public const string SKILL_USED = "SkillUsed";
        public const string QUEST_COMPLETED = "QuestCompleted";
        public const string ACHIEVEMENT_UNLOCKED = "AchievementUnlocked";
        public const string GAME_PAUSED = "GamePaused";
        public const string GAME_RESUMED = "GameResumed";
        public const string SAVE_GAME = "SaveGame";
        public const string LOAD_GAME = "LoadGame";
        public const string HEALTH_CHANGED = "HealthChanged";
        public const string INVENTORY_CHANGED = "InventoryChanged";
    }

    public class PlayerDiedEvent { }
    public class PlayerLevelUpEvent { public int newLevel; }
    public class PlayerJumpedEvent { }
    public class PlayerAttackedEvent { public int damage; }
    public class EnemyDiedEvent { public string enemyId; public Vector3 position; }
    public class ItemPickedUpEvent { public string itemId; public int amount; }
    public class SkillUsedEvent { public string skillId; public Vector3 targetPosition; }
    public class QuestCompletedEvent { public string questId; }
    public class AchievementUnlockedEvent { public string achievementId; }
    public class HealthChangedEvent { public int currentHealth; public int maxHealth; }
    public class InventoryChangedEvent { }
}
