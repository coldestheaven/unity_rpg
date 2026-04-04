using UnityEngine;
using UI.Views;

namespace RPG.Enemy
{
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("数据配置")]
        public EnemyData data;

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
        private Vector3 startPosition;
        private EnemyStateMachine stateMachine;

        protected bool isDead;
        protected bool canAttack = true;
        protected bool isAttacking;

        public EnemyData Data => data;
        public Transform Player => player;
        public Vector3 StartPosition => startPosition;
        public EnemyStateMachine StateMachine => stateMachine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            stateMachine = GetComponent<EnemyStateMachine>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        protected virtual void Start()
        {
            startPosition = transform.position;

            if (data != null)
            {
                maxHealth = data.maxHealth;
                attackDamage = data.attackDamage;
                moveSpeed = data.moveSpeed;
                detectionRange = data.detectionRange;
                attackRange = data.attackRange;
                attackCooldown = data.attackCooldown;
                experienceReward = data.experienceReward;
                goldReward = data.goldReward;
            }

            health = maxHealth;
            healthBar?.SetHealth(health, maxHealth);

            stateMachine?.Initialize(this);
        }

        protected virtual void Update()
        {
            if (isDead) return;

            if (stateMachine != null)
                stateMachine.Update();
            else
                CheckPlayerDistance();
        }

        protected virtual void FixedUpdate()
        {
            if (isDead) return;

            if (stateMachine != null)
                stateMachine.FixedUpdate();
            else
                AIUpdate();
        }

        protected virtual void AIUpdate()
        {
            if (player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
                ChasePlayer();
            else if (distanceToPlayer <= attackRange && canAttack)
                Attack();
        }

        protected virtual void ChasePlayer()
        {
            if (player == null) return;

            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;
            spriteRenderer.flipX = direction.x < 0;
            animator?.SetBool("IsMoving", true);
        }

        protected virtual void StopChasing()
        {
            rb.velocity = Vector2.zero;
            animator?.SetBool("IsMoving", false);
        }

        protected virtual void CheckPlayerDistance()
        {
            if (player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > detectionRange)
                StopChasing();
        }

        protected virtual void Attack()
        {
            if (!canAttack) return;

            isAttacking = true;
            canAttack = false;
            animator?.SetTrigger("Attack");
            Invoke(nameof(EndAttack), 0.3f);
            Invoke(nameof(ResetAttackCooldown), attackCooldown);
        }

        protected virtual void EndAttack() => isAttacking = false;

        protected virtual void ResetAttackCooldown() => canAttack = true;

        public virtual void TakeDamage(int damage, Vector2 attackerPosition)
        {
            if (isDead) return;

            health -= damage;
            healthBar?.SetHealth(health, maxHealth);

            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);

            animator?.SetTrigger("Hit");

            if (health <= 0)
                Die();
        }

        protected virtual void Die()
        {
            isDead = true;
            animator?.SetBool("IsDead", true);
            rb.velocity = Vector2.zero;

            if (deathEffect != null)
                Instantiate(deathEffect, transform.position, Quaternion.identity);

            RPG.Core.GameManager.Instance?.AddExperience(experienceReward);
            RPG.Core.GameManager.Instance?.AddGold(goldReward);

            Destroy(gameObject, 0.5f);
        }

        // Public API for EnemyStateMachine
        public void StopMovement() => StopChasing();

        public void MoveTowards(Vector2 direction, float speed)
        {
            rb.velocity = direction * speed;
            animator?.SetBool("IsMoving", true);
        }

        public void FaceDirection(float xDirection)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = xDirection < 0;
        }

        public bool IsPlayerInDetectionRange()
        {
            if (player == null) return false;
            return Vector2.Distance(transform.position, player.position) <= detectionRange;
        }

        public bool IsPlayerInAttackRange()
        {
            if (player == null) return false;
            return Vector2.Distance(transform.position, player.position) <= attackRange;
        }

        public bool CanPatrol() => data != null && data.canPatrol;

        public virtual void StartAttack() => Attack();

        public virtual void HandleDeath() => Die();

        public bool IsAlive() => !isDead;

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
