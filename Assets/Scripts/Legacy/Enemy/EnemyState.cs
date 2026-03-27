using UnityEngine;

namespace RPG.Enemy
{
    /// <summary>
    /// 敌人状态机
    /// </summary>
    public class EnemyStateMachine : MonoBehaviour
    {
        private EnemyBase enemy;
        private IEnemyState currentState;

        public IEnemyState CurrentState => currentState;

        public void Initialize(EnemyBase enemy)
        {
            this.enemy = enemy;
            ChangeState(new EnemyIdleState());
        }

        public void ChangeState(IEnemyState newState)
        {
            if (currentState != null)
            {
                currentState.ExitState(enemy);
            }

            currentState = newState;
            currentState.EnterState(enemy);
        }

        public void Update()
        {
            currentState?.UpdateState(enemy);
        }

        public void FixedUpdate()
        {
            currentState?.FixedUpdateState(enemy);
        }
    }

    /// <summary>
    /// 敌人状态接口
    /// </summary>
    public interface IEnemyState
    {
        void EnterState(EnemyBase enemy);
        void UpdateState(EnemyBase enemy);
        void FixedUpdateState(EnemyBase enemy);
        void ExitState(EnemyBase enemy);
    }

    /// <summary>
    /// 闲置状态
    /// </summary>
    public class EnemyIdleState : IEnemyState
    {
        public void EnterState(EnemyBase enemy)
        {
            enemy.StopMovement();
        }

        public void UpdateState(EnemyBase enemy)
        {
            if (enemy.IsPlayerInDetectionRange())
            {
                enemy.StateMachine.ChangeState(new EnemyChaseState());
            }
            else if (enemy.CanPatrol())
            {
                enemy.StateMachine.ChangeState(new EnemyPatrolState());
            }
        }

        public void FixedUpdateState(EnemyBase enemy)
        {
            // No physics in idle
        }

        public void ExitState(EnemyBase enemy)
        {
            // Cleanup
        }
    }

    /// <summary>
    /// 巡逻状态
    /// </summary>
    public class EnemyPatrolState : IEnemyState
    {
        private Vector2 patrolTarget;
        private float patrolTimer;
        private bool isMoving;

        public void EnterState(EnemyBase enemy)
        {
            SetRandomPatrolTarget(enemy);
        }

        public void UpdateState(EnemyBase enemy)
        {
            if (enemy.IsPlayerInDetectionRange())
            {
                enemy.StateMachine.ChangeState(new EnemyChaseState());
                return;
            }

            if (isMoving)
            {
                float distance = Vector2.Distance(enemy.transform.position, patrolTarget);
                if (distance < 0.1f)
                {
                    isMoving = false;
                    patrolTimer = enemy.Data.patrolWaitTime;
                }
            }
            else
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer <= 0)
                {
                    SetRandomPatrolTarget(enemy);
                }
            }
        }

        public void FixedUpdateState(EnemyBase enemy)
        {
            if (isMoving)
            {
                Vector2 direction = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                enemy.MoveTowards(direction, enemy.Data.patrolSpeed);
            }
            else
            {
                enemy.StopMovement();
            }
        }

        public void ExitState(EnemyBase enemy)
        {
            // Cleanup
        }

        private void SetRandomPatrolTarget(EnemyBase enemy)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 5f;
            patrolTarget = (Vector2)enemy.StartPosition + randomOffset;
            isMoving = true;
        }
    }

    /// <summary>
    /// 追逐状态
    /// </summary>
    public class EnemyChaseState : IEnemyState
    {
        public void EnterState(EnemyBase enemy)
        {
            // Start chasing
        }

        public void UpdateState(EnemyBase enemy)
        {
            if (!enemy.IsPlayerInDetectionRange())
            {
                if (enemy.CanPatrol())
                {
                    enemy.StateMachine.ChangeState(new EnemyPatrolState());
                }
                else
                {
                    enemy.StateMachine.ChangeState(new EnemyIdleState());
                }
                return;
            }

            if (enemy.IsPlayerInAttackRange())
            {
                enemy.StateMachine.ChangeState(new EnemyAttackState());
            }
        }

        public void FixedUpdateState(EnemyBase enemy)
        {
            if (enemy.Player != null)
            {
                Vector2 direction = (enemy.Player.position - enemy.transform.position).normalized;
                enemy.MoveTowards(direction, enemy.Data.chaseSpeed);
                enemy.FaceDirection(direction.x);
            }
        }

        public void ExitState(EnemyBase enemy)
        {
            enemy.StopMovement();
        }
    }

    /// <summary>
    /// 攻击状态
    /// </summary>
    public class EnemyAttackState : IEnemyState
    {
        public void EnterState(EnemyBase enemy)
        {
            enemy.StartAttack();
        }

        public void UpdateState(EnemyBase enemy)
        {
            if (!enemy.IsPlayerInAttackRange())
            {
                enemy.StateMachine.ChangeState(new EnemyChaseState());
            }
        }

        public void FixedUpdateState(EnemyBase enemy)
        {
            enemy.StopMovement();

            if (enemy.Player != null)
            {
                Vector2 direction = (enemy.Player.position - enemy.transform.position).normalized;
                enemy.FaceDirection(direction.x);
            }
        }

        public void ExitState(EnemyBase enemy)
        {
            // Attack finished
        }
    }

    /// <summary>
    /// 死亡状态
    /// </summary>
    public class EnemyDeathState : IEnemyState
    {
        public void EnterState(EnemyBase enemy)
        {
            enemy.StopMovement();
            enemy.HandleDeath();
        }

        public void UpdateState(EnemyBase enemy)
        {
            // Dead, no updates
        }

        public void FixedUpdateState(EnemyBase enemy)
        {
            // Dead, no physics
        }

        public void ExitState(EnemyBase enemy)
        {
            // Cannot exit death state
        }
    }
}
