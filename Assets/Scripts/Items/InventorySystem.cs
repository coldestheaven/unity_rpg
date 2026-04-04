using UnityEngine;
using System;
using System.Collections.Generic;
using Framework.Events;

namespace RPG.Items
{
    /// <summary>
    /// 背包槽位
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int quantity;

        public InventorySlot(ItemData data, int qty)
        {
            itemData = data;
            quantity = qty;
        }

        public bool IsEmpty => itemData == null || quantity <= 0;

        public void AddQuantity(int amount)
        {
            quantity += amount;
        }

        public void RemoveQuantity(int amount)
        {
            quantity -= amount;
            if (quantity <= 0)
            {
                itemData = null;
                quantity = 0;
            }
        }

        public bool CanStack(ItemData otherItem)
        {
            return itemData != null && otherItem != null &&
                   itemData == otherItem &&
                   itemData.maxStackSize > 1 &&
                   quantity < itemData.maxStackSize;
        }
    }

    /// <summary>
    /// 物品操作结果
    /// </summary>
    public enum InventoryOperationResult
    {
        Success,
        Failed_InventoryFull,
        Failed_ItemNotFound,
        Failed_NotEnoughQuantity,
        Failed_InvalidItem,
        Failed_ItemNotStackable
    }

    /// <summary>
    /// 背包系统 - 重构版
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("背包设置")]
        public int maxSlots = 20;
        public float gold = 0f;

        private List<InventorySlot> slots;

        public int SlotCount => slots.Count;
        public int UsedSlots => GetUsedSlotCount();
        public int EmptySlots => maxSlots - UsedSlots;

        public event Action<int> OnInventoryChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        public event Action<float> OnGoldChanged;

        private void Awake()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            slots = new List<InventorySlot>();
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot(null, 0));
            }
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public InventoryOperationResult AddItem(ItemData item, int quantity = 1)
        {
            if (item == null)
            {
                return InventoryOperationResult.Failed_InvalidItem;
            }

            // 尝试堆叠到现有物品
            if (item.CanStack())
            {
                int remainingQuantity = quantity;

                for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
                {
                    if (slots[i].CanStack(item))
                    {
                        int spaceAvailable = item.maxStackSize - slots[i].quantity;
                        int toAdd = Mathf.Min(spaceAvailable, remainingQuantity);

                        slots[i].AddQuantity(toAdd);
                        remainingQuantity -= toAdd;
                    }
                }

                if (remainingQuantity <= 0)
                {
                    OnItemAdded?.Invoke(item, quantity);
                    OnInventoryChanged?.Invoke(UsedSlots);
                    return InventoryOperationResult.Success;
                }

                quantity = remainingQuantity;
            }

            // 添加到空槽位
            while (quantity > 0 && HasEmptySlot())
            {
                int slotIndex = GetFirstEmptySlotIndex();
                int toAdd = Mathf.Min(item.maxStackSize, quantity);

                slots[slotIndex] = new InventorySlot(item, toAdd);
                quantity -= toAdd;
            }

            if (quantity > 0)
            {
                // 无法完全添加
                OnInventoryChanged?.Invoke(UsedSlots);
                return InventoryOperationResult.Failed_InventoryFull;
            }

            OnItemAdded?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke(UsedSlots);
            return InventoryOperationResult.Success;
        }

        /// <summary>
        /// 从背包移除物品
        /// </summary>
        public InventoryOperationResult RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null)
            {
                return InventoryOperationResult.Failed_InvalidItem;
            }

            int totalAvailable = GetItemCount(item);
            if (totalAvailable < quantity)
            {
                return InventoryOperationResult.Failed_NotEnoughQuantity;
            }

            int remainingToRemove = quantity;

            for (int i = slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                if (slots[i].itemData == item)
                {
                    int toRemove = Mathf.Min(slots[i].quantity, remainingToRemove);
                    slots[i].RemoveQuantity(toRemove);
                    remainingToRemove -= toRemove;

                    if (slots[i].IsEmpty)
                    {
                        slots[i] = new InventorySlot(null, 0);
                    }
                }
            }

            OnItemRemoved?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke(UsedSlots);
            return InventoryOperationResult.Success;
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;

            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.itemData == item)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        /// <summary>
        /// 检查是否拥有物品
        /// </summary>
        public bool HasItem(ItemData item)
        {
            return GetItemCount(item) > 0;
        }

        /// <summary>
        /// 检查是否有足够数量的物品
        /// </summary>
        public bool HasItem(ItemData item, int quantity)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// 获取指定槽位的物品
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                return slots[index];
            }
            return null;
        }

        /// <summary>
        /// 获取所有槽位
        /// </summary>
        public InventorySlot[] GetAllSlots()
        {
            return slots.ToArray();
        }

        /// <summary>
        /// 移动物品到指定槽位
        /// </summary>
        public bool MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= slots.Count ||
                toIndex < 0 || toIndex >= slots.Count ||
                fromIndex == toIndex)
            {
                return false;
            }

            // 交换槽位
            InventorySlot temp = slots[fromIndex];
            slots[fromIndex] = slots[toIndex];
            slots[toIndex] = temp;

            OnInventoryChanged?.Invoke(UsedSlots);
            return true;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public void ClearInventory()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i] = new InventorySlot(null, 0);
            }
            OnInventoryChanged?.Invoke(UsedSlots);
        }

        /// <summary>
        /// 添加金币
        /// </summary>
        public void AddGold(float amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);

            EventManager.Instance?.TriggerEvent("GoldChanged", new GoldEventArgs
            {
                currentGold = gold,
                changeAmount = amount
            });
        }

        /// <summary>
        /// 移除金币
        /// </summary>
        public bool RemoveGold(float amount)
        {
            if (gold < amount) return false;

            gold -= amount;
            OnGoldChanged?.Invoke(gold);

            EventManager.Instance?.TriggerEvent("GoldChanged", new GoldEventArgs
            {
                currentGold = gold,
                changeAmount = -amount
            });

            return true;
        }

        /// <summary>
        /// 获取已使用的槽位数量
        /// </summary>
        private int GetUsedSlotCount()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 检查是否有空槽位
        /// </summary>
        private bool HasEmptySlot()
        {
            return GetUsedSlotCount() < maxSlots;
        }

        /// <summary>
        /// 获取第一个空槽位索引
        /// </summary>
        private int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 保存背包数据
        /// </summary>
        public string SerializeInventory()
        {
            // TODO: 实现序列化
            return string.Empty;
        }

        /// <summary>
        /// 加载背包数据
        /// </summary>
        public void DeserializeInventory(string data)
        {
            // TODO: 实现反序列化
        }
    }

    /// <summary>
    /// 金币变更事件参数
    /// </summary>
    [System.Serializable]
    public class GoldEventArgs
    {
        public float currentGold;
        public float changeAmount;
    }

    /// <summary>
    /// 物品使用事件参数
    /// </summary>
    [System.Serializable]
    public class ItemUsedEventArgs
    {
        public string itemName;
        public ItemType itemType;
        public int value;
    }

    /// <summary>
    /// 任务物品事件参数
    /// </summary>
    [System.Serializable]
    public class QuestItemEventArgs
    {
        public string itemName;
        public string questId;
    }
}
