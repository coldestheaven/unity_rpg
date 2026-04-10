using UnityEngine;
using System;
using System.Collections.Generic;
using Framework.Events;

namespace RPG.Items
{
    /// <summary>
    /// 背包槽位（引用类型，序列化友好）。
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int quantity;

        public InventorySlot() { }

        public InventorySlot(ItemData data, int qty)
        {
            itemData = data;
            quantity = qty;
        }

        public bool IsEmpty => itemData == null || quantity <= 0;

        public void AddQuantity(int amount) => quantity += amount;

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
            => itemData != null && otherItem != null
            && itemData == otherItem
            && itemData.maxStackSize > 1
            && quantity < itemData.maxStackSize;

        public void Clear()
        {
            itemData = null;
            quantity  = 0;
        }
    }

    /// <summary>物品操作结果。</summary>
    public enum InventoryOperationResult
    {
        Success,
        Failed_InventoryFull,
        Failed_ItemNotFound,
        Failed_NotEnoughQuantity,
        Failed_InvalidItem,
        Failed_ItemNotStackable,
    }

    // ── 存档数据结构（仅供内部序列化用） ──────────────────────────────────────────

    [Serializable]
    internal struct InventorySaveData
    {
        public float               gold;
        public InventorySlotSaveData[] slots;
    }

    [Serializable]
    internal struct InventorySlotSaveData
    {
        public string itemId;   // ItemData.itemId 或 asset name 作为回退
        public int    quantity;
    }

    // ── InventorySystem ───────────────────────────────────────────────────────────

    /// <summary>
    /// 背包系统。
    ///
    /// ■ 功能：
    ///   • 固定槽位数的格子背包，支持物品堆叠。
    ///   • 金币管理（AddGold / RemoveGold），发布 <see cref="GoldChangedEvent"/>。
    ///   • JSON 序列化 / 反序列化（存读档）：
    ///       SerializeInventory()        → JSON string
    ///       DeserializeInventory(json, db) → 根据 ItemDatabase 恢复物品引用
    ///   • 已缓存 UsedSlots 计数，避免每帧 O(n) 遍历。
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("背包设置")]
        public int maxSlots = 20;

        [SerializeField] private float _gold = 0f;
        public float Gold => _gold;

        private List<InventorySlot> _slots;
        private int _usedSlots; // 缓存已用格数，避免每次 O(n)

        // ── 属性 ──────────────────────────────────────────────────────────────────

        public int SlotCount  => _slots.Count;
        public int UsedSlots  => _usedSlots;
        public int EmptySlots => maxSlots - _usedSlots;

        // ── 事件 ──────────────────────────────────────────────────────────────────

        /// <param name="usedSlots">触发时已使用的槽位数。</param>
        public event Action<int>           OnInventoryChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        public event Action<float>         OnGoldChanged;

        // ── 生命周期 ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots = new List<InventorySlot>(maxSlots);
            for (int i = 0; i < maxSlots; i++)
                _slots.Add(new InventorySlot());
            _usedSlots = 0;
        }

        // ── 核心操作 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 向背包中添加物品。优先堆叠到现有槽，再分配新空槽。
        /// </summary>
        public InventoryOperationResult AddItem(ItemData item, int quantity = 1)
        {
            if (item == null)     return InventoryOperationResult.Failed_InvalidItem;
            if (quantity <= 0)    return InventoryOperationResult.Failed_InvalidItem;

            int remaining = quantity;

            // Step 1：尝试堆叠到现有同类槽
            if (item.CanStack())
            {
                for (int i = 0; i < _slots.Count && remaining > 0; i++)
                {
                    if (!_slots[i].CanStack(item)) continue;
                    int space = item.maxStackSize - _slots[i].quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    _slots[i].AddQuantity(toAdd);
                    remaining -= toAdd;
                }
            }

            // Step 2：将剩余量填入空槽
            while (remaining > 0)
            {
                int slotIdx = GetFirstEmptySlotIndex();
                if (slotIdx < 0)
                {
                    // 背包满，触发已添加部分的通知后返回失败
                    if (remaining < quantity)
                    {
                        NotifyAdded(item, quantity - remaining);
                    }
                    NotifyChanged();
                    return InventoryOperationResult.Failed_InventoryFull;
                }

                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                _slots[slotIdx].itemData = item;
                _slots[slotIdx].quantity = toAdd;
                remaining   -= toAdd;
                _usedSlots++;
            }

            NotifyAdded(item, quantity);
            NotifyChanged();
            return InventoryOperationResult.Success;
        }

        /// <summary>
        /// 从背包移除物品（从后往前取，优先清空小堆）。
        /// </summary>
        public InventoryOperationResult RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null)  return InventoryOperationResult.Failed_InvalidItem;

            if (GetItemCount(item) < quantity)
                return InventoryOperationResult.Failed_NotEnoughQuantity;

            int remaining = quantity;
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].itemData != item) continue;

                int toRemove = Mathf.Min(_slots[i].quantity, remaining);
                _slots[i].RemoveQuantity(toRemove);
                remaining -= toRemove;

                if (_slots[i].IsEmpty)
                    _usedSlots--;
            }

            NotifyRemoved(item, quantity);
            NotifyChanged();
            return InventoryOperationResult.Success;
        }

        /// <summary>
        /// 交换两个槽位的内容（拖拽排序）。
        /// 若目标槽与源槽物品相同且可堆叠，则合并堆叠。
        /// </summary>
        public bool MoveItem(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
                return false;

            var from = _slots[fromIndex];
            var to   = _slots[toIndex];

            // 目标可堆叠时尝试合并
            if (!from.IsEmpty && !to.IsEmpty && from.itemData == to.itemData
                && from.itemData.CanStack())
            {
                int space  = from.itemData.maxStackSize - to.quantity;
                int toMove = Mathf.Min(from.quantity, space);
                if (toMove > 0)
                {
                    to.AddQuantity(toMove);
                    from.RemoveQuantity(toMove);
                    if (from.IsEmpty) _usedSlots--;
                    NotifyChanged();
                    return true;
                }
            }

            // 交换
            (from.itemData, to.itemData) = (to.itemData, from.itemData);
            (from.quantity,  to.quantity)  = (to.quantity,  from.quantity);

            // 更新 _usedSlots（交换不改变总已用数，但确保一致性）
            _usedSlots = RecalcUsedSlots();
            NotifyChanged();
            return true;
        }

        // ── 查询 ──────────────────────────────────────────────────────────────────

        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            int count = 0;
            foreach (var s in _slots)
                if (s.itemData == item) count += s.quantity;
            return count;
        }

        public bool HasItem(ItemData item)              => GetItemCount(item) > 0;
        public bool HasItem(ItemData item, int quantity) => GetItemCount(item) >= quantity;

        public InventorySlot GetSlot(int index)
            => IsValidIndex(index) ? _slots[index] : null;

        /// <summary>返回所有槽的只读快照数组（每次调用均分配新数组）。</summary>
        public InventorySlot[] GetAllSlots() => _slots.ToArray();

        /// <summary>
        /// 遍历所有槽（零分配），用于只读迭代场景。
        /// </summary>
        public List<InventorySlot> GetSlotsInternal() => _slots;

        /// <summary>查找物品所在的第一个槽位索引，未找到返回 -1。</summary>
        public int FindItemIndex(ItemData item)
        {
            if (item == null) return -1;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].itemData == item) return i;
            return -1;
        }

        // ── 清空 ──────────────────────────────────────────────────────────────────

        public void ClearInventory()
        {
            foreach (var slot in _slots) slot.Clear();
            _usedSlots = 0;
            NotifyChanged();
        }

        // ── 金币 ──────────────────────────────────────────────────────────────────

        public void AddGold(float amount)
        {
            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
            EventBus.Publish(new GoldChangedEvent((int)_gold, (int)amount));
        }

        public bool RemoveGold(float amount)
        {
            if (_gold < amount) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            EventBus.Publish(new GoldChangedEvent((int)_gold, -(int)amount));
            return true;
        }

        // ── 序列化 / 存档 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 将背包数据序列化为 JSON 字符串。
        /// 物品以 <see cref="ItemData.itemId"/>（优先）或 asset name 作为 key 存储。
        /// </summary>
        public string SerializeInventory()
        {
            var slotData = new InventorySlotSaveData[_slots.Count];
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                if (s.IsEmpty)
                {
                    slotData[i] = new InventorySlotSaveData { itemId = string.Empty, quantity = 0 };
                }
                else
                {
                    string id = string.IsNullOrEmpty(s.itemData.itemId)
                        ? s.itemData.name      // 回退到 Unity asset 名称
                        : s.itemData.itemId;
                    slotData[i] = new InventorySlotSaveData { itemId = id, quantity = s.quantity };
                }
            }

            var saveData = new InventorySaveData { gold = _gold, slots = slotData };
            return JsonUtility.ToJson(saveData);
        }

        /// <summary>
        /// 从 JSON 字符串恢复背包数据。
        /// 需要传入 <see cref="ItemDatabase"/> 以便通过 ID 查找物品资产引用。
        /// </summary>
        /// <param name="json">由 <see cref="SerializeInventory"/> 生成的 JSON 字符串。</param>
        /// <param name="database">物品数据库，用于 ID → ItemData 映射。</param>
        public void DeserializeInventory(string json, ItemDatabase database)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[InventorySystem] DeserializeInventory: 数据为空。");
                return;
            }

            InventorySaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<InventorySaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventorySystem] 反序列化失败: {e.Message}");
                return;
            }

            // 重置背包
            InitializeSlots();
            _gold = saveData.gold;

            if (saveData.slots == null) return;

            int slotCount = Mathf.Min(saveData.slots.Length, maxSlots);
            for (int i = 0; i < slotCount; i++)
            {
                var sd = saveData.slots[i];
                if (string.IsNullOrEmpty(sd.itemId) || sd.quantity <= 0) continue;

                ItemData itemData = database?.GetItem(sd.itemId);
                if (itemData == null)
                {
                    Debug.LogWarning($"[InventorySystem] 无法找到物品: {sd.itemId}，槽位 {i} 将留空。");
                    continue;
                }

                _slots[i].itemData = itemData;
                _slots[i].quantity = sd.quantity;
                _usedSlots++;
            }

            OnGoldChanged?.Invoke(_gold);
            NotifyChanged();
        }

        /// <summary>
        /// 向后兼容的无参重载（不传 database，尝试从 ItemSystem.Instance 获取）。
        /// </summary>
        public void DeserializeInventory(string json)
        {
            var db = ItemSystem.Instance?.itemDatabase;
            if (db == null)
                Debug.LogWarning("[InventorySystem] 未找到 ItemDatabase，物品引用无法恢复。");
            DeserializeInventory(json, db);
        }

        // ── 私有辅助 ──────────────────────────────────────────────────────────────

        private bool IsValidIndex(int index) => index >= 0 && index < _slots.Count;

        private int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].IsEmpty) return i;
            return -1;
        }

        private int RecalcUsedSlots()
        {
            int count = 0;
            foreach (var s in _slots) if (!s.IsEmpty) count++;
            return count;
        }

        private void NotifyAdded(ItemData item, int qty)    => OnItemAdded?.Invoke(item, qty);
        private void NotifyRemoved(ItemData item, int qty)  => OnItemRemoved?.Invoke(item, qty);
        private void NotifyChanged()                        => OnInventoryChanged?.Invoke(_usedSlots);
    }
}
