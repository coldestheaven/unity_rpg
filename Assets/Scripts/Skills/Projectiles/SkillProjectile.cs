using System;
using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Projectiles
{
    /// <summary>
    /// 挂载在投掷物预制体上的运行时组件。
    /// 由 <see cref="RPG.Skills.Graph.Nodes.ProjectileDamageNode"/> 在发射时初始化，
    /// 自主处理飞行、追踪、碰撞、伤害结算与销毁。
    ///
    /// 预制体要求:
    ///   • 必须有 Collider2D（设置为 IsTrigger = true）
    ///   • 推荐有 Rigidbody2D（Kinematic）以触发 OnTriggerEnter2D
    ///   • 可选 SpriteRenderer / Trail / ParticleSystem
    /// </summary>
    public sealed class SkillProjectile : MonoBehaviour
    {
        // ── 发射时注入的参数 ──────────────────────────────────────────────────

        [NonSerialized] public DamageInfo  Payload;
        [NonSerialized] public Vector2     Direction;          // 初始方向（已归一化）
        [NonSerialized] public float       Speed        = 8f;
        [NonSerialized] public float       MaxRange     = 15f;
        [NonSerialized] public bool        Piercing     = false;
        [NonSerialized] public int         MaxPierces   = 3;   // Piercing=true 时有效
        [NonSerialized] public float       HomingStrength = 0f;
        [NonSerialized] public LayerMask   HitMask;

        /// <summary>命中时回调（可选）。由节点注入以支持图级事件。</summary>
        [NonSerialized] public Action<Collider2D> OnHit;

        // ── 内部状态 ──────────────────────────────────────────────────────────

        private Vector3     _startPos;
        private int         _pierceCount;
        private Transform   _homingTarget;
        private Rigidbody2D _rb;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.isKinematic  = true;
            }
        }

        private void Start()
        {
            _startPos = transform.position;
            AlignSprite();

            if (HomingStrength > 0f)
                _homingTarget = FindNearestEnemy();
        }

        private void Update()
        {
            // ── 追踪 ───────────────────────────────────────────────────────
            if (HomingStrength > 0f && _homingTarget != null)
            {
                Vector2 toTarget = ((Vector2)_homingTarget.position - (Vector2)transform.position).normalized;
                Direction = Vector2.Lerp(Direction, toTarget, HomingStrength * Time.deltaTime).normalized;
                AlignSprite();
            }

            // ── 移动 ───────────────────────────────────────────────────────
            Vector3 move = (Vector3)Direction * Speed * Time.deltaTime;
            if (_rb != null)
                _rb.MovePosition(_rb.position + (Vector2)move);
            else
                transform.position += move;

            // ── 射程检查 ────────────────────────────────────────────────────
            if (Vector3.Distance(_startPos, transform.position) >= MaxRange)
                Destroy(gameObject);
        }

        // ── 碰撞 ──────────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HitMask.Contains(other.gameObject.layer)) return;
            if (other.isTrigger) return;

            OnHit?.Invoke(other);
            CombatResolver.TryApplyDamage(other, Payload);

            if (!Piercing)
            {
                Destroy(gameObject);
            }
            else
            {
                _pierceCount++;
                if (_pierceCount >= MaxPierces)
                    Destroy(gameObject);
            }
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private void AlignSprite()
        {
            float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private Transform FindNearestEnemy()
        {
            var enemies = Physics2D.OverlapCircleAll(transform.position, 30f, HitMask);
            Transform nearest = null;
            float minDist = float.MaxValue;
            foreach (var e in enemies)
            {
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e.transform; }
            }
            return nearest;
        }
    }

    // ── 扩展方法 ───────────────────────────────────────────────────────────────

    internal static class LayerMaskExtensions
    {
        internal static bool Contains(this LayerMask mask, int layer)
            => (mask.value & (1 << layer)) != 0;
    }
}
