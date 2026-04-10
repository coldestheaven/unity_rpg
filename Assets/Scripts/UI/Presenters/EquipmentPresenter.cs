using System;
using RPG.Items;

namespace UI.Presenters
{
    // ── 视图数据结构 ──────────────────────────────────────────────────────────────

    /// <summary>装备槽位的视图数据（只读结构体，零 GC 传递）。</summary>
    public readonly struct EquipmentSlotViewData
    {
        public readonly EquipmentSlot Slot;
        public readonly bool          IsEmpty;
        public readonly UnityEngine.Sprite Icon;
        public readonly string        ItemName;
        public readonly string        Description;
        public readonly string        StatsText;
        public readonly ItemRarity    Rarity;

        public EquipmentSlotViewData(
            EquipmentSlot slot,
            bool          isEmpty,
            UnityEngine.Sprite icon,
            string        itemName,
            string        description,
            string        statsText,
            ItemRarity    rarity)
        {
            Slot        = slot;
            IsEmpty     = isEmpty;
            Icon        = icon;
            ItemName    = itemName;
            Description = description;
            StatsText   = statsText;
            Rarity      = rarity;
        }
    }

    // ── EquipmentPresenter ────────────────────────────────────────────────────────

    /// <summary>
    /// 装备面板 Presenter（MVP 模式）。
    ///
    /// 监听 <see cref="EquipmentSystem"/> 的装备变更事件，
    /// 将所有槽位数据转换为视图结构体并发布给 UI Controller。
    ///
    /// ■ 职责：
    ///   • 绑定 / 解绑 EquipmentSystem
    ///   • 构建每个装备槽的 <see cref="EquipmentSlotViewData"/>
    ///   • 当前选中槽的追踪
    ///
    /// ■ 不关心：UI 的具体实现（UGUI / UIToolkit 均可）。
    /// </summary>
    public sealed class EquipmentPresenter
    {
        private EquipmentSystem _equipment;
        private EquipmentSlot   _selectedSlot;
        private bool            _hasSelection;

        // ── 事件 ──────────────────────────────────────────────────────────────────

        /// <summary>任意装备槽发生变化时触发（所有槽位的视图数据）。</summary>
        public event Action<EquipmentSlotViewData[]> SlotsChanged;

        /// <summary>当前选中槽的详情变化时触发。</summary>
        public event Action<EquipmentSlotViewData>   SelectionChanged;

        // ── 绑定 ──────────────────────────────────────────────────────────────────

        /// <summary>绑定到指定的 <see cref="EquipmentSystem"/>，并立即刷新。</summary>
        public void Bind(EquipmentSystem equipmentSystem)
        {
            if (ReferenceEquals(_equipment, equipmentSystem))
            {
                Refresh();
                return;
            }

            Unbind();
            _equipment = equipmentSystem;

            if (_equipment != null)
            {
                _equipment.OnEquipmentChanged    += HandleEquipmentChanged;
                _equipment.OnEquipmentUnequipped += HandleEquipmentUnequipped;
                Refresh();
            }
            else
            {
                PublishEmpty();
            }
        }

        /// <summary>解除绑定并清空视图数据。</summary>
        public void Unbind()
        {
            if (_equipment != null)
            {
                _equipment.OnEquipmentChanged    -= HandleEquipmentChanged;
                _equipment.OnEquipmentUnequipped -= HandleEquipmentUnequipped;
                _equipment = null;
            }
            _hasSelection = false;
            PublishEmpty();
        }

        // ── 操作 ──────────────────────────────────────────────────────────────────

        /// <summary>重新发布所有槽位视图数据。</summary>
        public void Refresh() => PublishAllSlots();

        /// <summary>设置当前选中的槽位（用于 UI 高亮和详情显示）。</summary>
        public void SelectSlot(EquipmentSlot slot)
        {
            _selectedSlot = slot;
            _hasSelection = true;
            PublishSelection();
        }

        /// <summary>取消选中。</summary>
        public void ClearSelection()
        {
            _hasSelection = false;
            SelectionChanged?.Invoke(BuildEmptySlotData(EquipmentSlot.MainHand));
        }

        // ── 私有实现 ──────────────────────────────────────────────────────────────

        private void HandleEquipmentChanged(EquipmentSlot slot, EquipmentData _)
        {
            PublishAllSlots();
            if (_hasSelection && _selectedSlot == slot)
                PublishSelection();
        }

        private void HandleEquipmentUnequipped(EquipmentSlot slot)
        {
            PublishAllSlots();
            if (_hasSelection && _selectedSlot == slot)
                ClearSelection();
        }

        private void PublishAllSlots()
        {
            if (_equipment == null) { PublishEmpty(); return; }

            var slots = System.Enum.GetValues(typeof(EquipmentSlot));
            var data  = new EquipmentSlotViewData[slots.Length];

            int i = 0;
            foreach (EquipmentSlot slot in slots)
                data[i++] = BuildSlotData(slot, _equipment[slot]);

            SlotsChanged?.Invoke(data);
        }

        private void PublishSelection()
        {
            if (!_hasSelection || _equipment == null)
            {
                SelectionChanged?.Invoke(BuildEmptySlotData(EquipmentSlot.MainHand));
                return;
            }
            SelectionChanged?.Invoke(BuildSlotData(_selectedSlot, _equipment[_selectedSlot]));
        }

        private void PublishEmpty()
        {
            SlotsChanged?.Invoke(System.Array.Empty<EquipmentSlotViewData>());
            SelectionChanged?.Invoke(BuildEmptySlotData(EquipmentSlot.MainHand));
        }

        private static EquipmentSlotViewData BuildSlotData(EquipmentSlot slot, EquipmentData item)
        {
            if (item == null) return BuildEmptySlotData(slot);

            string stats = BuildStatsText(item);
            return new EquipmentSlotViewData(
                slot,
                false,
                item.icon,
                item.itemName,
                item.description,
                stats,
                item.rarity);
        }

        private static EquipmentSlotViewData BuildEmptySlotData(EquipmentSlot slot)
            => new EquipmentSlotViewData(slot, true, null,
                string.Empty, string.Empty, string.Empty, ItemRarity.Common);

        private static string BuildStatsText(EquipmentData item)
        {
            if (item == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            if (item.attackPowerBonus != 0) sb.AppendLine($"攻击力: +{item.attackPowerBonus}");
            if (item.defenseBonus     != 0) sb.AppendLine($"防御力: +{item.defenseBonus}");
            if (item.healthBonus      != 0) sb.AppendLine($"生命值: +{item.healthBonus}");
            if (item.manaBonus        != 0) sb.AppendLine($"法力值: +{item.manaBonus}");
            if (item.moveSpeedBonus   != 0) sb.AppendLine($"移动速度: +{item.moveSpeedBonus:F2}");

            if (item is WeaponData weapon)
            {
                sb.AppendLine($"基础伤害: {weapon.baseDamage}");
                if (weapon.attackRange > 0) sb.AppendLine($"攻击范围: {weapon.attackRange:F1}");
            }
            else if (item is ArmorData armor)
            {
                sb.AppendLine($"基础防御: {armor.baseDefense}");
                if (armor.damageReductionPercentage > 0)
                    sb.AppendLine($"减伤: {armor.damageReductionPercentage:P0}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
