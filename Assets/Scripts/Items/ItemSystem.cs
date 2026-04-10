using Framework.Assets;
using Framework.Events;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 物品系统门面（Facade / Singleton MonoBehaviour）。
    ///
    /// 整合 <see cref="InventorySystem"/>、<see cref="EquipmentSystem"/>、
    /// <see cref="ItemDatabase"/>，为外部模块提供统一的物品操作入口：
    /// 添加、移除、使用、装备、卸装、丢弃、生成掉落物。
    ///
    /// ■ 挂载要求：
    ///   • 与 <see cref="InventorySystem"/>、<see cref="EquipmentSystem"/> 在同一 GameObject 上。
    ///   • 调用 DontDestroyOnLoad，全局单例。
    ///
    /// ■ 关键修复：
    ///   • UseItem 正确将"玩家 GameObject"传入 <see cref="ItemData.Use"/>，
    ///     而非 ItemSystem 自身的 GameObject。
    ///   • EquipItem 返回成功前先从背包移除，避免双份持有。
    /// </summary>
    public class ItemSystem : MonoBehaviour
    {
        public static ItemSystem Instance { get; private set; }

        [Header("引用")]
        public InventorySystem inventory;
        public EquipmentSystem equipment;

        [Header("物品数据库")]
        public ItemDatabase itemDatabase;

        // ── 生命周期 ──────────────────────────────────────────────────────────────

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
            if (inventory == null)   inventory  = GetComponent<InventorySystem>();
            if (equipment == null)   equipment  = GetComponent<EquipmentSystem>();
            if (itemDatabase == null)
                itemDatabase = AssetService.Load<ItemDatabase>(AssetPaths.Data.ItemDatabase);
        }

        private void SubscribeToEvents()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded    += OnItemAdded;
                inventory.OnItemRemoved  += OnItemRemoved;
                inventory.OnGoldChanged  += OnGoldChanged;
            }
            if (equipment != null)
            {
                equipment.OnEquipmentChanged    += OnEquipmentChanged;
                equipment.OnEquipmentUnequipped += OnEquipmentUnequipped;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded    -= OnItemAdded;
                inventory.OnItemRemoved  -= OnItemRemoved;
                inventory.OnGoldChanged  -= OnGoldChanged;
            }
            if (equipment != null)
            {
                equipment.OnEquipmentChanged    -= OnEquipmentChanged;
                equipment.OnEquipmentUnequipped -= OnEquipmentUnequipped;
            }
        }

        // ── 背包操作 ──────────────────────────────────────────────────────────────

        /// <summary>向玩家背包添加物品。</summary>
        public InventoryOperationResult AddItem(ItemData item, int quantity = 1)
            => inventory?.AddItem(item, quantity) ?? InventoryOperationResult.Failed_InventoryFull;

        /// <summary>向玩家背包添加物品（通过 itemId 查找）。</summary>
        public InventoryOperationResult AddItem(string itemId, int quantity = 1)
            => AddItem(itemDatabase?.GetItem(itemId), quantity);

        /// <summary>从玩家背包移除物品。</summary>
        public InventoryOperationResult RemoveItem(ItemData item, int quantity = 1)
            => inventory?.RemoveItem(item, quantity) ?? InventoryOperationResult.Failed_ItemNotFound;

        /// <summary>
        /// 使用物品。
        /// </summary>
        /// <param name="item">要使用的物品。</param>
        /// <param name="user">使用者 GameObject（通常为玩家）；为 null 时尝试在场景中查找玩家。</param>
        public void UseItem(ItemData item, GameObject user = null)
        {
            if (item == null) return;
            if (inventory == null || !inventory.HasItem(item, 1)) return;

            // 确定使用者：优先使用传入的 user，否则查找带 Player Tag 的对象
            GameObject actualUser = user ?? FindPlayerGameObject();
            if (actualUser == null)
            {
                Debug.LogWarning("[ItemSystem] UseItem: 找不到玩家对象，物品无法使用。");
                return;
            }

            item.Use(actualUser);

            // 消耗品使用后从背包扣除
            if (item.itemType == ItemType.Consumable)
                inventory.RemoveItem(item, 1);
        }

        // ── 装备操作 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 装备背包中的装备物品。
        /// 成功时从背包移除，再由 <see cref="EquipmentSystem"/> 装入对应槽位；
        /// 原槽位已有装备则自动返还背包。
        /// </summary>
        public bool EquipItem(EquipmentData equipmentData)
        {
            if (equipmentData == null || equipment == null || inventory == null) return false;
            if (!inventory.HasItem(equipmentData, 1)) return false;

            inventory.RemoveItem(equipmentData, 1);
            return equipment.EquipItem(equipmentData);
        }

        /// <summary>卸下指定槽位的装备，返还背包。</summary>
        public bool UnequipItem(EquipmentSlot slot)
            => equipment?.UnequipItem(slot) ?? false;

        // ── 丢弃操作 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 从背包丢弃物品，在世界中生成对应拾取物。
        /// </summary>
        /// <param name="item">要丢弃的物品。</param>
        /// <param name="quantity">丢弃数量。</param>
        /// <param name="dropPosition">生成位置（通常为玩家脚下）。</param>
        public bool DropItem(ItemData item, int quantity, Vector3 dropPosition)
        {
            if (item == null || !item.isDroppable) return false;
            if (inventory == null) return false;

            InventoryOperationResult result = inventory.RemoveItem(item, quantity);
            if (result != InventoryOperationResult.Success) return false;

            CreatePickup(dropPosition, item, quantity);

            item.OnDrop(dropPosition);
            EventBus.Publish(new ItemDroppedEvent(
                string.IsNullOrEmpty(item.itemId) ? item.name : item.itemId,
                item.itemName,
                quantity,
                dropPosition));

            return true;
        }

        // ── 掉落生成 ──────────────────────────────────────────────────────────────

        /// <summary>在世界中生成单件物品的拾取物体。</summary>
        public ItemPickup CreatePickup(Vector3 position, ItemData item, int quantity = 1)
            => ItemPickupFactory.Create(item, quantity, position);

        /// <summary>
        /// 根据掉落表在世界中随机生成一件物品。
        /// </summary>
        public void SpawnLoot(Vector3 position, LootTable lootTable)
        {
            if (lootTable == null) return;
            ItemData item = lootTable.GetRandomDrop();
            if (item != null)
                CreatePickup(position, item, 1);
        }

        /// <summary>
        /// 根据掉落表生成多件物品（使用 GetDrops，最多 maxDropCount 件）。
        /// </summary>
        public void SpawnLootBatch(Vector3 position, LootTable lootTable, float spread = 0.5f)
        {
            if (lootTable == null) return;
            var drops = lootTable.GetDrops();
            foreach (var (item, qty) in drops)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-spread, spread),
                    0f,
                    Random.Range(-spread, spread));
                CreatePickup(position + offset, item, qty);
            }
        }

        // ── 数据查询 ──────────────────────────────────────────────────────────────

        /// <summary>通过 itemId 从数据库获取物品数据。</summary>
        public ItemData GetItemData(string itemId) => itemDatabase?.GetItem(itemId);

        // ── 金币 ──────────────────────────────────────────────────────────────────

        public void AddGold(float amount)  => inventory?.AddGold(amount);
        public bool RemoveGold(float amount) => inventory?.RemoveGold(amount) ?? false;

        // ── 事件回调 ──────────────────────────────────────────────────────────────

        private void OnItemAdded(ItemData item, int quantity)
            => Debug.Log($"[ItemSystem] 获得: {item.itemName} ×{quantity}");

        private void OnItemRemoved(ItemData item, int quantity)
            => Debug.Log($"[ItemSystem] 移除: {item.itemName} ×{quantity}");

        private void OnGoldChanged(float gold)
            => Debug.Log($"[ItemSystem] 金币: {gold:F0}");

        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentData item)
            => Debug.Log($"[ItemSystem] 装备[{slot}]: {item?.itemName ?? "(空)"}");

        private void OnEquipmentUnequipped(EquipmentSlot slot)
            => Debug.Log($"[ItemSystem] 卸装[{slot}]");

        // ── 私有辅助 ──────────────────────────────────────────────────────────────

        private static GameObject FindPlayerGameObject()
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) return go;
            var ph = FindAnyObjectByType<Gameplay.Player.PlayerHealth>();
            return ph != null ? ph.gameObject : null;
        }

        // ── 销毁 ──────────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this) Instance = null;
        }
    }
}
