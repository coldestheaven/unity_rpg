using UnityEngine;
using RPG.Data;

namespace RPG.Building
{
    /// <summary>
    /// 已放置到场景中的建筑实例。
    ///
    /// ■ 职责：
    ///   • 持有运行时状态（InstanceId、血量、完成状态）。
    ///   • 提供 TakeDamage / Repair 方法，血量耗尽时通知 BuildingSystem 拆除。
    ///   • ToDTO / FromDTO 用于存读档。
    ///
    /// ■ 挂载：
    ///   • 由 BuildingSystem.PlaceBuilding 在实例化 buildingPrefab 后调用 Initialize。
    ///   • 不应手动拖拽此脚本；在 Prefab 上挂载即可，Initialize 会填充所有数据。
    /// </summary>
    public sealed class PlacedBuilding : MonoBehaviour
    {
        // ── 只读状态 ──────────────────────────────────────────────────────────

        public string       InstanceId     { get; private set; }
        public BuildingData Data           { get; private set; }
        public float        CurrentHealth  { get; private set; }
        public float        MaxHealth      => Data != null ? Data.maxHealth : 1f;
        public bool         IsAlive        => CurrentHealth > 0f;

        /// <summary>血量百分比 (0~1)。</summary>
        public float HealthPercent => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        // ── 事件 ─────────────────────────────────────────────────────────────

        public System.Action<PlacedBuilding, float> OnDamageTaken;
        public System.Action<PlacedBuilding>        OnRepaired;
        public System.Action<PlacedBuilding>        OnDestroyed;

        // ── 初始化 ────────────────────────────────────────────────────────────

        /// <summary>由 BuildingSystem 在放置后调用。</summary>
        public void Initialize(BuildingData data, string instanceId)
        {
            Data          = data;
            InstanceId    = instanceId;
            CurrentHealth = data.maxHealth;
        }

        /// <summary>由 BuildingSystem.RestoreFromDTOs 在存档加载后调用。</summary>
        public void RestoreFromDTO(BuildingData data, PlacedBuildingDTO dto)
        {
            Data          = data;
            InstanceId    = dto.instanceId;
            CurrentHealth = dto.currentHealth;
        }

        // ── 战斗接口 ──────────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamageTaken?.Invoke(this, amount);

            if (CurrentHealth <= 0f)
            {
                OnDestroyed?.Invoke(this);
                BuildingSystem.Instance?.DemolishBuilding(InstanceId, giveRefunds: false);
            }
        }

        public void Repair(float amount)
        {
            if (amount <= 0f || CurrentHealth >= MaxHealth) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnRepaired?.Invoke(this);
        }

        public void RepairFull() => Repair(MaxHealth);

        // ── 序列化 ────────────────────────────────────────────────────────────

        public PlacedBuildingDTO ToDTO()
        {
            return new PlacedBuildingDTO
            {
                instanceId    = InstanceId,
                buildingId    = Data != null ? Data.buildingId : "",
                posX          = transform.position.x,
                posY          = transform.position.y,
                posZ          = transform.position.z,
                rotY          = transform.eulerAngles.y,
                currentHealth = CurrentHealth,
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Data == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position,
                new Vector3(Data.footprintSize.x, 0.1f, Data.footprintSize.y));
        }
#endif
    }
}
