using Framework.Assets;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 物品系统管理器 - 整合所有物品相关功能
    /// </summary>
    public class ItemSystem : MonoBehaviour
    {
        public static ItemSystem Instance { get; private set; }

        [Header("引用")]
        public InventorySystem inventory;
        public EquipmentSystem equipment;

        [Header("物品数据库")]
        public ItemDatabase itemDatabase;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeReferences();
            SubscribeToEvents();
        }

        private void InitializeReferences()
        {
            if (inventory == null)
            {
                inventory = GetComponent<InventorySystem>();
            }

            if (equipment == null)
            {
                equipment = GetComponent<EquipmentSystem>();
            }

            if (itemDatabase == null)
            {
                itemDatabase = AssetService.Load<ItemDatabase>(AssetPaths.Data.ItemDatabase);
            }
        }

        private void SubscribeToEvents()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded += OnItemAdded;
                inventory.OnItemRemoved += OnItemRemoved;
                inventory.OnGoldChanged += OnGoldChanged;
            }

            if (equipment != null)
            {
                equipment.OnEquipmentChanged += OnEquipmentChanged;
                equipment.OnEquipmentUnequipped += OnEquipmentUnequipped;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded -= OnItemAdded;
                inventory.OnItemRemoved -= OnItemRemoved;
                inventory.OnGoldChanged -= OnGoldChanged;
            }

            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= OnEquipmentChanged;
                equipment.OnEquipmentUnequipped -= OnEquipmentUnequipped;
            }
        }

        #region Public Methods

        /// <summary>
        /// 添加物品
        /// </summary>
        public InventoryOperationResult AddItem(ItemData item, int quantity = 1)
        {
            return inventory?.AddItem(item, quantity) ?? InventoryOperationResult.Failed_InventoryFull;
        }

        /// <summary>
        /// 添加物品(通过ID)
        /// </summary>
        public InventoryOperationResult AddItem(string itemId, int quantity = 1)
        {
            ItemData item = itemDatabase?.GetItem(itemId);
            return AddItem(item, quantity);
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public InventoryOperationResult RemoveItem(ItemData item, int quantity = 1)
        {
            return inventory?.RemoveItem(item, quantity) ?? InventoryOperationResult.Failed_ItemNotFound;
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        public void UseItem(ItemData item)
        {
            if (item == null) return;

            if (inventory.HasItem(item, 1))
            {
                item.Use(gameObject);

                if (item.itemType == ItemType.Consumable)
                {
                    inventory.RemoveItem(item, 1);
                }
            }
        }

        /// <summary>
        /// 装备物品
        /// </summary>
        public bool EquipItem(EquipmentData equipmentData)
        {
            if (equipmentData == null || equipment == null) return false;

            if (inventory.HasItem(equipmentData, 1))
            {
                inventory.RemoveItem(equipmentData, 1);
                return equipment.EquipItem(equipmentData);
            }

            return false;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            return equipment?.UnequipItem(slot) ?? false;
        }

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItemData(string itemId)
        {
            return itemDatabase?.GetItem(itemId);
        }

        /// <summary>
        /// 生成掉落物品
        /// </summary>
        public void SpawnLoot(Vector3 position, LootTable lootTable)
        {
            ItemData item = lootTable?.GetRandomDrop();
            if (item != null)
            {
                CreatePickup(position, item, 1);
            }
        }

        /// <summary>
        /// 创建拾取物 (delegates to ItemPickupFactory)
        /// </summary>
        public ItemPickup CreatePickup(Vector3 position, ItemData item, int quantity = 1)
        {
            return ItemPickupFactory.Create(item, quantity, position);
        }

        /// <summary>
        /// 添加金币
        /// </summary>
        public void AddGold(float amount)
        {
            inventory?.AddGold(amount);
        }

        /// <summary>
        /// 移除金币
        /// </summary>
        public bool RemoveGold(float amount)
        {
            return inventory?.RemoveGold(amount) ?? false;
        }

        #endregion

        #region Event Handlers

        private void OnItemAdded(ItemData item, int quantity)
        {
            Debug.Log($"Item added: {item.name} x{quantity}");
        }

        private void OnItemRemoved(ItemData item, int quantity)
        {
            Debug.Log($"Item removed: {item.name} x{quantity}");
        }

        private void OnGoldChanged(float gold)
        {
            Debug.Log($"Gold changed: {gold}");
        }

        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentData item)
        {
            Debug.Log($"Equipment changed at {slot}: {item?.itemName}");
        }

        private void OnEquipmentUnequipped(EquipmentSlot slot)
        {
            Debug.Log($"Equipment unequipped from {slot}");
        }

        #endregion

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
