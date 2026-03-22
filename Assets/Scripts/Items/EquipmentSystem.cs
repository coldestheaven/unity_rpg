using UnityEngine;
using System.Collections.Generic;

namespace RPG.Items
{
    /// <summary>
    /// 装备系统 - 管理玩家装备
    /// </summary>
    public class EquipmentSystem : MonoBehaviour
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

            // 应用装备效果
            ApplyEquipmentStats(equipment, true);

            OnEquipmentChanged?.Invoke(slot, equipment);

            RPG.Core.EventManager.Instance?.TriggerEvent("ItemEquipped", new EquipmentEventArgs
            {
                itemData = equipment,
                slot = slot,
                isEquipped = true
            });

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

            // 移除装备效果
            ApplyEquipmentStats(item, false);

            // 返回到背包
            if (inventory != null)
            {
                inventory.AddItem(item, 1);
            }

            equippedItems[slot] = null;

            OnEquipmentUnequipped?.Invoke(slot);

            RPG.Core.EventManager.Instance?.TriggerEvent("ItemEquipped", new EquipmentEventArgs
            {
                itemData = item,
                slot = slot,
                isEquipped = false
            });

            Debug.Log($"Unequipped item from {slot}");
            return true;
        }

        /// <summary>
        /// 交换装备(穿戴背包中的装备)
        /// </summary>
        public bool SwapEquipment(EquipmentData fromInventory)
        {
            if (fromInventory == null) return false;

            EquipmentSlot slot = fromInventory.equipmentSlot;
            EquipmentData currentEquipped = equippedItems[slot];

            // 从背包移除
            if (inventory != null && !inventory.RemoveItem(fromInventory, 1))
            {
                return false;
            }

            // 卸下当前装备
            if (currentEquipped != null)
            {
                ApplyEquipmentStats(currentEquipped, false);

                // 返回到背包
                if (inventory != null)
                {
                    inventory.AddItem(currentEquipped, 1);
                }
            }

            // 装备新物品
            equippedItems[slot] = fromInventory;
            ApplyEquipmentStats(fromInventory, true);

            OnEquipmentChanged?.Invoke(slot, fromInventory);

            RPG.Core.EventManager.Instance?.TriggerEvent("ItemEquipped", new EquipmentEventArgs
            {
                itemData = fromInventory,
                slot = slot,
                isEquipped = true
            });

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

        /// <summary>
        /// 应用/移除装备属性
        /// </summary>
        private void ApplyEquipmentStats(EquipmentData equipment, bool apply)
        {
            if (equipment == null) return;

            int multiplier = apply ? 1 : -1;

            var playerHealth = GetComponent<RPG.Player.PlayerHealth>();
            if (playerHealth != null)
            {
                // 应用生命加成
                playerHealth.Heal(equipment.healthBonus * multiplier);
            }

            // TODO: 应用其他属性到玩家系统
            var playerController = GetComponent<RPG.Player.PlayerController>();
            if (playerController != null)
            {
                if (equipment.attackPowerBonus != 0)
                {
                    playerController.SetAttackDamage(playerController.GetComponent<RPG.Player.PlayerCombat>()?.attackDamage ?? 0 + equipment.attackPowerBonus * multiplier);
                }

                if (equipment.moveSpeedBonus != 0)
                {
                    playerController.SetMoveSpeed(playerController.GetComponent<RPG.Player.PlayerMovement>()?.moveSpeed ?? 0 + equipment.moveSpeedBonus * multiplier);
                }
            }

            Debug.Log($"{(apply ? "Applied" : "Removed")} equipment stats for {equipment.itemName}");
        }

        /// <summary>
        /// 清空所有装备
        /// </summary>
        public void ClearAllEquipment()
        {
            List<EquipmentSlot> slotsToClear = new List<EquipmentSlot>();

            foreach (var pair in equippedItems)
            {
                if (pair.Value != null)
                {
                    slotsToClear.Add(pair.Key);
                }
            }

            foreach (var slot in slotsToClear)
            {
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

    /// <summary>
    /// 装备事件参数
    /// </summary>
    [System.Serializable]
    public class EquipmentEventArgs
    {
        public EquipmentData itemData;
        public EquipmentSlot slot;
        public bool isEquipped;
    }
}
