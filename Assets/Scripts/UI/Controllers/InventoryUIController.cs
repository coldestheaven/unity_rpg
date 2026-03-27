using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RPG.Items;
using UI.Presenters;
using UI.Views;

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

        private InventorySystem inventory;
        private readonly List<GameObject> slotObjects = new List<GameObject>();
        private readonly List<ItemSlot> slotViews = new List<ItemSlot>();
        private InventoryPresenter presenter;

        protected override void Initialize()
        {
            base.Initialize();
            presenter = new InventoryPresenter();
            presenter.SlotsChanged += HandleSlotsChanged;
            presenter.DetailsChanged += HandleDetailsChanged;

            inventory = ResolveInventory();
            presenter.Bind(inventory);
        }

        private void OnDestroy()
        {
            if (presenter != null)
            {
                presenter.SlotsChanged -= HandleSlotsChanged;
                presenter.DetailsChanged -= HandleDetailsChanged;
                presenter.Unbind();
            }
        }

        public void RefreshInventory()
        {
            presenter?.Refresh();
        }

        private void ClearSlots()
        {
            foreach (var slotObject in slotObjects)
            {
                if (slotObject != null)
                {
                    Destroy(slotObject);
                }
            }

            slotObjects.Clear();
            slotViews.Clear();
        }

        private void HandleSlotsChanged(InventorySlotViewData[] slots)
        {
            ClearSlots();

            if (slots == null || slotPrefab == null || slotsContainer == null)
            {
                return;
            }

            foreach (var slot in slots)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                slotObjects.Add(slotObj);
                ItemSlot itemSlot = slotObj.GetComponent<ItemSlot>();
                if (itemSlot != null)
                {
                    itemSlot.Setup(slot);
                    itemSlot.OnSlotClicked += HandleSlotClicked;
                    slotViews.Add(itemSlot);
                    continue;
                }

                InventorySlotUI legacySlotView = slotObj.GetComponent<InventorySlotUI>();
                if (legacySlotView != null)
                {
                    legacySlotView.Setup(slot);
                    legacySlotView.OnSlotClicked += HandleSlotClicked;
                }
            }
        }

        private void HandleSlotClicked(int slotIndex)
        {
            presenter?.SelectSlot(slotIndex);
        }

        private void HandleDetailsChanged(InventoryDetailsViewData details)
        {
            if (itemDetailsPanel != null)
            {
                itemDetailsPanel.SetActive(details.Visible);
            }

            if (!details.Visible)
            {
                if (itemIcon != null)
                {
                    itemIcon.enabled = false;
                    itemIcon.sprite = null;
                }

                if (itemName != null)
                {
                    itemName.text = string.Empty;
                }

                if (itemDescription != null)
                {
                    itemDescription.text = string.Empty;
                }

                return;
            }

            if (itemIcon != null)
            {
                itemIcon.enabled = details.Icon != null;
                itemIcon.sprite = details.Icon;
            }

            if (itemName != null)
            {
                itemName.text = details.ItemName;
            }

            if (itemDescription != null)
            {
                itemDescription.text = details.Description;
            }
        }

        private InventorySystem ResolveInventory()
        {
            if (ItemSystem.Instance != null && ItemSystem.Instance.inventory != null)
            {
                return ItemSystem.Instance.inventory;
            }

            return FindAnyObjectByType<InventorySystem>();
        }
    }

    public class InventorySlotUI : MonoBehaviour
    {
        public System.Action<int> OnSlotClicked;

        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button button;

        private int slotIndex;

        public void Setup(InventorySlotViewData slot)
        {
            slotIndex = slot.SlotIndex;

            if (slot.IsEmpty)
            {
                if (icon != null) icon.enabled = false;
                if (amountText != null) amountText.text = "";
            }
            else
            {
                if (icon != null)
                {
                    icon.enabled = slot.Icon != null;
                    icon.sprite = slot.Icon;
                }

                if (amountText != null)
                {
                    amountText.text = slot.AmountText;
                }
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSlotClicked?.Invoke(slotIndex));
            }
        }
    }
}
