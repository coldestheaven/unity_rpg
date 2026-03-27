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
    public class EnemyAI : Framework.Base.MonoBehaviourBase
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float chaseSpeed = 5f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float patrolRange = 5f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        private int currentPatrolIndex = 0;

        private EnemyState currentState = EnemyState.Idle;
        private GameObject target;
        private Vector3 startPosition;
        private Rigidbody2D rb;

        public EnemyState CurrentState => currentState;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            startPosition = transform.position;
        }

        protected override void Update()
        {
            base.Update();
            UpdateAI();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
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
                    MoveTo(patrolPoints[currentPatrolIndex].position, moveSpeed);
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
            else
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        private void UpdatePatrol()
        {
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
                ChangeState(EnemyState.Patrol);
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
            else
            {
                PerformAttack();
            }
        }

        private void MoveTo(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.velocity = direction * speed;
        }

        private void PerformAttack()
        {
            // Attack logic here
        }

        private void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            rb.velocity = Vector2.zero;
        }
    }
}
