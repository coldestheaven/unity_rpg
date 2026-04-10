using System;
using System.Collections.Generic;
using Framework.Core.Pools;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 加权随机掉落表（ScriptableObject）。
    ///
    /// ■ 功能：
    ///   • 多个掉落条目，每条目配置权重、数量范围、最低品质过滤。
    ///   • 支持"空手"概率 <see cref="NothingChance"/>。
    ///   • <see cref="GetRandomDrop"/> 返回单件物品（供 <see cref="ItemSystem.SpawnLoot"/> 调用）。
    ///   • <see cref="GetDrops"/> 返回一批物品（含数量），用于开宝箱、击杀精英等场景。
    ///
    /// ■ 创建: Assets → Create → RPG/Items/Loot Table
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "RPG/Items/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        // ── 内部数据结构 ──────────────────────────────────────────────────────────

        [Serializable]
        public sealed class LootEntry
        {
            [Tooltip("掉落的物品资产。")]
            public ItemData item;

            [Tooltip("掉落权重（相对值，越大概率越高）。")]
            [Min(0f)]
            public float weight = 10f;

            [Tooltip("最少掉落数量。")]
            [Min(1)]
            public int minQuantity = 1;

            [Tooltip("最多掉落数量（≥ minQuantity）。")]
            [Min(1)]
            public int maxQuantity = 1;

            [Tooltip("只有物品品质 ≥ 此值时条目才有效（作为品质门槛过滤器）。")]
            public ItemRarity minRarity = ItemRarity.Common;

            /// <summary>在 [minQuantity, maxQuantity] 范围内随机取整数数量。</summary>
            public int GetRandomQuantity()
                => minQuantity >= maxQuantity
                    ? minQuantity
                    : UnityEngine.Random.Range(minQuantity, maxQuantity + 1);

            /// <summary>条目是否有效（有物品且品质符合要求）。</summary>
            public bool IsValid()
                => item != null && item.rarity >= minRarity && weight > 0f;
        }

        // ── Inspector 字段 ────────────────────────────────────────────────────────

        [Header("掉落条目")]
        [SerializeField] private LootEntry[] _entries = Array.Empty<LootEntry>();

        [Header("掉落控制")]
        [Tooltip("不掉任何物品的概率（0 = 必掉，1 = 全不掉）。")]
        [SerializeField, Range(0f, 1f)] private float _nothingChance = 0.1f;

        [Tooltip("一次掉落调用最多掉出多少种不同物品（GetDrops 用）。")]
        [SerializeField, Min(1)] private int _maxDropCount = 1;

        [Tooltip("保证至少掉落一件（忽略 NothingChance 直到掉出至少一件）。")]
        [SerializeField] private bool _guaranteeOneItem = false;

        // ── 只读属性 ──────────────────────────────────────────────────────────────

        public float   NothingChance      => _nothingChance;
        public int     MaxDropCount       => _maxDropCount;
        public bool    GuaranteeOneItem   => _guaranteeOneItem;
        public LootEntry[] Entries        => _entries;

        // ── 权重缓存 ──────────────────────────────────────────────────────────────

        private float _cachedTotalWeight = -1f;

        private void OnValidate() => _cachedTotalWeight = -1f;

        /// <summary>所有有效条目的权重总和（延迟计算，OnValidate 时重置）。</summary>
        public float TotalWeight
        {
            get
            {
                if (_cachedTotalWeight >= 0f) return _cachedTotalWeight;
                float sum = 0f;
                if (_entries != null)
                    foreach (var e in _entries)
                        if (e != null && e.IsValid())
                            sum += e.weight;
                _cachedTotalWeight = sum;
                return sum;
            }
        }

        // ── 公开 API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 随机获取一件掉落物品（不含数量信息）。
        /// 供 <see cref="ItemSystem.SpawnLoot"/> 调用。
        /// </summary>
        /// <returns>随机抽取的 <see cref="ItemData"/>；空手时返回 <c>null</c>。</returns>
        public ItemData GetRandomDrop()
        {
            if (_entries == null || _entries.Length == 0) return null;
            if (TotalWeight <= 0f)                        return null;

            // 空手检定
            if (!_guaranteeOneItem && UnityEngine.Random.value < _nothingChance)
                return null;

            return PickOneEntry()?.item;
        }

        /// <summary>
        /// 获取一批掉落物品（含各自数量），最多 <see cref="MaxDropCount"/> 件。
        /// </summary>
        /// <returns>物品 + 数量的列表（调用方负责使用后 release ListPool 中的临时 list，
        ///          这里直接返回 new list，不依赖 ListPool，因为返回给外部)。</returns>
        public List<(ItemData item, int quantity)> GetDrops()
        {
            var result = new List<(ItemData, int)>();

            if (_entries == null || _entries.Length == 0 || TotalWeight <= 0f)
                return result;

            int attempts = 0;
            int maxAttempts = _maxDropCount * 4; // 避免死循环

            while (result.Count < _maxDropCount && attempts < maxAttempts)
            {
                attempts++;

                // 首次且保证一件时跳过空手检定
                if (result.Count == 0 && _guaranteeOneItem)
                {
                    var e = PickOneEntry();
                    if (e != null) result.Add((e.item, e.GetRandomQuantity()));
                    continue;
                }

                // 空手检定
                if (UnityEngine.Random.value < _nothingChance)
                    continue;

                var entry = PickOneEntry();
                if (entry != null)
                    result.Add((entry.item, entry.GetRandomQuantity()));
            }

            return result;
        }

        /// <summary>
        /// 判断此掉落表能否产出任何物品。
        /// </summary>
        public bool CanDrop() => TotalWeight > 0f && _nothingChance < 1f;

        // ── 私有实现 ──────────────────────────────────────────────────────────────

        /// <summary>按权重随机抽取一个有效条目。</summary>
        private LootEntry PickOneEntry()
        {
            float roll = UnityEngine.Random.Range(0f, TotalWeight);
            float cumulative = 0f;

            foreach (var e in _entries)
            {
                if (e == null || !e.IsValid()) continue;
                cumulative += e.weight;
                if (roll <= cumulative) return e;
            }

            // 兜底（浮点误差时走最后一个有效条目）
            for (int i = _entries.Length - 1; i >= 0; i--)
                if (_entries[i] != null && _entries[i].IsValid()) return _entries[i];

            return null;
        }
    }
}
