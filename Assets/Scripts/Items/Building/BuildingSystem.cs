using System;
using System.Collections.Generic;
using UnityEngine;
using Framework.Events;
using RPG.Core;
using RPG.Data;
using RPG.Items;

namespace RPG.Building
{
    // ──────────────────────────────────────────────────────────────────────────
    // BuildingSystem
    //
    // 职责：
    //   • 管理所有已放置建筑（字典按 InstanceId 索引）。
    //   • 检查建造费用并从背包扣除/返还物品。
    //   • 提供 PlaceBuilding / DemolishBuilding 入口，并发布相应事件。
    //   • 支持存档：ClearAllBuildings / RestoreFromDTOs 与 SaveSystem 对接。
    //
    // 挂载要求：
    //   • 单例，DontDestroyOnLoad。
    //   • Inspector 需要引用 BuildingDatabase（或运行时通过 SetDatabase 注入）。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>建造系统核心管理器。</summary>
    public sealed class BuildingSystem : Singleton<BuildingSystem>
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("数据库")]
        [SerializeField] private BuildingDatabase _database;

        [Header("父节点（可选，统一整理层级）")]
        [SerializeField] private Transform _buildingRoot;

        // ── 运行时状态 ────────────────────────────────────────────────────────

        private readonly Dictionary<string, PlacedBuilding> _placed
            = new Dictionary<string, PlacedBuilding>(32);

        private int _instanceCounter; // 自增 ID 种子

        // ── 公开属性 ──────────────────────────────────────────────────────────

        public int BuildingCount => _placed.Count;

        // ── 数据库注入 ────────────────────────────────────────────────────────

        /// <summary>运行时注入数据库（替代 Inspector 赋值）。</summary>
        public void SetDatabase(BuildingDatabase db) => _database = db;

        // ── 查询 ──────────────────────────────────────────────────────────────

        public PlacedBuilding GetBuilding(string instanceId)
        {
            _placed.TryGetValue(instanceId, out var b);
            return b;
        }

        public PlacedBuilding[] GetAllBuildings()
        {
            var arr = new PlacedBuilding[_placed.Count];
            _placed.Values.CopyTo(arr, 0);
            return arr;
        }

        public BuildingData GetBuildingData(string buildingId)
            => _database != null ? _database.GetById(buildingId) : null;

        // ── 建造可行性 ────────────────────────────────────────────────────────

        /// <summary>检查玩家背包是否拥有足够的建造材料。</summary>
        public bool CanAfford(BuildingData data)
        {
            if (data == null || data.buildCosts == null) return true;
            var inv = ItemSystem.Instance?.inventory;
            if (inv == null) return false;

            foreach (var cost in data.buildCosts)
            {
                if (cost.item == null) continue;
                if (inv.GetItemCount(cost.item) < cost.quantity)
                    return false;
            }
            return true;
        }

        // ── 放置 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 在指定位置放置建筑。
        /// </summary>
        /// <returns>成功时返回 <see cref="PlacedBuilding"/>，否则 null。</returns>
        public PlacedBuilding PlaceBuilding(BuildingData data,
                                            Vector3      position,
                                            Quaternion   rotation)
        {
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning("[BuildingSystem] BuildingData 无效，放置失败。");
                return null;
            }

            if (!CanAfford(data))
            {
                Debug.LogWarning($"[BuildingSystem] 材料不足，无法建造 {data.buildingName}。");
                return null;
            }

            // 扣除材料
            DeductCosts(data);

            // 实例化 Prefab
            var root = _buildingRoot != null ? _buildingRoot : transform;
            var go   = Instantiate(data.buildingPrefab, position, rotation, root);

            // 获取或添加 PlacedBuilding 组件
            var pb = go.GetComponent<PlacedBuilding>() ?? go.AddComponent<PlacedBuilding>();
            var id = GenerateInstanceId();
            pb.Initialize(data, id);

            _placed[id] = pb;

            EventBus.Publish(new BuildingPlacedEvent(data.buildingId, id, position));
            Debug.Log($"[BuildingSystem] 放置建筑 '{data.buildingName}' (id={id})。");

            return pb;
        }

        // ── 拆除 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 拆除指定实例 ID 的建筑。
        /// </summary>
        /// <param name="instanceId">PlacedBuilding.InstanceId。</param>
        /// <param name="giveRefunds">是否返还材料（正常拆除=true，被摧毁=false）。</param>
        public bool DemolishBuilding(string instanceId, bool giveRefunds = true)
        {
            if (!_placed.TryGetValue(instanceId, out var pb))
            {
                Debug.LogWarning($"[BuildingSystem] 找不到建筑实例 {instanceId}，拆除失败。");
                return false;
            }

            if (!pb.Data.canBeDemolished && giveRefunds)
            {
                Debug.LogWarning($"[BuildingSystem] '{pb.Data.buildingName}' 不允许拆除。");
                return false;
            }

            var buildingId = pb.Data.buildingId;

            // 返还材料
            if (giveRefunds)
                GiveRefunds(pb.Data);

            _placed.Remove(instanceId);

            if (pb != null && pb.gameObject != null)
                Destroy(pb.gameObject);

            EventBus.Publish(new BuildingDemolishedEvent(buildingId, instanceId));
            Debug.Log($"[BuildingSystem] 已拆除建筑 '{buildingId}' (id={instanceId})。");

            return true;
        }

        // ── 清空（存档加载前调用） ────────────────────────────────────────────

        public void ClearAllBuildings()
        {
            foreach (var pb in _placed.Values)
            {
                if (pb != null && pb.gameObject != null)
                    Destroy(pb.gameObject);
            }
            _placed.Clear();
            _instanceCounter = 0;
        }

        // ── 存档恢复 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 从存档 DTO 数组恢复所有建筑（ClearAllBuildings 之后调用）。
        /// </summary>
        public void RestoreFromDTOs(PlacedBuildingDTO[] dtos)
        {
            if (dtos == null || _database == null) return;

            var root = _buildingRoot != null ? _buildingRoot : transform;

            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.buildingId)) continue;

                var data = _database.GetById(dto.buildingId);
                if (data == null || !data.IsValid())
                {
                    Debug.LogWarning($"[BuildingSystem] 存档中找不到建筑数据 '{dto.buildingId}'，跳过。");
                    continue;
                }

                var pos = new Vector3(dto.posX, dto.posY, dto.posZ);
                var rot = Quaternion.Euler(0f, dto.rotY, 0f);
                var go  = Instantiate(data.buildingPrefab, pos, rot, root);

                var pb = go.GetComponent<PlacedBuilding>() ?? go.AddComponent<PlacedBuilding>();
                pb.RestoreFromDTO(data, dto);

                _placed[dto.instanceId] = pb;

                // 同步 ID 种子，避免碰撞
                if (int.TryParse(dto.instanceId.Replace("b", ""), out int seed))
                    _instanceCounter = Math.Max(_instanceCounter, seed);
            }
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────

        private void DeductCosts(BuildingData data)
        {
            var inv = ItemSystem.Instance?.inventory;
            if (inv == null || data.buildCosts == null) return;

            foreach (var cost in data.buildCosts)
            {
                if (cost.item == null) continue;
                inv.RemoveItem(cost.item, cost.quantity);
            }
        }

        private void GiveRefunds(BuildingData data)
        {
            var inv = ItemSystem.Instance?.inventory;
            if (inv == null) return;

            foreach (var (item, qty) in data.GetDemolishRefunds())
            {
                if (item == null || qty <= 0) continue;
                inv.AddItem(item, qty);
            }
        }

        private string GenerateInstanceId()
        {
            _instanceCounter++;
            return $"b{_instanceCounter:D6}";
        }
    }
}
