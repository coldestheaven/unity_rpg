using System;
using System.Collections.Generic;

namespace Framework.Interfaces
{
    /// <summary>
    /// DAO pattern interface for read-only typed game-data access.
    ///
    /// All ScriptableObject databases implement this interface so callers depend on
    /// the abstraction rather than the concrete SO type.  This also makes it trivial
    /// to swap in test doubles or alternative storage backends.
    ///
    /// Usage:
    ///   IRepository&lt;ItemData&gt; repo = GameDataService.Instance.Items;
    ///   ItemData sword   = repo.GetById("item_sword");
    ///   bool     found   = repo.TryGetById("item_sword", out var data);
    ///   var      weapons = repo.Query(i => i.itemType == ItemType.Weapon);
    /// </summary>
    public interface IRepository<T>
    {
        // ── Basic access ──────────────────────────────────────────────────────

        /// <summary>Returns the item with the given id, or <c>null</c> if not found.</summary>
        T GetById(string id);

        /// <summary>
        /// Attempts to retrieve the item with the given id.
        /// Returns <c>false</c> and sets <paramref name="value"/> to <c>default</c> if not found.
        /// Preferred over <see cref="GetById"/> when absence is a normal case.
        /// </summary>
        bool TryGetById(string id, out T value);

        /// <summary>Returns <c>true</c> if an item with the given id exists.</summary>
        bool Exists(string id);

        // ── Bulk / query ──────────────────────────────────────────────────────

        /// <summary>Returns a read-only view of all items in the repository.</summary>
        IReadOnlyList<T> GetAll();

        /// <summary>
        /// Returns all items that satisfy <paramref name="predicate"/>.
        /// Allocates a new list on each call — cache the result if called frequently.
        /// </summary>
        IReadOnlyList<T> Query(Func<T, bool> predicate);

        /// <summary>Total number of items stored.</summary>
        int Count { get; }
    }
}
