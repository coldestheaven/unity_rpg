using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 背包系统
    /// </summary>
    public class InventorySystem : Framework.Base.MonoBehaviourBase
    {
        [Header("Inventory")]
        [SerializeField] private int slotCount = 20;
        [SerializeField] private int maxStackSize = 99;

        private List<InventorySlot> slots;

        public int SlotCount => slots.Count;
        public int TotalItems => CountTotalItems();

        public event System.Action OnInventoryChanged;

        protected override void Awake()
        {
            base.Awake();
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            slots = new List<InventorySlot>();
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new InventorySlot());
            }
        }

        public bool AddItem(string itemId, int amount = 1)
        {
            int remaining = amount;

            // Try to stack in existing slots
            foreach (var slot in slots)
            {
                if (slot.ItemId == itemId && slot.Amount < maxStackSize)
                {
                    int space = maxStackSize - slot.Amount;
                    int addAmount = Mathf.Min(space, remaining);

                    slot.Amount += addAmount;
                    remaining -= addAmount;

                    if (remaining <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            // Try to add to empty slots
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    int addAmount = Mathf.Min(maxStackSize, remaining);

                    slot.ItemId = itemId;
                    slot.Amount = addAmount;
                    remaining -= addAmount;

                    if (remaining <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining <= 0;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            int remaining = amount;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].ItemId == itemId)
                {
                    int removeAmount = Mathf.Min(slots[i].Amount, remaining);

                    slots[i].Amount -= removeAmount;
                    remaining -= removeAmount;

                    if (slots[i].Amount <= 0)
                    {
                        slots[i].Clear();
                    }

                    if (remaining <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining <= 0;
        }

        public int GetItemCount(string itemId)
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.ItemId == itemId)
                {
                    count += slot.Amount;
                }
            }
            return count;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                return slots[index];
            }
            return null;
        }

        private int CountTotalItems()
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
    }

    [System.Serializable]
    public class InventorySlot
    {
        public string ItemId;
        public int Amount;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Amount <= 0;

        public void Clear()
        {
            ItemId = null;
            Amount = 0;
        }
    }
}
