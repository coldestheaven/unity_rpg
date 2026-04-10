using System;
using System.Collections.Generic;
using Framework.Core.Pools;
using Framework.Events;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 商店系统（MonoBehaviour）。
    ///
    /// ■ 功能：
    ///   • 管理商品列表（<see cref="ShopEntry"/>），支持无限库存或有限库存。
    ///   • 玩家购买：扣除金币 + 添加物品至玩家背包。
    ///   • 玩家出售：从背包移除物品 + 按比例获得金币。
    ///   • 通过 EventBus 发布 <see cref="ItemBoughtEvent"/> / <see cref="ItemSoldEvent"/>。
    ///
    /// ■ 使用方式：
    ///   将此组件挂载到 NPC / 商店 Trigger 的 GameObject。
    ///   玩家进入交互范围时，UI 调用 <see cref="GetAvailableEntries"/> 渲染商品列表。
    ///   点击购买/出售时调用 <see cref="Buy"/> / <see cref="Sell"/>。
    /// </summary>
    public sealed class ShopSystem : MonoBehaviour
    {
        // ── 数据结构 ──────────────────────────────────────────────────────────────

        [Serializable]
        public sealed class ShopEntry
        {
            [Tooltip("销售的物品资产。")]
            public ItemData item;

            [Tooltip("购买价格（-1 = 使用物品自身 value 字段）。")]
            public int buyPrice = -1;

            [Tooltip("库存数量（-1 = 无限补货；≥ 0 = 有限数量）。")]
            public int stock = -1;

            [Tooltip("每次进货补充的数量（与 restockIntervalSec 配合使用）。")]
            [Min(1)]
            public int restockAmount = 1;

            [HideInInspector]
            public int currentStock; // 运行时当前库存

            /// <summary>返回该条目实际的购买单价。</summary>
            public int GetBuyPrice() => buyPrice >= 0 ? buyPrice : (item?.value ?? 0);

            /// <summary>该商品是否当前可购买。</summary>
            public bool IsAvailable => item != null && (stock < 0 || currentStock > 0);
        }

        // ── Inspector 字段 ────────────────────────────────────────────────────────

        [Header("商店信息")]
        [SerializeField] private string _shopName = "商店";

        [Header("商品列表")]
        [SerializeField] private ShopEntry[] _entries = Array.Empty<ShopEntry>();

        [Header("出售设置")]
        [Tooltip("玩家出售物品时获得的金币比例（0.5 = 物品价值的 50%）。")]
        [SerializeField, Range(0f, 1f)] private float _sellRatio = 0.5f;

        [Tooltip("是否接受玩家出售物品（false = 纯买店）。")]
        [SerializeField] private bool _acceptSell = true;

        [Header("自动补货")]
        [Tooltip("每隔多少秒为有限库存商品补货（0 = 不自动补货）。")]
        [SerializeField, Min(0f)] private float _restockIntervalSec = 0f;

        // ── 只读属性 ──────────────────────────────────────────────────────────────

        public string ShopName   => _shopName;
        public bool   AcceptSell => _acceptSell;
        public float  SellRatio  => _sellRatio;

        // ── 事件 ──────────────────────────────────────────────────────────────────

        /// <summary>库存或价格变化时触发，UI 监听此事件刷新显示。</summary>
        public event Action OnShopChanged;

        // ── 生命周期 ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            foreach (var e in _entries)
                if (e != null) e.currentStock = e.stock;
        }

        private void Start()
        {
            if (_restockIntervalSec > 0f)
                InvokeRepeating(nameof(RestockAll), _restockIntervalSec, _restockIntervalSec);
        }

        // ── 公开 API ──────────────────────────────────────────────────────────────

        /// <summary>获取所有当前可购买的商品条目。</summary>
        public ShopEntry[] GetAvailableEntries()
        {
            using (ListPool<ShopEntry>.Rent(out var tmp))
            {
                foreach (var e in _entries)
                    if (e != null && e.IsAvailable) tmp.Add(e);
                return tmp.ToArray();
            }
        }

        /// <summary>获取全部商品条目（含无货商品，用于 UI 完整展示）。</summary>
        public ShopEntry[] GetAllEntries() => _entries;

        /// <summary>
        /// 玩家购买商品。
        /// </summary>
        /// <param name="entry">要购买的商品条目（来自 <see cref="GetAvailableEntries"/>）。</param>
        /// <param name="buyer">玩家的 <see cref="InventorySystem"/>。</param>
        /// <param name="quantity">购买数量（≥ 1）。</param>
        /// <returns>购买是否成功。</returns>
        public bool Buy(ShopEntry entry, InventorySystem buyer, int quantity = 1)
        {
            if (!CanBuy(entry, buyer, quantity, out string reason))
            {
                Debug.LogWarning($"[ShopSystem] 购买失败（{_shopName}）: {reason}");
                return false;
            }

            // 添加物品到背包
            InventoryOperationResult result = buyer.AddItem(entry.item, quantity);
            if (result != InventoryOperationResult.Success)
            {
                Debug.LogWarning($"[ShopSystem] 背包无法容纳 {entry.item.itemName}: {result}");
                return false;
            }

            // 扣除金币
            int totalCost = entry.GetBuyPrice() * quantity;
            buyer.RemoveGold(totalCost);

            // 扣减库存
            if (entry.stock >= 0)
                entry.currentStock -= quantity;

            string itemId = string.IsNullOrEmpty(entry.item.itemId) ? entry.item.name : entry.item.itemId;
            EventBus.Publish(new ItemBoughtEvent(itemId, entry.item.itemName, quantity, totalCost));
            OnShopChanged?.Invoke();

            Debug.Log($"[ShopSystem] 购买: {entry.item.itemName} ×{quantity}，花费 {totalCost} 金币");
            return true;
        }

        /// <summary>
        /// 玩家出售物品。
        /// </summary>
        /// <param name="item">要出售的物品。</param>
        /// <param name="seller">玩家的 <see cref="InventorySystem"/>。</param>
        /// <param name="quantity">出售数量（≥ 1）。</param>
        public bool Sell(ItemData item, InventorySystem seller, int quantity = 1)
        {
            if (!_acceptSell)
            {
                Debug.LogWarning($"[ShopSystem] {_shopName} 不收购物品。");
                return false;
            }
            if (item == null || !item.isSellable)
            {
                Debug.LogWarning($"[ShopSystem] 物品不可出售: {item?.itemName ?? "null"}");
                return false;
            }
            if (seller == null || !seller.HasItem(item, quantity))
            {
                Debug.LogWarning($"[ShopSystem] 背包中没有足够的 {item?.itemName}");
                return false;
            }

            InventoryOperationResult result = seller.RemoveItem(item, quantity);
            if (result != InventoryOperationResult.Success) return false;

            int totalGold = GetSellPrice(item) * quantity;
            seller.AddGold(totalGold);

            string itemId = string.IsNullOrEmpty(item.itemId) ? item.name : item.itemId;
            EventBus.Publish(new ItemSoldEvent(itemId, item.itemName, quantity, totalGold));
            OnShopChanged?.Invoke();

            Debug.Log($"[ShopSystem] 出售: {item.itemName} ×{quantity}，获得 {totalGold} 金币");
            return true;
        }

        /// <summary>计算物品的出售价格（单件）。</summary>
        public int GetSellPrice(ItemData item)
        {
            if (item == null || !item.isSellable) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(item.value * _sellRatio));
        }

        /// <summary>检查购买条件并返回失败原因（用于 UI 禁用提示）。</summary>
        public bool CanBuy(ShopEntry entry, InventorySystem buyer, int quantity,
            out string failReason)
        {
            failReason = string.Empty;
            if (entry == null || entry.item == null) { failReason = "无效商品";         return false; }
            if (quantity <= 0)                       { failReason = "数量不合法";       return false; }
            if (!entry.IsAvailable)                  { failReason = "库存不足";         return false; }
            if (entry.stock >= 0 && entry.currentStock < quantity)
            {
                failReason = $"库存不足（剩余 {entry.currentStock}）";
                return false;
            }
            if (buyer == null)                       { failReason = "背包系统无效";     return false; }
            if (buyer.Gold < entry.GetBuyPrice() * quantity)
            {
                failReason = $"金币不足（需 {entry.GetBuyPrice() * quantity}，拥有 {buyer.Gold:F0}）";
                return false;
            }
            return true;
        }

        // ── 补货 ──────────────────────────────────────────────────────────────────

        /// <summary>为所有有限库存商品执行一次补货。</summary>
        public void RestockAll()
        {
            bool changed = false;
            foreach (var e in _entries)
            {
                if (e == null || e.stock < 0) continue; // 无限库存不需要补货
                if (e.currentStock >= e.stock) continue; // 已满库存
                e.currentStock = Mathf.Min(e.currentStock + e.restockAmount, e.stock);
                changed = true;
            }
            if (changed) OnShopChanged?.Invoke();
        }

        /// <summary>重置所有库存为初始值（换天/进入新场景时调用）。</summary>
        public void ResetStock()
        {
            foreach (var e in _entries)
                if (e != null) e.currentStock = e.stock;
            OnShopChanged?.Invoke();
        }
    }
}
