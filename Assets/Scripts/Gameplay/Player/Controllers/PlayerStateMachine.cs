using Framework.Core.StateMachine;
using UnityEngine;

namespace Gameplay.Player
{
    public enum PlayerStateType
    {
        Idle,
        Move,
        Jump,
        Fall,
        Hurt,
        Interact,
        Skill,
        Attack,
        Dead
    }

    public class PlayerStateMachine
    {
        private readonly StateMachine stateMachine = new StateMachine();
        private readonly PlayerController controller;

        private readonly PlayerIdleState idleState;
        private readonly PlayerMoveState moveState;
        private readonly PlayerJumpState jumpState;
        private readonly PlayerFallState fallState;
        private readonly PlayerHurtState hurtState;
        private readonly PlayerInteractState interactState;
        private readonly PlayerSkillState skillState;
        private readonly PlayerAttackState attackState;
        private readonly PlayerDeadState deadState;
        private bool attackStatePendingExit;

        public event System.Action<PlayerStateType, PlayerStateType> OnStateChanged;

        public PlayerStateType CurrentStateType { get; private set; }
        public string CurrentStateName => CurrentStateType.ToString();

        public PlayerStateMachine(PlayerController controller)
        {
            this.controller = controller;

            idleState = new PlayerIdleState(this, controller);
            moveState = new PlayerMoveState(this, controller);
            jumpState = new PlayerJumpState(this, controller);
            fallState = new PlayerFallState(this, controller);
            hurtState = new PlayerHurtState(this, controller);
            interactState = new PlayerInteractState(this, controller);
            skillState = new PlayerSkillState(this, controller);
            attackState = new PlayerAttackState(this, controller);
            deadState = new PlayerDeadState(this, controller);
        }

        public void Initialize()
        {
            ChangeState(PlayerStateType.Idle);
        }

        public void Update()
        {
            stateMachine.Update();
        }

        public void FixedUpdate()
        {
            stateMachine.FixedUpdate();
        }

        public void ForceState(PlayerStateType stateType)
        {
            ChangeState(stateType);
        }

        public void NotifyAttackFinished()
        {
            attackStatePendingExit = true;
        }

        public void EvaluateGlobalTransitions()
        {
            if (!controller.IsAlive)
            {
                ChangeState(PlayerStateType.Dead);
                return;
            }

            if (CurrentStateType == PlayerStateType.Hurt)
            {
                return;
            }

            if (CurrentStateType == PlayerStateType.Interact && controller.IsInteracting)
            {
                return;
            }

            if (CurrentStateType == PlayerStateType.Skill && controller.IsUsingSkill)
            {
                return;
            }

            if (CurrentStateType == PlayerStateType.Attack && !attackStatePendingExit)
            {
                return;
            }

            if (controller.InteractRequested && controller.CanInteract())
            {
                ChangeState(PlayerStateType.Interact);
                return;
            }

            if (controller.CanUseRequestedSkill())
            {
                ChangeState(PlayerStateType.Skill);
                return;
            }

            if (controller.AttackRequested && controller.Combat != null && controller.Combat.CanAttack)
            {
                ChangeState(PlayerStateType.Attack);
                return;
            }

            if (!controller.IsGrounded)
            {
                if (controller.Movement != null && controller.Movement.VerticalVelocity > 0.05f)
                {
                    ChangeState(PlayerStateType.Jump);
                }
                else
                {
                    ChangeState(PlayerStateType.Fall);
                }
                return;
            }

            if (controller.HasMovementInput)
            {
                ChangeState(PlayerStateType.Move);
                return;
            }

            ChangeState(PlayerStateType.Idle);
        }

        private void ChangeState(PlayerStateType stateType)
        {
            if (CurrentStateType == stateType && stateMachine.CurrentState != null)
            {
                return;
            }

            PlayerStateType previousState = CurrentStateType;
            CurrentStateType = stateType;
            if (stateType != PlayerStateType.Attack)
            {
                attackStatePendingExit = false;
            }

            IState nextState = GetState(stateType);

            if (stateMachine.CurrentState == null)
            {
                stateMachine.Initialize(nextState);
            }
            else
            {
                stateMachine.TransitionTo(nextState);
            }

            OnStateChanged?.Invoke(previousState, stateType);
        }

