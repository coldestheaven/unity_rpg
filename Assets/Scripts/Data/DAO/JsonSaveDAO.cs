using System;
using System.Collections.Generic;
using System.IO;
using Framework.Interfaces;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// File-system implementation of <see cref="ISaveDAO"/>.
    ///
    /// Storage layout:
    /// <code>
    ///   {rootDir}/
    ///     QuickSave/
    ///       meta.sav          ← SaveSlotInfo (slot metadata)
    ///       progress.sav      ← PlayerProgressDTO
    ///       stats.sav         ← PlayerStatsDTO
    ///       position.sav      ← PlayerPositionDTO
    ///     AutoSave/
    ///       …
    ///     2025-01-01_12-00-00/
    ///       …
    /// </code>
    ///
    /// Each file contains a JSON object serialised by <c>JsonUtility.ToJson</c>.
    /// The <c>.sav</c> extension distinguishes save data from other files that may
    /// appear in the same directory.
    ///
    /// Thread safety: not thread-safe — all calls must originate from the main thread.
    /// </summary>
    public sealed class JsonSaveDAO : ISaveDAO
    {
        private readonly string _rootDir;
        private const string Extension = ".sav";
        private const string MetaKey   = "meta";

        /// <param name="rootDir">
        /// Root directory for all save data (e.g.
        /// <c>Path.Combine(Application.persistentDataPath, "Saves")</c>).
        /// Created automatically if it does not exist.
        /// </param>
        public JsonSaveDAO(string rootDir)
        {
            _rootDir = rootDir;
            Directory.CreateDirectory(rootDir);
        }

        // ── ISaveDAO — slot management ────────────────────────────────────────

        /// <inheritdoc/>
        public bool SlotExists(string slotName)
            => Directory.Exists(SlotPath(slotName));

        /// <inheritdoc/>
        public IReadOnlyList<SaveSlotInfo> GetAllSlots()
        {
            var result = new List<SaveSlotInfo>();
            if (!Directory.Exists(_rootDir)) return result;

            foreach (string dir in Directory.GetDirectories(_rootDir))
            {
                string slotName = Path.GetFileName(dir);
                if (TryRead<SaveSlotInfo>(slotName, MetaKey, out var info) && info != null)
                {
                    info.slotName = slotName;
                    result.Add(info);
                }
                else
                {
                    // Slot exists but has no metadata — create a stub entry.
                    result.Add(new SaveSlotInfo { slotName = slotName });
                }
            }

            // Sort: most-recently saved first.
            result.Sort((a, b) => b.saveTimestampUtc.CompareTo(a.saveTimestampUtc));
            return result;
        }

        /// <inheritdoc/>
        public void DeleteSlot(string slotName)
        {
            string dir = SlotPath(slotName);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }

        // ── ISaveDAO — typed data access ──────────────────────────────────────

        /// <inheritdoc/>
        public void Write<T>(string slotName, string key, T data)
        {
            try
            {
                string dir = SlotPath(slotName);
                Directory.CreateDirectory(dir);
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath(slotName, key), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSaveDAO] Write failed ({slotName}/{key}): {e.Message}");
            }
        }

        /// <inheritdoc/>
        public bool TryRead<T>(string slotName, string key, out T data)
        {
            string path = FilePath(slotName, key);
            if (!File.Exists(path)) { data = default; return false; }

            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonSaveDAO] Read failed ({slotName}/{key}): {e.Message}");
                data = default;
                return false;
            }
        }

        /// <inheritdoc/>
        public T Read<T>(string slotName, string key)
        {
            TryRead<T>(slotName, key, out var data);
            return data;
        }

        // ── Path helpers ──────────────────────────────────────────────────────

        private string SlotPath(string slotName) => Path.Combine(_rootDir, slotName);
        private string FilePath(string slotName, string key)
            => Path.Combine(SlotPath(slotName), key + Extension);
    }
}
