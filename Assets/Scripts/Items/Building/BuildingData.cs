using System;
using UnityEngine;
using RPG.Items;

namespace RPG.Building
{
    // ──────────────────────────────────────────────────────────────────────────
    // BuildingData (ScriptableObject)
    //
    // 每种建筑类型对应一个 ScriptableObject 资产：
    //   Assets/Create → RPG/Buildings/Building
    //
    // 字段说明：
    //   buildCosts       — 建造所需的物品与数量。
    //   demolishRefund   — 拆除返还比例（0=不返还，1=全返还）。
    //   footprintSize    — 占地大小（用于碰撞检测，非强制 Grid）。
    //   buildingPrefab   — 放置成功后实例化的 GameObject。
    //   previewPrefab    — 放置预览时显示的半透明 Ghost（可选，若为 null 则用 buildingPrefab 代替）。
    // ──────────────────────────────────────────────────────────────────────────

    [CreateAssetMenu(fileName = "Building", menuName = "RPG/Buildings/Building")]
    public class BuildingData : ScriptableObject
    {
        // ── 基础信息 ──────────────────────────────────────────────────────────

        [Header("基础")]
        [Tooltip("唯一 ID，建议格式: building_wall_wood")]
        public string buildingId;

        public string buildingName;

        [TextArea(2, 5)]
        public string description;

        public Sprite icon;

        public BuildingCategory category = BuildingCategory.Utility;

        // ── 预制件 ────────────────────────────────────────────────────────────

        [Header("预制件")]
        [Tooltip("建造完成后放入场景的 Prefab。")]
        public GameObject buildingPrefab;

        [Tooltip("放置预览 Ghost（半透明）。为 null 时使用 buildingPrefab 代替。")]
        public GameObject previewPrefab;

        // ── 建造费用 ──────────────────────────────────────────────────────────

        [Header("建造费用")]
        public BuildingCost[] buildCosts = Array.Empty<BuildingCost>();

        // ── 拆除 ──────────────────────────────────────────────────────────────

        [Header("拆除")]
        [Tooltip("是否允许玩家拆除此建筑。")]
        public bool canBeDemolished = true;

        [Tooltip("拆除时返还建造材料的比例（0~1）。")]
        [Range(0f, 1f)]
        public float demolishRefundRatio = 0.5f;

        // ── 属性 ──────────────────────────────────────────────────────────────

        [Header("属性")]
        [Min(1f)]
        public float maxHealth = 100f;

        [Tooltip("是否允许旋转放置（每次按键旋转 90°）。")]
        public bool canBeRotated = true;

        [Tooltip("占地尺寸（X/Z 轴，世界单位，用于放置碰撞检测）。")]
        public Vector2 footprintSize = Vector2.one;

        // ── 放置验证 ──────────────────────────────────────────────────────────

        [Header("放置验证")]
        [Tooltip("必须落在地面层上才能放置。")]
        public bool requiresGroundContact = false;

        [Tooltip("地面有效层（requiresGroundContact=true 时使用）。")]
        public LayerMask validGroundLayers;

        [Tooltip("碰撞会阻止放置的层（通常包含 Building/Obstacle 等）。")]
        public LayerMask blockedByLayers;

        // ── 工具方法 ──────────────────────────────────────────────────────────

        /// <summary>计算拆除返还物品列表。</summary>
        public (ItemData item, int qty)[] GetDemolishRefunds()
        {
            if (buildCosts == null || buildCosts.Length == 0)
                return Array.Empty<(ItemData, int)>();

            var result = new (ItemData, int)[buildCosts.Length];
            for (int i = 0; i < buildCosts.Length; i++)
            {
                int refunded = Mathf.RoundToInt(buildCosts[i].quantity * demolishRefundRatio);
                result[i] = (buildCosts[i].item, refunded);
            }
            return result;
        }

        public bool IsValid()
            => !string.IsNullOrEmpty(buildingId) && buildingPrefab != null;
    }

    // ──────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class BuildingCost
    {
        [Tooltip("所需物品。")]
        public ItemData item;

        [Min(1)]
        public int quantity = 1;
    }

    public enum BuildingCategory
    {
        Defense,
        Production,
        Storage,
        Decoration,
        Utility,
    }
}