        private IState GetState(PlayerStateType stateType)
        {
            switch (stateType)
            {
                case PlayerStateType.Move:
                    return moveState;
                case PlayerStateType.Jump:
                    return jumpState;
                case PlayerStateType.Fall:
                    return fallState;
                case PlayerStateType.Hurt:
                    return hurtState;
                case PlayerStateType.Interact:
                    return interactState;
                case PlayerStateType.Skill:
                    return skillState;
                case PlayerStateType.Attack:
                    return attackState;
                case PlayerStateType.Dead:
                    return deadState;
                default:
                    return idleState;
            }
        }
    }

    public abstract class PlayerStateBase : State
    {
        protected readonly PlayerStateMachine playerStateMachine;
        protected readonly PlayerController controller;

        protected PlayerStateBase(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(null, controller.gameObject)
        {
            this.playerStateMachine = playerStateMachine;
            this.controller = controller;
        }

        protected void EvaluateSharedTransitions()
        {
            playerStateMachine.EvaluateGlobalTransitions();
        }
    }

    public class PlayerIdleState : PlayerStateBase
    {
        public PlayerIdleState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            controller.Movement?.SetCanMove(true);
        }

        public override void Update()
        {
            EvaluateSharedTransitions();
        }
    }

    public class PlayerMoveState : PlayerStateBase
    {
        public PlayerMoveState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            controller.Movement?.SetCanMove(true);
        }

        public override void Update()
        {
            EvaluateSharedTransitions();
        }
    }

    public class PlayerJumpState : PlayerStateBase
    {
        public PlayerJumpState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            controller.Movement?.SetCanMove(true);
        }

        public override void Update()
        {
            EvaluateSharedTransitions();
        }
    }

    public class PlayerFallState : PlayerStateBase
    {
        public PlayerFallState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            controller.Movement?.SetCanMove(true);
        }

        public override void Update()
        {
            EvaluateSharedTransitions();
        }
    }

    public class PlayerAttackState : PlayerStateBase
    {
        private bool attackTriggered;

        public PlayerAttackState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            attackTriggered = false;
            controller.Movement?.SetCanMove(false);

            if (controller.Combat != null && controller.Combat.CanAttack)
            {
                controller.Combat.Attack();
                attackTriggered = true;
            }
        }

        public override void Update()
        {
            if (!attackTriggered || controller.Combat == null || !controller.Combat.IsAttacking)
            {
                playerStateMachine.EvaluateGlobalTransitions();
            }
        }

        public override void Exit()
        {
            controller.Movement?.SetCanMove(true);
        }
    }

    public class PlayerHurtState : PlayerStateBase
    {
        private const float HurtDuration = 0.2f;
        private float elapsed;

        public PlayerHurtState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            elapsed = 0f;
            controller.Movement?.SetCanMove(false);
        }

        public override void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= HurtDuration)
            {
                playerStateMachine.EvaluateGlobalTransitions();
            }
        }

        public override void Exit()
        {
            if (controller.IsAlive)
            {
                controller.Movement?.SetCanMove(true);
            }
        }
    }

    public class PlayerInteractState : PlayerStateBase
    {
        private const float InteractFallbackDuration = 0.15f;
        private float elapsed;
        private bool interactionStarted;

        public PlayerInteractState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            elapsed = 0f;
            interactionStarted = controller.TryInteract();
            controller.Movement?.SetCanMove(false);
        }

        public override void Update()
        {
            elapsed += Time.deltaTime;

            if ((!interactionStarted || !controller.IsInteracting) && elapsed >= InteractFallbackDuration)
            {
                playerStateMachine.EvaluateGlobalTransitions();
            }
        }

        public override void Exit()
        {
            if (controller.IsAlive)
            {
                controller.Movement?.SetCanMove(true);
            }
        }
    }

    public class PlayerSkillState : PlayerStateBase
    {
        private const float SkillFallbackDuration = 0.15f;
        private float elapsed;
        private bool skillTriggered;

        public PlayerSkillState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            elapsed = 0f;
            skillTriggered = controller.TryUseRequestedSkill();
            controller.Movement?.SetCanMove(false);
        }

        public override void Update()
        {
            elapsed += Time.deltaTime;

            if ((!skillTriggered || !controller.IsUsingSkill) && elapsed >= SkillFallbackDuration)
            {
                playerStateMachine.EvaluateGlobalTransitions();
            }
        }

        public override void Exit()
        {
            if (controller.IsAlive)
            {
                controller.Movement?.SetCanMove(true);
            }
        }
    }

    public class PlayerDeadState : PlayerStateBase
    {
        public PlayerDeadState(PlayerStateMachine playerStateMachine, PlayerController controller)
            : base(playerStateMachine, controller)
        {
        }

        public override void Enter()
        {
            controller.Movement?.SetCanMove(false);
        }
    }
}
