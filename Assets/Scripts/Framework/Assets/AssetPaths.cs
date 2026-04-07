namespace Framework.Assets
{
    /// <summary>
    /// 所有资源路径常量的集中定义。
    ///
    /// 规则：
    ///   • 路径相对于 Assets/Resources/（Resources 后端）
    ///     或 Addressables 中配置的地址（Addressables 后端）。
    ///   • 按资产类别分嵌套静态类，便于 IDE 自动补全，避免拼写错误。
    ///   • 新增资产时在此处添加常量，不要在业务代码中硬编码字符串。
    /// </summary>
    public static class AssetPaths
    {
        // ── 配置数据（ScriptableObject）────────────────────────────────────────
        public static class Data
        {
            /// <summary>主数据服务资产（GameDataService.asset）。</summary>
            public const string GameDataService    = "GameData/GameDataService";

            /// <summary>任务数据库资产（QuestDatabase.asset）。</summary>
            public const string QuestDatabase      = "QuestDatabase";

            /// <summary>物品数据库资产（ItemDatabase.asset）。</summary>
            public const string ItemDatabase       = "ItemDatabase";

            /// <summary>成就数据库资产（AchievementDatabase.asset）。</summary>
            public const string AchievementDatabase = "AchievementDatabase";

            /// <summary>技能数据库资产（SkillDatabase.asset）。</summary>
            public const string SkillDatabase      = "SkillDatabase";

            /// <summary>敌人数据库资产（EnemyDatabase.asset）。</summary>
            public const string EnemyDatabase      = "EnemyDatabase";

            /// <summary>Buff 数据库资产（BuffDatabase.asset）。</summary>
            public const string BuffDatabase       = "BuffDatabase";
        }

        // ── 预制件 ───────────────────────────────────────────────────────────
        public static class Prefabs
        {
            public const string Player      = "Prefabs/Player";
            public const string ItemPickup  = "Prefabs/ItemPickup";
            public const string DamagePopup = "Prefabs/DamagePopup";
        }

        // ── 音频 ─────────────────────────────────────────────────────────────
        public static class Audio
        {
            public const string BgmMain     = "Audio/BGM/Main";
            public const string BgmCombat   = "Audio/BGM/Combat";
            public const string SfxHit      = "Audio/SFX/Hit";
            public const string SfxPickup   = "Audio/SFX/Pickup";
        }

        // ── UI ───────────────────────────────────────────────────────────────
        public static class UI
        {
            public const string HudPanel       = "UI/HUDPanel";
            public const string InventoryPanel = "UI/InventoryPanel";
            public const string GameOverPanel  = "UI/GameOverPanel";
        }
    }
}
