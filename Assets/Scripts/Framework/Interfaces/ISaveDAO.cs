using System.Collections.Generic;

namespace Framework.Interfaces
{
    /// <summary>
    /// DAO (Data Access Object) interface for runtime save-game storage.
    ///
    /// Separates the business logic of "what to save" from the mechanics of "how to
    /// store it", allowing the implementation to be swapped (file system, PlayerPrefs,
    /// cloud, in-memory for tests) without changing any business code.
    ///
    /// Storage model: each save is a named <em>slot</em>; within a slot, data is
    /// partitioned by a string <em>key</em> (e.g. "progress", "stats", "position").
    ///
    /// Usage:
    ///   dao.Write("QuickSave", SaveKeys.Progress, new PlayerProgressDTO { ... });
    ///   if (dao.TryRead("QuickSave", SaveKeys.Progress, out PlayerProgressDTO dto)) { ... }
    /// </summary>
    public interface ISaveDAO
    {
        // ── Slot management ───────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if the named slot exists.</summary>
        bool SlotExists(string slotName);

        /// <summary>
        /// Returns metadata for every available save slot, ordered by most-recently
        /// saved first.  Returns an empty list if no slots exist.
        /// </summary>
        IReadOnlyList<SaveSlotInfo> GetAllSlots();

        /// <summary>
        /// Deletes the named slot and all data within it.
        /// No-op if the slot does not exist.
        /// </summary>
        void DeleteSlot(string slotName);

        // ── Typed data access ─────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="data"/> and writes it under
        /// <c>slot/key</c>.  Creates the slot if it does not exist.
        /// </summary>
        void Write<T>(string slotName, string key, T data);

        /// <summary>
        /// Attempts to read and deserialise the data stored at <c>slot/key</c>.
        /// Returns <c>false</c> (and <c>data = default</c>) if the slot or key
        /// does not exist or the data cannot be deserialised.
        /// </summary>
        bool TryRead<T>(string slotName, string key, out T data);

        /// <summary>
        /// Reads and deserialises the data at <c>slot/key</c>.
        /// Returns <c>default(T)</c> if the slot or key does not exist.
        /// </summary>
        T Read<T>(string slotName, string key);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SaveSlotInfo — lightweight metadata for the save-slot list UI
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight metadata record written alongside each save slot.
    /// Enables the save-slot UI to display human-readable information
    /// without loading the full save payload.
    /// </summary>
    [System.Serializable]
    public class SaveSlotInfo
    {
        /// <summary>Save slot name (used as the directory / identifier).</summary>
        public string slotName;

        /// <summary><c>DateTime.UtcNow.Ticks</c> at the time of the save.</summary>
        public long saveTimestampUtc;

        /// <summary>Player level at the time of the save.</summary>
        public int playerLevel;

        /// <summary>Active scene name at the time of the save.</summary>
        public string sceneName;

        /// <summary>Application version string (<c>Application.version</c>).</summary>
        public string gameVersion;
    }
}
