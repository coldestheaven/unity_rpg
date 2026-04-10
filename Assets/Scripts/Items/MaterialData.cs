using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// 材料类物品数据（ScriptableObject）。
    ///
    /// 用于合成配方、任务收集、强化消耗等场景。
    /// 继承自 <see cref="ItemData"/>，在其基础上新增材料分类与品阶信息。
    ///
    /// ■ 创建: Assets → Create → RPG/Items/Material
    /// </summary>
    [CreateAssetMenu(fileName = "New Material", menuName = "RPG/Items/Material")]
    public sealed class MaterialData : ItemData
    {
        // ── 材料分类 ──────────────────────────────────────────────────────────────

        /// <summary>材料大类枚举，用于合成界面分类展示与配方过滤。</summary>
        public enum MaterialCategory
        {
            /// <summary>矿石（铁矿石、魔铁锭等）。</summary>
            Ore,
            /// <summary>木材（橡木板、魔法木等）。</summary>
            Wood,
            /// <summary>草药（治愈草、毒蘑菇等）。</summary>
            Herb,
            /// <summary>皮革（兽皮、龙皮等）。</summary>
            Leather,
            /// <summary>宝石（红宝石、魔法水晶等）。</summary>
            Gem,
            /// <summary>布料（棉布、丝绸等）。</summary>
            Cloth,
            /// <summary>炼金材料（魔法粉末、龙血等）。</summary>
            Alchemical,
            /// <summary>通用（不属于以上分类）。</summary>
            Misc,
        }

        // ── Inspector 字段 ────────────────────────────────────────────────────────

        [Header("材料属性")]
        [Tooltip("材料分类，用于合成界面与配方搜索。")]
        public MaterialCategory category = MaterialCategory.Misc;

        [Tooltip("品阶（1~5）。1 = 基础材料，5 = 顶级材料，影响合成配方的难度分级。")]
        [Range(1, 5)]
        public int craftingTier = 1;

        [Tooltip("每次精炼/合成时，此材料作为消耗品能提供的"品质点"数值。")]
        [Min(0)]
        public int qualityPoints = 1;

        [Tooltip("是否可在炼金台中使用（消耗品合成原料）。")]
        public bool isAlchemyIngredient = false;

        [Tooltip("是否可在铁匠铺强化装备时作为消耗品。")]
        public bool isUpgradeMaterial = false;

        // ── 重写方法 ──────────────────────────────────────────────────────────────

        public override bool CanStack() => true; // 材料默认可堆叠

        public override void Use(UnityEngine.GameObject user)
        {
            // 材料一般不能直接"使用"，需通过合成界面消耗
            UnityEngine.Debug.Log($"[MaterialData] {itemName} 是合成材料，无法直接使用。");
        }
    }
}
