using UnityEngine;
using UnityEngine.UI;
using RPG.Core;
using RPG.Items;

namespace RPG.UI
{
    /// <summary>
    /// 背包UI控制器
    /// </summary>
    public class InventoryUIController : UIPanel
    {
        [Header("背包设置")]
        public GameObject slotPrefab;
        public Transform contentPanel;
        public int slotsPerRow = 5;

        [Header("详细信息")]
        public GameObject detailPanel;
        public Image itemIcon;
        public Text itemName;
        public Text itemDescription;
        public Text itemQuantity;
        public Button useButton;
        public Button dropButton;

        [Header("金币显示")]
        public Text goldText;

        private InventorySlotUI[] slotUIs;
        private InventorySystem inventory;
        private ItemSystem itemSystem;
        private ItemData selectedItem;
        private int selectedSlotIndex = -1;

        protected override void Start()
        {
            base.Start();
            InitializeInventoryUI();
            SubscribeToEvents();
        }

        private void InitializeInventoryUI()
        {
            inventory = GetComponent<InventorySystem>();
            if (inventory == null)
            {
                inventory = InventorySystem.Instance?.GetComponent<InventorySystem>();
            }

            itemSystem = ItemSystem.Instance;

            CreateSlots();
            UpdateInventoryUI();
        }

        private void CreateSlots()
        {
            // 清空现有槽位
            foreach (Transform child in contentPanel)
            {
                Destroy(child.gameObject);
            }

            int maxSlots = inventory?.maxSlots ?? 20;
            slotUIs = new InventorySlotUI[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slotObject = Instantiate(slotPrefab, contentPanel);
                InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();

                if (slotUI == null)
                {
                    slotUI = slotObject.AddComponent<InventorySlotUI>();
                }

                slotUI.Initialize(i, OnSlotClicked);
                slotUIs[i] = slotUI;
            }
        }

        private void SubscribeToEvents()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged += OnInventoryChanged;
                inventory.OnGoldChanged += OnGoldChanged;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("ItemPickedUp", OnItemPickedUp);
                EventManager.Instance.AddListener("GoldChanged", OnGoldChangedEvent);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= OnInventoryChanged;
                inventory.OnGoldChanged -= OnGoldChanged;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("ItemPickedUp", OnItemPickedUp);
                EventManager.Instance.RemoveListener("GoldChanged", OnGoldChangedEvent);
            }
        }

        private void UpdateInventoryUI()
        {
            if (inventory == null || slotUIs == null) return;

            InventorySlot[] slots = inventory.GetAllSlots();

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (i < slots.Length)
                {
                    slotUIs[i].SetItem(slots[i].itemData, slots[i].quantity);
                }
                else
                {
                    slotUIs[i].ClearSlot();
                }
            }

            UpdateGoldDisplay();
        }

        private void UpdateGoldDisplay()
        {
            if (goldText != null && inventory != null)
            {
                goldText.text = $"金币: {inventory.gold:F0}";
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            InventorySlot slot = inventory?.GetSlot(slotIndex);

            if (slot != null && !slot.IsEmpty)
            {
                selectedItem = slot.itemData;
                ShowItemDetails(slot);
            }
            else
            {
                HideItemDetails();
                selectedItem = null;
            }
        }

        private void ShowItemDetails(InventorySlot slot)
        {
            if (detailPanel == null) return;

            detailPanel.SetActive(true);

            if (itemIcon != null && slot.itemData != null)
            {
                itemIcon.sprite = slot.itemData.icon;
            }

            if (itemName != null)
            {
                itemName.text = slot.itemData?.itemName ?? "";
            }

            if (itemDescription != null)
            {
                itemDescription.text = slot.itemData?.description ?? "";
            }

            if (itemQuantity != null)
            {
                itemQuantity.text = slot.quantity > 1 ? $"数量: {slot.quantity}" : "";
            }

            UpdateButtonStates();
        }

        private void HideItemDetails()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
            selectedItem = null;
            selectedSlotIndex = -1;
        }

        private void UpdateButtonStates()
        {
            if (useButton != null)
            {
                useButton.interactable = selectedItem != null &&
                                         selectedItem.itemType == ItemType.Consumable;
            }

            if (dropButton != null)
            {
                dropButton.interactable = selectedItem != null &&
                                         selectedItem.isDroppable;
            }
        }

        #region Event Handlers

        private void OnInventoryChanged(int usedSlots)
        {
            UpdateInventoryUI();
        }

        private void OnGoldChanged(float gold)
        {
            UpdateGoldDisplay();
        }

        private void OnGoldChangedEvent(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is GoldEventArgs data)
            {
                UpdateGoldDisplay();
            }
        }

        private void OnItemPickedUp(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is ItemPickupEventArgs)
            {
                UpdateInventoryUI();
            }
        }

        #endregion

        #region Button Handlers

        public void OnUseButtonClicked()
        {
            if (selectedItem != null && itemSystem != null)
            {
                itemSystem.UseItem(selectedItem);
                UpdateInventoryUI();
                HideItemDetails();
            }
        }

        public void OnDropButtonClicked()
        {
            if (selectedItem != null && selectedSlotIndex >= 0 && inventory != null)
            {
                if (inventory.RemoveItem(selectedItem, 1))
                {
                    // 创建掉落物
                    Vector3 dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                    itemSystem?.CreatePickup(dropPosition, selectedItem, 1);

                    UpdateInventoryUI();
                    HideItemDetails();
                }
            }
        }

        public void OnCloseButtonClicked()
        {
            HidePanel();
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
    }

    /// <summary>
    /// 背包槽位UI
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI元素")]
        public Image itemIcon;
        public Text quantityText;
        public Button slotButton;
        public Image selectionHighlight;

        private int slotIndex;
        private System.Action<int> onSlotClicked;
        private ItemData itemData;

        public void Initialize(int index, System.Action<int> onClick)
        {
            slotIndex = index;
            onSlotClicked = onClick;

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => onSlotClicked?.Invoke(slotIndex));
            }
        }

        public void SetItem(ItemData data, int quantity)
        {
            itemData = data;

            if (itemIcon != null)
            {
                itemIcon.sprite = data?.icon;
                itemIcon.enabled = data != null;
            }

            if (quantityText != null)
            {
                quantityText.text = quantity > 1 ? quantity.ToString() : "";
                quantityText.enabled = quantity > 1;
            }
        }

        public void ClearSlot()
        {
            itemData = null;

            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.enabled = false;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.enabled = selected;
            }
        }
    }
}
