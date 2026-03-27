using UnityEngine;
using UnityEngine.UI;
using UI.Presenters;

namespace UI.Views
{
    /// <summary>
    /// 物品槽UI组件
    /// </summary>
    public class ItemSlot : Framework.Base.MonoBehaviourBase
    {
        [Header("Visual")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Image background;
        [SerializeField] private Image highlight;

        [Header("Settings")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;

        private int slotIndex;
        private bool isEmpty = true;

        public bool IsEmpty => isEmpty;

        public event System.Action<int> OnSlotClicked;

        public void Setup(InventorySlotViewData viewData)
        {
            slotIndex = viewData.SlotIndex;
            isEmpty = viewData.IsEmpty;

            UpdateVisuals(viewData);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedColor : defaultColor;
            }

            if (highlight != null)
            {
                highlight.enabled = selected;
            }
        }

        private void UpdateVisuals(InventorySlotViewData viewData)
        {
            if (isEmpty)
            {
                if (icon != null)
                {
                    icon.enabled = false;
                }
                if (amountText != null)
                {
                    amountText.text = "";
                }
            }
            else
            {
                if (icon != null)
                {
                    icon.enabled = viewData.Icon != null;
                    icon.sprite = viewData.Icon;
                }
                if (amountText != null)
                {
                    amountText.text = viewData.AmountText;
                }
            }
        }

        public void OnClick()
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
    }
}
