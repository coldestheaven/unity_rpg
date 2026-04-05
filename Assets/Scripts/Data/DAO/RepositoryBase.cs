using System;
using System.Collections.Generic;
using Framework.Interfaces;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// Abstract ScriptableObject base class for all game-data repositories.
    ///
    /// Eliminates the duplicate dictionary + IRepository boilerplate that previously
    /// existed in every database SO (ItemDatabase, SkillDatabase, …).
    ///
    /// Subclass responsibilities:
    ///   1. Declare a serialised entry array (e.g. <c>[SerializeField] SkillEntry[] _skills</c>).
    ///   2. Implement <see cref="PopulateDictionary"/> to iterate the array and write
    ///      valid (id, data) pairs into the provided dictionary.  Log and skip invalid entries.
    ///
    /// The base class provides:
    ///   • Lazy-initialised dictionary via <see cref="EnsureReady"/>.
    ///   • Full <see cref="IRepository{T}"/> implementation including <c>Query</c>.
    ///   • Reset on <c>OnEnable</c> so hot-reloads rebuild the dictionary.
    ///
    /// Example subclass:
    /// <code>
    ///   [CreateAssetMenu(menuName = "RPG/Data/Skill Database")]
    ///   public class SkillDatabase : RepositoryBase&lt;SkillData&gt;
    ///   {
    ///       [Serializable] public class SkillEntry { public string skillId; public SkillData skillData; }
    ///       [SerializeField] private SkillEntry[] _skills = Array.Empty&lt;SkillEntry&gt;();
    ///
    ///       protected override void PopulateDictionary(Dictionary&lt;string, SkillData&gt; dict)
    ///       {
    ///           foreach (var e in _skills)
    ///               if (e?.skillData != null &amp;&amp; !string.IsNullOrEmpty(e.skillId))
    ///                   dict[e.skillId] = e.skillData;
    ///       }
    ///   }
    /// </code>
    /// </summary>
    public abstract class RepositoryBase<T> : ScriptableObject, IRepository<T>
        where T : class
    {
        private Dictionary<string, T> _dict;

        // ── Subclass contract ─────────────────────────────────────────────────

        /// <summary>
        /// Populate <paramref name="dict"/> from the serialised entry array.
        /// Skip null entries or entries with missing IDs / data objects silently or
        /// with a per-entry <c>Debug.LogWarning</c>.
        /// </summary>
        protected abstract void PopulateDictionary(Dictionary<string, T> dict);

        // ── IRepository<T> ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public T GetById(string id)
        {
            EnsureReady();
            return _dict.TryGetValue(id ?? string.Empty, out var v) ? v : null;
        }

        /// <inheritdoc/>
        public bool TryGetById(string id, out T value)
        {
            EnsureReady();
            return _dict.TryGetValue(id ?? string.Empty, out value);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            EnsureReady();
            return _dict.ContainsKey(id ?? string.Empty);
        }

        /// <inheritdoc/>
        public IReadOnlyList<T> GetAll()
        {
            EnsureReady();
            return new List<T>(_dict.Values);
        }

        /// <inheritdoc/>
        public IReadOnlyList<T> Query(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            EnsureReady();
            var result = new List<T>();
            foreach (var v in _dict.Values)
                if (predicate(v)) result.Add(v);
            return result;
        }

        /// <inheritdoc/>
        public int Count
        {
            get { EnsureReady(); return _dict.Count; }
        }

        // ── Explicit initialisation (called by GameDataService) ───────────────

        /// <summary>
        /// Builds (or rebuilds) the internal dictionary from the serialised entries.
        /// Called automatically on first access; also called by
        /// <see cref="RPG.Data.GameDataService.InitializeAll"/> to control timing.
        /// </summary>
        public void Initialize()
        {
            _dict = new Dictionary<string, T>();
            PopulateDictionary(_dict);
            Debug.Log($"[{GetType().Name}] Initialized with {_dict.Count} entries.");
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        // Clear the cache whenever Unity reloads the SO (editor hot-reload, domain reload).
        private void OnEnable() => _dict = null;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Lazily initialises the dictionary on first access.</summary>
        protected void EnsureReady()
        {
            if (_dict == null) Initialize();
        }
    }
}
