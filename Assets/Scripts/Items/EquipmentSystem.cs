using UnityEngine;
using System;
using System.Collections.Generic;
using Core.Stats;
using Framework.Core.Pools;
using Framework.Events;

namespace RPG.Items
{
    /// <summary>
    /// 装备系统 - 管理玩家装备
    /// </summary>
    public class EquipmentSystem : MonoBehaviour, IPlayerStatModifierSource
    {
        [System.Serializable]
        public class EquipmentSlotData
        {
            public EquipmentSlot slotType;
            public EquipmentData equippedItem;
            public EquipmentSlotData(EquipmentSlot type)
            {
                slotType = type;
            }
        }

        private Dictionary<EquipmentSlot, EquipmentData> equippedItems;
        private InventorySystem inventory;

        public EquipmentData this[EquipmentSlot slot] => equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;

        public event Action<EquipmentSlot, EquipmentData> OnEquipmentChanged;
        public event Action<EquipmentSlot> OnEquipmentUnequipped;
        public event Action ModifiersChanged;

        private void Awake()
        {
            InitializeEquipmentSystem();
        }

        private void InitializeEquipmentSystem()
        {
            equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
            inventory = GetComponent<InventorySystem>();

            // 初始化所有装备槽位
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }
        }

        /// <summary>
        /// 装备物品
        /// </summary>
        public bool EquipItem(EquipmentData equipment)
        {
            if (equipment == null) return false;

            EquipmentSlot slot = equipment.equipmentSlot;
            EquipmentData currentItem = equippedItems[slot];

            // 如果当前槽位已有装备,先卸下
            if (currentItem != null)
            {
                UnequipItem(slot);
            }

            // 装备新物品
            equippedItems[slot] = equipment;

            OnEquipmentChanged?.Invoke(slot, equipment);
            NotifyModifiersChanged();

            Framework.Events.EventBus.Publish(new Framework.Events.ItemEquippedEvent(equipment.name, slot.ToString(), true));

            Debug.Log($"Equipped {equipment.itemName} to {slot}");
            return true;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot)) return false;

            EquipmentData item = equippedItems[slot];
            if (item == null) return false;

            // 返回到背包
            if (inventory != null)
            {
                inventory.AddItem(item, 1);
            }

            equippedItems[slot] = null;

            OnEquipmentUnequipped?.Invoke(slot);
            NotifyModifiersChanged();

            Framework.Events.EventBus.Publish(new Framework.Events.ItemEquippedEvent(item.name, slot.ToString(), false));

            Debug.Log($"Unequipped item from {slot}");
            return true;
        }

        /// <summary>
        /// 从背包中交换装备（背包 → 装备槽，原装备 → 背包）。
        /// </summary>
        public bool SwapEquipment(EquipmentData fromInventory)
        {
            if (fromInventory == null) return false;

            EquipmentSlot slot = fromInventory.equipmentSlot;
            EquipmentData currentEquipped = equippedItems[slot];

            // 从背包移除（修复：RemoveItem 返回 InventoryOperationResult，不是 bool）
            if (inventory != null &&
                inventory.RemoveItem(fromInventory, 1) != InventoryOperationResult.Success)
            {
                return false;
            }

            // 将当前槽位装备返还背包
            if (currentEquipped != null && inventory != null)
                inventory.AddItem(currentEquipped, 1);

            // 装入新装备
            equippedItems[slot] = fromInventory;

            OnEquipmentChanged?.Invoke(slot, fromInventory);
            NotifyModifiersChanged();
            Framework.Events.EventBus.Publish(
                new Framework.Events.ItemEquippedEvent(fromInventory.name, slot.ToString(), true));

            return true;
        }

        /// <summary>
        /// 检查是否装备了指定物品
        /// </summary>
        public bool IsEquipped(EquipmentData equipment)
        {
            foreach (var slot in equippedItems.Values)
            {
                if (slot == equipment) return true;
            }
            return false;
        }

        /// <summary>
        /// 检查指定槽位是否有装备
        /// </summary>
        public bool HasEquipmentInSlot(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) && equippedItems[slot] != null;
        }

        /// <summary>
        /// 获取所有已装备的物品
        /// </summary>
        public Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
        }

        /// <summary>
        /// 获取装备提供的总属性加成
        /// </summary>
        public EquipmentStats GetTotalStats()
        {
            EquipmentStats stats = new EquipmentStats();

            foreach (var item in equippedItems.Values)
            {
                if (item != null)
                {
                    stats.attackPower += item.attackPowerBonus;
                    stats.defense += item.defenseBonus;
                    stats.health += item.healthBonus;
                    stats.mana += item.manaBonus;
                    stats.moveSpeed += item.moveSpeedBonus;
                }
            }

            return stats;
        }

        public void ApplyModifiers(ref PlayerStatBlock stats)
        {
            EquipmentStats equipmentStats = GetTotalStats();
            stats.Add(
                equipmentStats.health,
                equipmentStats.attackPower,
                equipmentStats.defense,
                equipmentStats.moveSpeed);
        }

        private void NotifyModifiersChanged()
        {
            ModifiersChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有装备
        /// </summary>
        public void ClearAllEquipment()
        {
            using (ListPool<EquipmentSlot>.Rent(out var slotsToClear))
            {
                foreach (var pair in equippedItems)
                    if (pair.Value != null)
                        slotsToClear.Add(pair.Key);

                foreach (var slot in slotsToClear)
                    UnequipItem(slot);
            }
        }
    }

    /// <summary>
    /// 装备属性汇总
    /// </summary>
    [System.Serializable]
    public class EquipmentStats
    {
        public int attackPower;
        public int defense;
        public int health;
        public int mana;
        public float moveSpeed;
    }

}
