using UnityEngine;

namespace RPG.Enemy
{
    public class SlimeEnemy : EnemyBase
    {
        [Header("史莱姆特有属性")]
        public float jumpCooldown = 2f;
        public float jumpForce = 8f;

        private bool canJump = true;

        protected override void Start()
        {
            base.Start();
            enemyType = EnemyType.Slime;
        }

        protected override void AIUpdate()
        {
            if (isDead) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                if (canJump)
                {
                    JumpTowardsPlayer();
                }
            }
        }

        private void JumpTowardsPlayer()
        {
            if (player == null) return;

            canJump = false;

            Vector2 direction = (player.position - transform.position).normalized;
            rb.AddForce(direction * jumpForce, ForceMode2D.Impulse);

            animator.SetBool("IsMoving", true);

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        private void ResetJump()
        {
            canJump = true;
        }
    }

    public class GoblinEnemy : EnemyBase
    {
        [Header("哥布林特有属性")]
        public float sprintSpeed = 8f;
        public float patrolRange = 5f;
        public Transform[] patrolPoints;
        private int currentPatrolPoint;

        private bool isPatrolling;

        protected override void Start()
        {
            base.Start();
            enemyType = EnemyType.Goblin;
            isPatrolling = true;
        }

        protected override void AIUpdate()
        {
            if (isDead) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                isPatrolling = false;
                base.AIUpdate();
            }
            else if (isPatrolling && patrolPoints.Length > 0)
            {
                Patrol();
            }
            else
            {
                StopChasing();
            }
        }

        private void Patrol()
        {
            Transform targetPoint = patrolPoints[currentPatrolPoint];
            Vector2 direction = (targetPoint.position - transform.position).normalized;

            if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
            {
                currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
            }

            rb.velocity = direction * moveSpeed;
            spriteRenderer.flipX = direction.x < 0;
        }

        protected override void ChasePlayer()
        {
            if (player == null) return;

            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * sprintSpeed;
            spriteRenderer.flipX = direction.x < 0;
            animator.SetBool("IsMoving", true);
        }
    }

    public class SkeletonEnemy : EnemyBase
    {
        [Header("骷髅特有属性")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float projectileSpeed = 10f;
        public float attackRange = 6f;

        protected override void Start()
        {
            base.Start();
            enemyType = EnemyType.Skeleton;
        }

        protected override void Attack()
        {
            if (!canAttack || isAttacking) return;

            isAttacking = true;
            canAttack = false;

            animator.SetTrigger("Attack");

            FireProjectile();

            Invoke(nameof(EndAttack), 0.5f);
            Invoke(nameof(ResetAttackCooldown), attackCooldown);
        }

        private void FireProjectile()
        {
            if (projectilePrefab == null || firePoint == null || player == null) return;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 direction = (player.position - firePoint.position).normalized;

            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(direction, projectileSpeed, attackDamage);
            }
            else
            {
                Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                if (projectileRb != null)
                {
                    projectileRb.velocity = direction * projectileSpeed;
                }
            }
        }
    }

    public class Projectile : MonoBehaviour
    {
        public int damage;
        public float lifetime = 3f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Vector2 direction, float speed, int dmg)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * speed;
            }
            damage = dmg;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<RPG.Player.PlayerHealth>()?.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
