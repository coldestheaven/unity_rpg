using UnityEngine;
using UnityEngine.UI;

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

        private string itemId;
        private int amount;
        private bool isEmpty = true;

        public string ItemId => itemId;
        public int Amount => amount;
        public bool IsEmpty => isEmpty;

        public event System.Action OnSlotClicked;

        public void Setup(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
            this.isEmpty = string.IsNullOrEmpty(itemId) || amount <= 0;

            UpdateVisuals();
        }

        public void SetAmount(int newAmount)
        {
            amount = newAmount;
            isEmpty = amount <= 0;
            UpdateVisuals();
        }

        public void Clear()
        {
            itemId = null;
            amount = 0;
            isEmpty = true;
            UpdateVisuals();
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

        private void UpdateVisuals()
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
                    icon.enabled = true;
                    // Load icon sprite from DataManager
                }
                if (amountText != null)
                {
                    amountText.text = amount > 1 ? amount.ToString() : "";
                }
            }
        }

        public void OnClick()
        {
            OnSlotClicked?.Invoke();
        }
    }
}
