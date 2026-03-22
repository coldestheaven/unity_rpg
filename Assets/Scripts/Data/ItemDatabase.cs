using UnityEngine;
using System.Collections.Generic;

namespace RPG.Items
{
    /// <summary>
    /// 物品数据库 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RPG/Data/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [System.Serializable]
        public class ItemEntry
        {
            public string itemId;
            public ItemData itemData;
        }

        public ItemEntry[] items;

        private Dictionary<string, ItemData> itemDictionary;

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Initialize()
        {
            itemDictionary = new Dictionary<string, ItemData>();

            if (items != null)
            {
                foreach (var entry in items)
                {
                    if (entry != null && entry.itemData != null && !string.IsNullOrEmpty(entry.itemId))
                    {
                        itemDictionary[entry.itemId] = entry.itemData;
                    }
                }
            }

            Debug.Log($"ItemDatabase initialized with {itemDictionary.Count} items");
        }

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            // 确保字典已初始化
            if (itemDictionary == null)
            {
                Initialize();
            }

            itemDictionary.TryGetValue(itemId, out ItemData itemData);
            return itemData;
        }

        /// <summary>
        /// 获取所有物品
        /// </summary>
        public ItemData[] GetAllItems()
        {
            if (itemDictionary == null)
            {
                Initialize();
            }

            return new List<ItemData>(itemDictionary.Values).ToArray();
        }

        /// <summary>
        /// 根据类型获取物品
        /// </summary>
        public ItemData[] GetItemsByType(ItemType type)
        {
            List<ItemData> result = new List<ItemData>();

            foreach (var item in itemDictionary.Values)
            {
                if (item.itemType == type)
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 根据标签获取物品
        /// </summary>
        public ItemData[] GetItemsByTag(string tag)
        {
            List<ItemData> result = new List<ItemData>();

            foreach (var item in itemDictionary.Values)
            {
                if (item.HasTag(tag))
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 添加物品到数据库
        /// </summary>
        public void AddItem(string itemId, ItemData itemData)
        {
            if (itemDictionary == null)
            {
                Initialize();
            }

            itemDictionary[itemId] = itemData;

            // 同时更新数组(运行时)
            List<ItemEntry> entryList = new List<ItemEntry>(items);
            entryList.Add(new ItemEntry { itemId = itemId, itemData = itemData });
            items = entryList.ToArray();
        }

        /// <summary>
        /// 检查物品是否存在
        /// </summary>
        public bool ContainsItem(string itemId)
        {
            return itemDictionary.ContainsKey(itemId);
        }
    }
}
