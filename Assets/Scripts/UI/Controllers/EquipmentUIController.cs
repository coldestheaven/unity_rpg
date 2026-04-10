using System.Collections.Generic;
using RPG.Items;
using UI.Presenters;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Controllers
{
    /// <summary>
    /// 装备面板 UI 控制器。
    ///
    /// ■ 职责：
    ///   • 为每个 <see cref="EquipmentSlot"/> 维护一个槽位图标 UI 对象。
    ///   • 监听 <see cref="EquipmentPresenter"/> 的数据变化并刷新视图。
    ///   • 点击槽位 → 通知 Presenter 选中 → 更新详情面板。
    ///   • 点击"卸装"按钮 → 调用 <see cref="ItemSystem.UnequipItem"/>。
    ///
    /// ■ 设置（Inspector）：
    ///   1. 将 Equipment Panel 下的每个槽位图标 Image 拖入 <see cref="_slotIcons"/> 列表，
    ///      按 <see cref="EquipmentSlot"/> 枚举顺序对应。
    ///   2. 可选：设置选中高亮 / 详情面板引用。
    /// </summary>
    public sealed class EquipmentUIController : UIPanelBase
    {
        // ── Inspector 字段 ────────────────────────────────────────────────────────

        [Header("装备槽图标（按 EquipmentSlot 枚举顺序）")]
        [Tooltip("与 EquipmentSlot 枚举一一对应（MainHand=0, OffHand=1, ...）。")]
        [SerializeField] private List<EquipmentSlotIcon> _slotIcons = new List<EquipmentSlotIcon>();

        [Header("详情面板")]
        [SerializeField] private GameObject   _detailsPanel;
        [SerializeField] private Image        _detailIcon;
        [SerializeField] private Text         _detailName;
        [SerializeField] private Text         _detailDescription;
        [SerializeField] private Text         _detailStats;
        [SerializeField] private Button       _unequipButton;

        // ── 运行时 ────────────────────────────────────────────────────────────────

        private EquipmentPresenter _presenter;
        private EquipmentSlot      _selectedSlot;
        private bool               _hasSelection;

        // ── 生命周期 ──────────────────────────────────────────────────────────────

        protected override void Initialize()
        {
            base.Initialize();

            _presenter = new EquipmentPresenter();
            _presenter.SlotsChanged     += HandleSlotsChanged;
            _presenter.SelectionChanged += HandleSelectionChanged;

            var equipment = ResolveEquipmentSystem();
            _presenter.Bind(equipment);

            if (_unequipButton != null)
                _unequipButton.onClick.AddListener(OnUnequipClicked);

            HideDetails();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.SlotsChanged     -= HandleSlotsChanged;
                _presenter.SelectionChanged -= HandleSelectionChanged;
                _presenter.Unbind();
            }
        }

        // ── 数据回调 ──────────────────────────────────────────────────────────────

        private void HandleSlotsChanged(EquipmentSlotViewData[] slots)
        {
            // 先把所有图标恢复为空
            foreach (var icon in _slotIcons)
                icon?.SetEmpty();

            if (slots == null) return;

            foreach (var data in slots)
            {
                var icon = GetIconForSlot(data.Slot);
                if (icon == null) continue;

                if (data.IsEmpty)
                    icon.SetEmpty();
                else
                    icon.SetItem(data.Icon, data.Rarity, data.Slot);
            }
        }

        private void HandleSelectionChanged(EquipmentSlotViewData data)
        {
            if (data.IsEmpty)
            {
                HideDetails();
                return;
            }

            ShowDetails(data);
        }

        // ── 点击处理 ──────────────────────────────────────────────────────────────

        /// <summary>由各 EquipmentSlotIcon 在点击时调用。</summary>
        public void OnSlotClicked(EquipmentSlot slot)
        {
            _selectedSlot = slot;
            _hasSelection = true;
            _presenter?.SelectSlot(slot);

            // 更新高亮
            foreach (var icon in _slotIcons)
                icon?.SetSelected(icon.Slot == slot);
        }

        private void OnUnequipClicked()
        {
            if (!_hasSelection) return;
            ItemSystem.Instance?.UnequipItem(_selectedSlot);
            _hasSelection = false;
            HideDetails();
        }

        // ── 详情面板 ──────────────────────────────────────────────────────────────

        private void ShowDetails(EquipmentSlotViewData data)
        {
            if (_detailsPanel != null) _detailsPanel.SetActive(true);

            if (_detailIcon != null)
            {
                _detailIcon.enabled = data.Icon != null;
                _detailIcon.sprite  = data.Icon;
                if (data.Icon != null)
                    _detailIcon.color = data.Rarity.GetColor();
            }

            if (_detailName != null)
                _detailName.text = data.Rarity.ColoredName(data.ItemName);

            if (_detailDescription != null)
                _detailDescription.text = data.Description;

            if (_detailStats != null)
                _detailStats.text = data.StatsText;

            if (_unequipButton != null)
                _unequipButton.gameObject.SetActive(true);
        }

        private void HideDetails()
        {
            if (_detailsPanel != null) _detailsPanel.SetActive(false);
            if (_unequipButton != null) _unequipButton.gameObject.SetActive(false);
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────────

        private EquipmentSlotIcon GetIconForSlot(EquipmentSlot slot)
        {
            int idx = (int)slot;
            return (idx >= 0 && idx < _slotIcons.Count) ? _slotIcons[idx] : null;
        }

        private static EquipmentSystem ResolveEquipmentSystem()
        {
            if (ItemSystem.Instance != null && ItemSystem.Instance.equipment != null)
                return ItemSystem.Instance.equipment;
            return FindAnyObjectByType<EquipmentSystem>();
        }
    }

    // ── 子组件：单个装备槽图标 ───────────────────────────────────────────────────

    /// <summary>
    /// 装备槽图标组件，挂载在每个槽位的 UI GameObject 上。
    /// 绑定 Button，点击时通知 <see cref="EquipmentUIController"/>。
    /// </summary>
    [System.Serializable]
    public sealed class EquipmentSlotIcon : MonoBehaviour
    {
        [SerializeField] private EquipmentSlot _slot;
        [SerializeField] private Image         _icon;
        [SerializeField] private Image         _background;
        [SerializeField] private Image         _rarityBorder;
        [SerializeField] private Color         _defaultBg    = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color         _selectedBg   = new Color(0.4f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color         _emptyIconColor = new Color(1f, 1f, 1f, 0.2f);

        [Header("绑定控制器（运行时自动查找）")]
        [SerializeField] private EquipmentUIController _controller;

        public EquipmentSlot Slot => _slot;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponentInParent<EquipmentUIController>();

            var btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => _controller?.OnSlotClicked(_slot));
        }

        public void SetEmpty()
        {
            if (_icon != null)
            {
                _icon.enabled = false;
                _icon.color   = _emptyIconColor;
            }
            if (_rarityBorder != null) _rarityBorder.enabled = false;
            SetBackground(_defaultBg);
        }

        public void SetItem(Sprite icon, ItemRarity rarity, EquipmentSlot slot)
        {
            if (_icon != null)
            {
                _icon.enabled = true;
                _icon.sprite  = icon;
                _icon.color   = Color.white;
            }
            if (_rarityBorder != null)
            {
                _rarityBorder.enabled = true;
                _rarityBorder.color   = rarity.GetColor();
            }
            SetBackground(_defaultBg);
        }

        public void SetSelected(bool selected)
            => SetBackground(selected ? _selectedBg : _defaultBg);

        private void SetBackground(Color color)
        {
            if (_background != null) _background.color = color;
        }
    }
}
