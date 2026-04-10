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
        public const string Meta      = "meta";
        public const string Progress  = "progress";
        public const string Stats     = "stats";
        public const string Position  = "position";
        public const string Settings  = "settings";
        public const string Inventory = "inventory";
        public const string Equipment = "equipment";
        public const string Buildings = "buildings";
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

    // ── Inventory ─────────────────────────────────────────────────────────────

    /// <summary>Full inventory snapshot: gold balance + per-slot item/quantity.</summary>
    [Serializable]
    public class InventoryDTO
    {
        public int                  gold  = 0;
        public InventorySlotDTO[]   slots = Array.Empty<InventorySlotDTO>();
    }

    [Serializable]
    public class InventorySlotDTO
    {
        public string itemId   = "";
        public int    quantity = 0;
    }

    // ── Equipment ─────────────────────────────────────────────────────────────

    /// <summary>All equipped items keyed by slot name.</summary>
    [Serializable]
    public class EquipmentDTO
    {
        public EquipmentSlotDTO[] slots = Array.Empty<EquipmentSlotDTO>();
    }

    [Serializable]
    public class EquipmentSlotDTO
    {
        public string slotName = "";
        public string itemId   = "";
    }

    // ── Buildings ─────────────────────────────────────────────────────────────

    /// <summary>All placed buildings in the current scene.</summary>
    [Serializable]
    public class BuildingsSaveDTO
    {
        public PlacedBuildingDTO[] buildings = Array.Empty<PlacedBuildingDTO>();
    }

    [Serializable]
    public class PlacedBuildingDTO
    {
        public string instanceId   = "";
        public string buildingId   = "";
        public float  posX, posY, posZ;
        public float  rotY;
        public float  currentHealth;
    }
}
