using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// Factory Method pattern — centralises all world pickup creation.
    ///
    /// Callers (ItemSystem, LootTable, enemy death handlers) should use this instead
    /// of calling <c>new GameObject</c> / <c>AddComponent&lt;ItemPickup&gt;</c> directly.
    ///
    /// If <see cref="ItemData.worldPickupPrefab"/> is assigned it is instantiated; otherwise
    /// a minimal runtime GameObject is created and an <see cref="ItemPickup"/> component
    /// is attached automatically.
    /// </summary>
    public static class ItemPickupFactory
    {
        /// <summary>
        /// Creates a pickup object at <paramref name="position"/>.
        /// </summary>
        /// <param name="item">Item to place in the world. Null is a no-op.</param>
        /// <param name="quantity">Stack size of the pickup.</param>
        /// <param name="position">World-space spawn position.</param>
        /// <param name="parent">Optional parent transform.</param>
        public static ItemPickup Create(
            ItemData item,
            int quantity,
            Vector3 position,
            Transform parent = null)
        {
            if (item == null) return null;

            GameObject go;

            if (item.worldPickupPrefab != null)
            {
                go = Object.Instantiate(item.worldPickupPrefab, position,
                                        Quaternion.identity, parent);
            }
            else
            {
                go = new GameObject($"Pickup_{item.itemName}");
                go.transform.position = position;
                if (parent != null) go.transform.SetParent(parent);
            }

            ItemPickup pickup = go.GetComponent<ItemPickup>()
                             ?? go.AddComponent<ItemPickup>();

            pickup.SetItem(item, quantity);
            return pickup;
        }

        /// <summary>
        /// Convenience overload accepting a <see cref="LootTable"/> roll result.
        /// Rolls the table and creates a pickup at the given position, or returns null
        /// if the table rolls no drop.
        /// </summary>
        public static ItemPickup CreateFromLoot(
            LootTable lootTable,
            Vector3 position,
            Transform parent = null)
        {
            if (lootTable == null) return null;

            ItemData drop = lootTable.GetRandomDrop();
            return drop != null ? Create(drop, 1, position, parent) : null;
        }
    }
}
