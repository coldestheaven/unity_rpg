using System;
using UnityEngine;
using Framework.Events;
using RPG.Core;
using RPG.Data;

namespace RPG.Items
{
    // ──────────────────────────────────────────────────────────────────────────
    // DropSystem
    //
    // 职责：
    //   • 监听 EnemyDiedEvent，自动从 EnemyData.lootTable 生成掉落物。
    //   • 可手动调用 SpawnDrops 在任意位置生成一批物品。
    //   • 支持掉落扩散半径、最大掉落数上限、整体掉落率倍增系数。
    //
    // 设计：
    //   • 使用 RPG.Enemy.LootTable（EnemyData 内嵌的简单加权表）。
    //   • ItemPickup 通过 ItemPickupFactory 创建，支持自定义 worldPickupPrefab。
    //   • 每次掉落结果通过 PickupSpawnedEvent 广播，供 UI 等系统响应。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 掉落系统 — 将敌人死亡事件转化为世界中的拾取物。
    /// </summary>
    public sealed class DropSystem : Singleton<DropSystem>
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("掉落参数")]
        [Tooltip("物品落点围绕死亡位置的扩散半径（世界单位）。")]
        [SerializeField, Min(0f)] private float _dropSpread = 0.8f;

        [Tooltip("单次死亡最多掉落物品种类数（0 = 无限制）。")]
        [SerializeField, Min(0)] private int _maxDropsPerKill = 8;

        [Tooltip("全局掉落率倍增系数（1 = 正常，2 = 双倍）。")]
        [SerializeField, Min(0f)] private float _dropRateMultiplier = 1f;

        [Tooltip("金币拾取预制件（null 则直接加入背包）。")]
        [SerializeField] private GameObject _goldPickupPrefab;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        // ── 事件处理 ──────────────────────────────────────────────────────────

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            // 尝试从数据库取敌人数据
            var enemyData = GameDataService.Instance?.Enemies?.GetById(evt.EnemyId);
            if (enemyData == null)
            {
                // 数据库查不到时静默跳过（野外生成的敌人可能没有 ID）
                return;
            }

            // 掉金币
            if (evt.Gold > 0)
                HandleGoldDrop(evt.Gold, evt.Position);

            // 掉物品
            var table = enemyData.lootTable;
            if (table?.possibleDrops == null || table.possibleDrops.Length == 0)
                return;

            SpawnDropsFromLegacyTable(table, evt.Position);
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// 在指定位置手动生成单个掉落物。
        /// </summary>
        public ItemPickup SpawnDrop(ItemData item, int quantity, Vector3 position)
        {
            if (item == null || quantity <= 0) return null;

            var pos    = RandomizedPosition(position);
            var pickup = ItemPickupFactory.Create(item, quantity, pos);

            EventBus.Publish(new PickupSpawnedEvent(
                ResolveId(item), quantity, pos));

            return pickup;
        }

        /// <summary>
        /// 在指定位置批量生成物品（来自 ScriptableObject LootTable）。
        /// </summary>
        public void SpawnDropsFromTable(LootTable table, Vector3 position)
        {
            if (table == null) return;

            var drops = table.GetDrops();
            int count = 0;
            foreach (var (item, qty) in drops)
            {
                if (_maxDropsPerKill > 0 && count >= _maxDropsPerKill) break;
                SpawnDrop(item, qty, position);
                count++;
            }
        }

        // ── 内部 ──────────────────────────────────────────────────────────────

        private void SpawnDropsFromLegacyTable(RPG.Enemy.LootTable table, Vector3 origin)
        {
            int count = 0;
            foreach (var entry in table.possibleDrops)
            {
                if (_maxDropsPerKill > 0 && count >= _maxDropsPerKill) break;
                if (entry?.itemData == null) continue;

                float roll     = UnityEngine.Random.Range(0f, 100f);
                float adjusted = entry.dropChance * _dropRateMultiplier;
                if (roll > adjusted) continue;

                int qty = table.GetRandomDropAmount(entry);
                SpawnDrop(entry.itemData, qty, origin);
                count++;
            }
        }

        private void HandleGoldDrop(int gold, Vector3 position)
        {
            if (_goldPickupPrefab != null)
            {
                // 生成金币拾取物（预制件自行处理 AddGold 逻辑）
                var pos = RandomizedPosition(position);
                var go  = Instantiate(_goldPickupPrefab, pos, Quaternion.identity);
                var gc  = go.GetComponent<IGoldPickup>();
                gc?.SetAmount(gold);
            }
            else
            {
                // 无预制件：直接加入玩家背包金币
                ItemSystem.Instance?.inventory?.AddGold(gold);
            }
        }

        private Vector3 RandomizedPosition(Vector3 origin)
        {
            var offset = UnityEngine.Random.insideUnitSphere * _dropSpread;
            offset.y   = 0f;
            return origin + offset;
        }

        private static string ResolveId(ItemData item)
            => !string.IsNullOrEmpty(item.itemId) ? item.itemId : item.name;
    }

    /// <summary>
    /// 金币拾取物可实现的接口，让 DropSystem 设置金额。
    /// </summary>
    public interface IGoldPickup
    {
        void SetAmount(int gold);
    }
}
