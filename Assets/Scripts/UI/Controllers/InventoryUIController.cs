using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UI.Controllers
{
    /// <summary>
    /// 背包UI控制器
    /// </summary>
    public class InventoryUIController : UIPanelBase
    {
        [Header("Inventory")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsContainer;

        [Header("Item Details")]
        [SerializeField] private GameObject itemDetailsPanel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Text itemName;
        [SerializeField] private Text itemDescription;

        private Gameplay.Inventory.InventorySystem inventory;
        private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
        private Gameplay.Inventory.InventorySlot selectedSlot;

        protected override void Initialize()
        {
            base.Initialize();

            inventory = FindAnyObjectByType<Gameplay.Inventory.InventorySystem>();
            if (inventory != null)
            {
                inventory.OnInventoryChanged += RefreshInventory;
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= RefreshInventory;
            }
        }

        public void RefreshInventory()
        {
            ClearSlots();
            CreateSlots();
        }

        private void ClearSlots()
        {
            foreach (var slotUI in slotUIs)
            {
                if (slotUI != null)
                {
                    Destroy(slotUI.gameObject);
                }
            }
            slotUIs.Clear();
        }

        private void CreateSlots()
        {
            if (inventory == null) return;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot == null) continue;

                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                var slotUI = slotObj.GetComponent<InventorySlotUI>();

                if (slotUI != null)
                {
                    slotUI.Setup(slot, i);
                    slotUI.OnSlotClicked += HandleSlotClicked;
                    slotUIs.Add(slotUI);
                }
            }
        }

        private void HandleSlotClicked(int slotIndex)
        {
            var slot = inventory.GetSlot(slotIndex);
            if (slot != null && !slot.IsEmpty)
            {
                selectedSlot = slot;
                ShowItemDetails(slot);
            }
        }

        private void ShowItemDetails(Gameplay.Inventory.InventorySlot slot)
        {
            if (itemDetailsPanel != null)
            {
                itemDetailsPanel.SetActive(true);
                // Load item data from DataManager
            }
        }
    }

    public class InventorySlotUI : MonoBehaviour
    {
        public System.Action<int> OnSlotClicked;

        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button button;

        private int slotIndex;

        public void Setup(Gameplay.Inventory.InventorySlot slot, int index)
        {
            slotIndex = index;

            if (slot.IsEmpty)
            {
                if (icon != null) icon.enabled = false;
                if (amountText != null) amountText.text = "";
            }
            else
            {
                // Load icon from item data
                if (amountText != null) amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : "";
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSlotClicked?.Invoke(slotIndex));
            }
        }
    }
}
