using System;

namespace RPG.Data
{
    // ──────────────────────────────────────────────────────────────────────────
    // Save DTO (Data Transfer Objects)
    //
    // Each DTO is a plain, serialisable data bag covering one domain of the game.
    // DTOs replace the previous flat SaveData class, which mixed all domains into
    // a single struct and coupled the save layer directly to runtime types.
    //
    // Design rules:
    //   • Only primitive/value types and serialisable types — no MonoBehaviour refs.
    //   • No game logic — these are pure data containers.
    //   • Written/read by SaveSystem through ISaveDAO using the SaveKeys constants.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>String constants used as DAO keys within a save slot.</summary>
    public static class SaveKeys
    {
        public const string Meta     = "meta";
        public const string Progress = "progress";
        public const string Stats    = "stats";
        public const string Position = "position";
        public const string Settings = "settings";
    }

    // ── Player progress (XP / level / gold) ──────────────────────────────────

    /// <summary>Player experience, level, and currency at save time.</summary>
    [Serializable]
    public class PlayerProgressDTO
    {
        public int   level                 = 1;
        public float experience            = 0f;
        public float experienceToNextLevel = 100f;
        public int   gold                  = 0;
    }

    // ── Player combat stats ───────────────────────────────────────────────────

    /// <summary>Player combat statistics at save time.</summary>
    [Serializable]
    public class PlayerStatsDTO
    {
        public float maxHealth   = 100f;
        public float currentHealth = 100f;
        public float attackPower = 10f;
        public float defense     = 0f;
        public float moveSpeed   = 5f;
    }

    // ── Scene / position ──────────────────────────────────────────────────────

    /// <summary>Scene and world-space position of the player at save time.</summary>
    [Serializable]
    public class PlayerPositionDTO
    {
        public string sceneName = "";
        public float  posX;
        public float  posY;
        public float  posZ;
    }

    // ── Audio / graphics settings ─────────────────────────────────────────────

    /// <summary>Player-configurable settings at save time.</summary>
    [Serializable]
    public class GameSettingsDTO
    {
        public float masterVolume    = 1f;
        public float musicVolume     = 1f;
        public float sfxVolume       = 1f;
        public int   graphicsQuality = 2;
    }
}
