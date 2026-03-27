using UnityEngine;

namespace Gameplay.Enemy
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    /// <summary>
    /// 敌人AI系统
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : Framework.Base.MonoBehaviourBase
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float chaseSpeed = 5f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float idleDuration = 0.75f;
        private int currentPatrolIndex = 0;
        private float idleTimer;
        private float facingDirectionX = 1f;

        private EnemyState currentState = EnemyState.Idle;
        private GameObject target;
        private Rigidbody2D rb;
        private EnemyAttack attack;
        private Gameplay.Combat.Health health;

        public EnemyState CurrentState => currentState;
        public bool IsMoving => currentState == EnemyState.Patrol || currentState == EnemyState.Chase;
        public bool IsAttacking => currentState == EnemyState.Attack && attack != null && attack.IsAttacking;
        public float FacingDirectionX => facingDirectionX;
        public Vector2 Velocity => rb != null ? rb.velocity : Vector2.zero;

        protected override void Awake()
        {
            base.Awake();
            EnsurePresentationComponent();
            rb = GetComponent<Rigidbody2D>();
            attack = GetComponent<EnemyAttack>();
            health = GetComponent<Gameplay.Combat.Health>();
        }

        private void EnsurePresentationComponent()
        {
            if (GetComponent<EnemyPresenter>() == null && GetComponent<Animator>() != null)
            {
                gameObject.AddComponent<EnemyPresenter>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }

        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }

        }

        protected override void Update()
        {
            base.Update();
            if (health != null && health.IsDead)
            {
                ChangeState(EnemyState.Dead);
                return;
            }

            UpdateAI();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (currentState == EnemyState.Dead)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            FixedUpdateAI();
        }

        public void SetTarget(GameObject newTarget)
        {
            target = newTarget;
        }

        private void UpdateAI()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
                case EnemyState.Dead:
                    break;
            }
        }

        private void FixedUpdateAI()
        {
            switch (currentState)
            {
                case EnemyState.Patrol:
                    if (HasPatrolPoints())
                    {
                        MoveTo(patrolPoints[currentPatrolIndex].position, moveSpeed);
                    }
                    break;
                case EnemyState.Chase:
                    if (target != null)
                    {
                        MoveTo(target.transform.position, chaseSpeed);
                    }
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (target != null)
            {
                ChangeState(EnemyState.Chase);
            }
            else if (HasPatrolPoints())
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleDuration)
                {
                    ChangeState(EnemyState.Patrol);
                }
            }
        }

        private void UpdatePatrol()
        {
            if (!HasPatrolPoints())
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (target != null)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.1f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }

        private void UpdateChase()
        {
            if (target == null)
            {
                ChangeState(HasPatrolPoints() ? EnemyState.Patrol : EnemyState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance <= attackRange)
            {
                ChangeState(EnemyState.Attack);
            }
        }

        private void UpdateAttack()
        {
            if (target == null)
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > attackRange * 1.5f)
            {
                ChangeState(EnemyState.Chase);
            }
            else if (attack != null && attack.CanAttack)
            {
                attack.TryAttackTarget(target);
            }
        }

        private void MoveTo(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.velocity = direction * speed;

            if (Mathf.Abs(direction.x) > 0.01f)
            {
                facingDirectionX = direction.x;
            }
        }

        private bool HasPatrolPoints()
        {
            return patrolPoints != null && patrolPoints.Length > 0;
        }

        private void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            rb.velocity = Vector2.zero;
            idleTimer = 0f;
        }

        private void HandleDeath()
        {
            ChangeState(EnemyState.Dead);
            rb.velocity = Vector2.zero;
        }
    }
}
