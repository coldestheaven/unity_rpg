using RPG.Items;
using UnityEngine;

namespace UI.Presenters
{
    public readonly struct InventorySlotViewData
    {
        public readonly int SlotIndex;
        public readonly bool IsEmpty;
        public readonly Sprite Icon;
        public readonly string AmountText;
        public readonly string ItemName;
        public readonly string Description;

        public InventorySlotViewData(int slotIndex, bool isEmpty, Sprite icon, string amountText, string itemName, string description)
        {
            SlotIndex = slotIndex;
            IsEmpty = isEmpty;
            Icon = icon;
            AmountText = amountText;
            ItemName = itemName;
            Description = description;
        }
    }

    public readonly struct InventoryDetailsViewData
    {
        public readonly bool Visible;
        public readonly Sprite Icon;
        public readonly string ItemName;
        public readonly string Description;

        public InventoryDetailsViewData(bool visible, Sprite icon, string itemName, string description)
        {
            Visible = visible;
            Icon = icon;
            ItemName = itemName;
            Description = description;
        }
    }

    public sealed class InventoryPresenter
    {
        private InventorySystem inventory;
        private int selectedSlotIndex = -1;

        public event System.Action<InventorySlotViewData[]> SlotsChanged;
        public event System.Action<InventoryDetailsViewData> DetailsChanged;

        public void Bind(InventorySystem inventorySystem)
        {
            if (ReferenceEquals(inventory, inventorySystem))
            {
                Refresh();
                return;
            }

            Unbind();
            inventory = inventorySystem;

            if (inventory != null)
            {
                inventory.OnInventoryChanged += HandleInventoryChanged;
                Refresh();
            }
            else
            {
                PublishEmpty();
            }
        }

        public void Unbind()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= HandleInventoryChanged;
                inventory = null;
            }

            selectedSlotIndex = -1;
        }

        public void Refresh()
        {
            PublishSlots();
            PublishDetails();
        }

        public void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            PublishDetails();
        }

        private void HandleInventoryChanged(int usedSlots)
        {
            if (inventory == null)
            {
                PublishEmpty();
                return;
            }

            if (selectedSlotIndex >= inventory.SlotCount)
            {
                selectedSlotIndex = -1;
            }

            var selectedSlot = selectedSlotIndex >= 0 ? inventory.GetSlot(selectedSlotIndex) : null;
            if (selectedSlot != null && selectedSlot.IsEmpty)
            {
                selectedSlotIndex = -1;
            }

            Refresh();
        }

        private void PublishSlots()
        {
            if (inventory == null)
            {
                PublishEmpty();
                return;
            }

            InventorySlotViewData[] viewData = new InventorySlotViewData[inventory.SlotCount];
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                viewData[i] = BuildSlotViewData(i, inventory.GetSlot(i));
            }

            SlotsChanged?.Invoke(viewData);
        }

        private void PublishDetails()
        {
            if (inventory == null || selectedSlotIndex < 0 || selectedSlotIndex >= inventory.SlotCount)
            {
                DetailsChanged?.Invoke(new InventoryDetailsViewData(false, null, string.Empty, string.Empty));
                return;
            }

            var slot = inventory.GetSlot(selectedSlotIndex);
            if (slot == null || slot.IsEmpty || slot.itemData == null)
            {
                DetailsChanged?.Invoke(new InventoryDetailsViewData(false, null, string.Empty, string.Empty));
                return;
            }

            DetailsChanged?.Invoke(new InventoryDetailsViewData(
                true,
                slot.itemData.icon,
                slot.itemData.itemName,
                slot.itemData.description));
        }

        private void PublishEmpty()
        {
            SlotsChanged?.Invoke(System.Array.Empty<InventorySlotViewData>());
            DetailsChanged?.Invoke(new InventoryDetailsViewData(false, null, string.Empty, string.Empty));
        }

        private static InventorySlotViewData BuildSlotViewData(int slotIndex, InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty || slot.itemData == null)
            {
                return new InventorySlotViewData(slotIndex, true, null, string.Empty, string.Empty, string.Empty);
            }

            return new InventorySlotViewData(
                slotIndex,
                false,
                slot.itemData.icon,
                slot.quantity > 1 ? slot.quantity.ToString() : string.Empty,
                slot.itemData.itemName,
                slot.itemData.description);
        }
    }
}
