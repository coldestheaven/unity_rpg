using UnityEngine;

namespace RPG.Enemy
{
    public enum EnemyType
    {
        Slime,
        Goblin,
        Skeleton,
        Boss
    }

    public abstract class EnemyBase : MonoBehaviour, RPG.Core.IDamageable
    {
        [Header("基本属性")]
        public string enemyName;
        public EnemyType enemyType;
        public int health;
        public int maxHealth;
        public int attackDamage;
        public float moveSpeed;
        public int experienceReward;
        public int goldReward;

        [Header("AI设置")]
        public float detectionRange = 5f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;
        public LayerMask playerLayer;

        [Header("视觉反馈")]
        public GameObject deathEffect;
        public GameObject hitEffect;
        public HealthBar healthBar;

        protected Transform player;
        protected Rigidbody2D rb;
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;

        protected bool isDead;
        protected bool canAttack = true;
        protected bool isAttacking;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        protected virtual void Start()
        {
            health = maxHealth;
            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
            }
        }

        protected virtual void Update()
        {
            if (isDead) return;

            CheckPlayerDistance();
        }

        protected virtual void FixedUpdate()
        {
            if (isDead) return;

            AIUpdate();
        }

        protected virtual void AIUpdate()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
            {
                ChasePlayer();
            }
            else if (distanceToPlayer <= attackRange && canAttack)
            {
                Attack();
            }
        }

        protected virtual void ChasePlayer()
        {
            if (player == null) return;

            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            spriteRenderer.flipX = direction.x < 0;
            animator.SetBool("IsMoving", true);
        }

        protected virtual void StopChasing()
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
        }

        protected virtual void CheckPlayerDistance()
        {
            if (player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer > detectionRange)
            {
                StopChasing();
            }
        }

        protected virtual void Attack()
        {
            if (!canAttack) return;

            isAttacking = true;
            canAttack = false;

            animator.SetTrigger("Attack");

            Invoke(nameof(EndAttack), 0.3f);
            Invoke(nameof(ResetAttackCooldown), attackCooldown);
        }

        protected virtual void EndAttack()
        {
            isAttacking = false;
        }

        protected virtual void ResetAttackCooldown()
        {
            canAttack = true;
        }

        public virtual void TakeDamage(int damage, Vector2 attackerPosition)
        {
            if (isDead) return;

            health -= damage;

            if (healthBar != null)
            {
                healthBar.SetHealth(health);
            }

            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            animator.SetTrigger("Hit");

            if (health <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            isDead = true;
            animator.SetBool("IsDead", true);
            rb.velocity = Vector2.zero;

            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }

            RPG.Core.GameManager.Instance?.AddExperience(experienceReward);
            RPG.Core.GameManager.Instance?.AddGold(goldReward);

            Destroy(gameObject, 0.5f);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        public bool IsAlive()
        {
            return !isDead;
        }
    }
}
