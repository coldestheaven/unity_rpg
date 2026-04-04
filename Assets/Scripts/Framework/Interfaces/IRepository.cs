using System.Collections.Generic;

namespace Framework.Interfaces
{
    /// <summary>
    /// Repository pattern interface for read-only game data access.
    ///
    /// All ScriptableObject databases (ItemDatabase, QuestDatabase, etc.) implement this
    /// interface so callers depend on the abstraction rather than the concrete SO type.
    ///
    /// Usage:
    ///   IRepository&lt;ItemData&gt; repo = dataManager.Items;
    ///   ItemData sword = repo.GetById("item_sword");
    ///   foreach (var item in repo.GetAll()) { ... }
    /// </summary>
    public interface IRepository<T>
    {
        /// <summary>Returns the item with the given id, or null if not found.</summary>
        T GetById(string id);

        /// <summary>Returns true if an item with the given id exists.</summary>
        bool Exists(string id);

        /// <summary>Returns a read-only view of all items in the repository.</summary>
        IReadOnlyList<T> GetAll();

        /// <summary>Total number of items stored.</summary>
        int Count { get; }
    }
}
