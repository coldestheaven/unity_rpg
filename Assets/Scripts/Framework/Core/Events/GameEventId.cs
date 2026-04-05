namespace Framework.Events
{
    /// <summary>
    /// 所有游戏事件的枚举 ID。
    /// EventBus 以此为数组下标，O(1) 分发，无字符串比较，无 GC。
    ///
    /// 添加新事件：
    ///   1. 在 _Count 之前新增枚举项。
    ///   2. 在 GameEventTypes.cs 中创建对应的 readonly struct，实现 IGameEvent.EventId。
    /// </summary>
    public enum GameEventId
    {
        // ── Game State ──────────────────────────────
        GameStarted = 0,
        GameEnded,
        GameVictory,
        GamePaused,
        GameResumed,
        ReturnToMainMenu,
        GameStateChanged,

        // ── Save / Load ─────────────────────────────
        GameSaved,
        GameLoaded,
        SaveDeleted,

        // ── Player ──────────────────────────────────
        PlayerDied,
        PlayerLevelUp,
        PlayerXPGained,
        PlayerJumped,
        PlayerAttacked,

        // ── Currency ────────────────────────────────
        GoldChanged,

        // ── Enemy ───────────────────────────────────
        EnemyDied,
        EnemyKilled,

        // ── Items ───────────────────────────────────
        ItemPickedUp,
        ItemPickupFailed,
        ItemUsed,
        ItemEquipped,
        InventoryChanged,
        QuestItemCollected,

        // ── Skills ──────────────────────────────────
        SkillUsed,

        // ── Quests ──────────────────────────────────
        QuestStarted,
        QuestCompleted,
        QuestObjectivesCompleted,
        QuestAvailable,
        QuestRewardsClaimed,

        // ── Achievements ────────────────────────────
        AchievementUnlocked,
        AchievementRewardsClaimed,

        // ── Combat / Health ─────────────────────────
        HealthChanged,

        /// <summary>哨兵值，用于 EventBus 内部数组定长，请勿使用。</summary>
        _Count
    }
}
