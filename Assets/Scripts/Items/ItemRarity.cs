using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 物品品质（稀有度）枚举。
    /// 整数值从低到高，可直接用于排序和比较。
    /// </summary>
    public enum ItemRarity
    {
        /// <summary>普通 — 灰色，常见基础物品。</summary>
        Common    = 0,
        /// <summary>优秀 — 绿色，略有属性加成。</summary>
        Uncommon  = 1,
        /// <summary>稀有 — 蓝色，明显属性提升。</summary>
        Rare      = 2,
        /// <summary>史诗 — 紫色，特殊效果或高属性。</summary>
        Epic      = 3,
        /// <summary>传说 — 橙色，顶级装备或唯一物品。</summary>
        Legendary = 4,
    }

    /// <summary>
    /// <see cref="ItemRarity"/> 扩展方法：颜色、显示名称、边框 Sprite 名称等。
    /// </summary>
    public static class ItemRarityExtensions
    {
        private static readonly Color[] k_Colors =
        {
            new Color(0.70f, 0.70f, 0.70f), // Common    — 灰
            new Color(0.12f, 0.85f, 0.12f), // Uncommon  — 绿
            new Color(0.18f, 0.52f, 0.98f), // Rare      — 蓝
            new Color(0.64f, 0.21f, 0.93f), // Epic      — 紫
            new Color(1.00f, 0.50f, 0.00f), // Legendary — 橙
        };

        private static readonly string[] k_DisplayNames =
        {
            "普通", "优秀", "稀有", "史诗", "传说",
        };

        private static readonly string[] k_HexColors =
        {
            "#B3B3B3", "#1FD91F", "#2F85FB", "#A336EE", "#FF8000",
        };

        /// <summary>返回品质对应的 UI 显示颜色。</summary>
        public static Color GetColor(this ItemRarity rarity)
            => k_Colors[Mathf.Clamp((int)rarity, 0, k_Colors.Length - 1)];

        /// <summary>返回品质的中文显示名称。</summary>
        public static string GetDisplayName(this ItemRarity rarity)
            => k_DisplayNames[Mathf.Clamp((int)rarity, 0, k_DisplayNames.Length - 1)];

        /// <summary>返回带品质颜色的物品名 Rich Text 字符串（用于 UI Text 组件）。</summary>
        public static string ColoredName(this ItemRarity rarity, string itemName)
        {
            string hex = k_HexColors[Mathf.Clamp((int)rarity, 0, k_HexColors.Length - 1)];
            return $"<color={hex}>{itemName}</color>";
        }

        /// <summary>
        /// 尝试从字符串解析 <see cref="ItemRarity"/>（大小写不敏感）。
        /// </summary>
        public static bool TryParse(string value, out ItemRarity rarity)
            => System.Enum.TryParse(value, ignoreCase: true, out rarity);
    }
}
