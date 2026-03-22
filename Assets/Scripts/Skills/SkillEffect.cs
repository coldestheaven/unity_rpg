using UnityEngine;
using RPG.Core;

namespace RPG.Skills
{
    /// <summary>
    /// 技能效果基类
    /// </summary>
    public abstract class SkillEffect : MonoBehaviour
    {
        protected SkillData skillData;
        protected Transform caster;

        protected Rigidbody2D rb;
        protected Collider2D col;

        public virtual void Initialize(SkillData data, Transform casterTransform)
        {
            skillData = data;
            caster = casterTransform;

            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
        }

        protected virtual void Start()
        {
            ApplyInitialEffects();
        }

        protected virtual void Update()
        {
            UpdateEffect();
        }

        protected virtual void FixedUpdate()
        {
            PhysicsUpdate();
        }

        /// <summary>
        /// 应用初始效果
        /// </summary>
        protected virtual void ApplyInitialEffects()
        {
            // 可以在子类中实现
        }

        /// <summary>
        /// 更新效果
        /// </summary>
        protected virtual void UpdateEffect()
        {
            // 可以在子类中实现
        }

        /// <summary>
        /// 物理更新
        /// </summary>
        protected virtual void PhysicsUpdate()
        {
            // 可以在子类中实现
        }

        /// <summary>
        /// 对目标造成伤害
        /// </summary>
        protected void DealDamage(IDamageable target, Vector2 attackerPosition)
        {
            if (target != null)
            {
                int damage = skillData.GetDamage(1); // TODO: 使用技能等级
                target.TakeDamage(damage, attackerPosition);
            }
        }

        /// <summary>
        /// 销毁效果
        /// </summary>
        protected void DestroyEffect()
        {
            if (skillData.impactEffect != null)
            {
                Instantiate(skillData.impactEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 投射物效果
    /// </summary>
    public class ProjectileEffect : SkillEffect
    {
        [Header("投射物设置")]
        public float speed = 10f;
        public float lifetime = 3f;
        public bool homing = false;
        public float homingStrength = 5f;

        private Transform target;
        private float timeAlive;

        protected override void ApplyInitialEffects()
        {
            if (rb != null)
            {
                rb.velocity = transform.right * speed;
            }
        }

        protected override void Update()
        {
            base.Update();

            timeAlive += Time.deltaTime;

            // 追踪逻辑
            if (homing && target != null)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                rb.velocity = Vector2.Lerp(rb.velocity, direction * speed, homingStrength * Time.deltaTime);
            }

            // 自动销毁
            if (timeAlive >= lifetime)
            {
                DestroyEffect();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            // 检查是否击中目标
            if (other.CompareTag("Enemy"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DealDamage(damageable, caster.position);
                    DestroyEffect();
                }
            }

            // 检查是否击中障碍物
            if (other.CompareTag("Ground") || other.CompareTag("Wall"))
            {
                DestroyEffect();
            }
        }

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }
    }

    /// <summary>
    /// 范围效果
    /// </summary>
    public class AreaEffect : SkillEffect
    {
        [Header("范围设置")]
        public float duration = 2f;
        public float tickRate = 0.5f;
        public int tickCount;

        private float timeElapsed;
        private float lastTickTime;

        protected override void ApplyInitialEffects()
        {
            tickCount = Mathf.FloorToInt(duration / tickRate);
        }

        protected override void Update()
        {
            base.Update();

            timeElapsed += Time.deltaTime;

            // 定时伤害
            if (timeElapsed - lastTickTime >= tickRate)
            {
                ApplyAreaDamage();
                lastTickTime = timeElapsed;
            }

            // 自动销毁
            if (timeElapsed >= duration)
            {
                DestroyEffect();
            }
        }

        private void ApplyAreaDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillData.areaRadius);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        DealDamage(damageable, caster.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, skillData.areaRadius);
        }
    }

    /// <summary>
    /// 波浪效果
    /// </summary>
    public class WaveEffect : SkillEffect
    {
        [Header("波浪设置")]
        public float expansionSpeed = 5f;
        public float maxRadius = 10f;
        public float damagePerSecond = 10f;

        private float currentRadius;
        private float damageTimer;

        protected override void Update()
        {
            base.Update();

            // 扩展范围
            currentRadius += expansionSpeed * Time.deltaTime;

            // 持续伤害
            damageTimer += Time.deltaTime;
            if (damageTimer >= 1f)
            {
                ApplyWaveDamage();
                damageTimer = 0f;
            }

            // 自动销毁
            if (currentRadius >= maxRadius)
            {
                DestroyEffect();
            }
        }

        private void ApplyWaveDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        // 根据距离计算伤害
                        float distance = Vector2.Distance(transform.position, hit.transform.position);
                        float damageMultiplier = 1f - (distance / maxRadius);
                        int damage = Mathf.RoundToInt(damagePerSecond * damageMultiplier);

                        damageable.TakeDamage(damage, caster.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }

    /// <summary>
    /// 瞬发效果
    /// </summary>
    public class InstantEffect : SkillEffect
    {
        [Header("瞬发设置")]
        public float damageDelay = 0.1f;
        public float destroyDelay = 0.5f;

        private bool damageDealt;

        protected override void Update()
        {
            base.Update();

            // 延迟伤害
            if (!damageDealt && damageDelay > 0)
            {
                damageDelay -= Time.deltaTime;
                if (damageDelay <= 0)
                {
                    ApplyInstantDamage();
                }
            }
            else if (!damageDealt)
            {
                ApplyInstantDamage();
            }

            // 延迟销毁
            destroyDelay -= Time.deltaTime;
            if (destroyDelay <= 0)
            {
                DestroyEffect();
            }
        }

        private void ApplyInstantDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillData.areaRadius);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        DealDamage(damageable, caster.position);
                    }
                }
            }

            damageDealt = true;
        }
    }
}
