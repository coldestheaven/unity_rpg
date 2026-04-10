using UnityEngine;
using Framework.Events;

namespace RPG.Items
{
    // ──────────────────────────────────────────────────────────────────────────
    // PickupDetector
    //
    // 职责：
    //   • 挂在玩家身上，定期扫描周围的 ItemPickup。
    //   • 在自动拾取半径内直接调用 ManualPickup。
    //   • 在磁铁半径内触发 ItemPickup 的飞向玩家效果。
    //
    // 性能：
    //   • 使用 NonAlloc 版 OverlapCircle/OverlapSphere，固定缓冲区无 GC。
    //   • 可配置每帧检测间隔（scanInterval）减少 CPU 开销。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 玩家侧的自动拾取检测组件。
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public sealed class PickupDetector : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("拾取范围")]
        [Tooltip("自动拾取半径：在此范围内立即收入背包。")]
        [SerializeField, Min(0f)] private float _autoPickupRadius = 1.2f;

        [Tooltip("磁铁半径：在此范围内掉落物飞向玩家。0 = 禁用。")]
        [SerializeField, Min(0f)] private float _magnetRadius = 2.5f;

        [Tooltip("是否开启自动拾取。关闭后只有磁铁效果生效。")]
        [SerializeField] private bool _autoPickup = true;

        [Header("性能")]
        [Tooltip("扫描间隔（秒）。0 = 每帧扫描。")]
        [SerializeField, Min(0f)] private float _scanInterval = 0.1f;

        [Tooltip("OverlapCircle 结果缓冲区大小（同帧最多感知几个物体）。")]
        [SerializeField, Min(1)] private int _bufferSize = 16;

        [Tooltip("拾取物所在 Layer（建议独立 Layer 提升性能）。")]
        [SerializeField] private LayerMask _pickupLayer = ~0;

        // ── 内部 ─────────────────────────────────────────────────────────────

        private InventorySystem _inventory;
        private Collider2D[]    _hitBuffer2D;
        private float           _scanTimer;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _inventory   = GetComponent<InventorySystem>();
            _hitBuffer2D = new Collider2D[_bufferSize];
        }

        private void Update()
        {
            _scanTimer += Time.deltaTime;
            if (_scanTimer < _scanInterval) return;
            _scanTimer = 0f;

            Scan();
        }

        // ── 核心扫描 ──────────────────────────────────────────────────────────

        private void Scan()
        {
            float radius = Mathf.Max(_autoPickupRadius, _magnetRadius);
            if (radius <= 0f) return;

            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, radius, _hitBuffer2D, _pickupLayer);

            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer2D[i];
                if (col == null) continue;

                var pickup = col.GetComponent<ItemPickup>();
                if (pickup == null) continue;

                float dist = Vector2.Distance(transform.position, pickup.transform.position);

                if (_autoPickup && dist <= _autoPickupRadius)
                {
                    // 直接收取
                    pickup.ManualPickup(gameObject);
                }
                else if (dist <= _magnetRadius && pickup.flyToPlayer && !pickup.autoPickup)
                {
                    // 启动飞向玩家
                    pickup.SetAutoPickup(true);
                }
            }
        }

        // ── 公开控制 ──────────────────────────────────────────────────────────

        /// <summary>运行时修改自动拾取开关。</summary>
        public void SetAutoPickup(bool enabled) => _autoPickup = enabled;

        /// <summary>扩大拾取半径（例如技能效果临时增益）。</summary>
        public void SetPickupRadius(float radius) => _autoPickupRadius = Mathf.Max(0, radius);

        // ── Gizmo ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_autoPickupRadius > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, _autoPickupRadius);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, _autoPickupRadius);
            }

            if (_magnetRadius > _autoPickupRadius)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                Gizmos.DrawSphere(transform.position, _magnetRadius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _magnetRadius);
            }
        }
#endif
    }
}
