using UnityEngine;
using System;
using System.Collections.Generic;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家控制器 - 重构版
    /// 整合所有玩家子系统,提供统一接口
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerPresenter))]
    [RequireComponent(typeof(PlayerStatsRuntime))]
    [RequireComponent(typeof(PlayerBuffController))]
    public class PlayerController : Framework.Base.MonoBehaviourBase
    {
        [Header("Components")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerInputController input;

        private PlayerStateMachine playerStateMachine;
        private IPlayerSkillCaster skillCaster;
        private IPlayerInteractor interactor;
        private readonly Queue<IPlayerCommand> pendingCommands = new Queue<IPlayerCommand>();
        private readonly PlayerCommandContext commandContext = new PlayerCommandContext();

        public static PlayerController Instance { get; private set; }

        public PlayerMovement Movement => movement;
        public PlayerCombat Combat => combat;
        public PlayerHealth Health => health;
        public PlayerInputController Input => input;
        public PlayerCommandContext Commands => commandContext;
        public PlayerStateType CurrentState => playerStateMachine != null ? playerStateMachine.CurrentStateType : PlayerStateType.Idle;
        public string CurrentStateName => playerStateMachine != null ? playerStateMachine.CurrentStateName : "Uninitialized";
        public bool IsUsingSkill => skillCaster != null && skillCaster.IsCastingSkill;
        public bool IsInteracting => interactor != null && interactor.IsInteracting;
        public float AttackDamage => combat != null ? combat.CurrentDamage : 0f;
        public float MoveSpeed => movement != null ? movement.MoveSpeed : 0f;
        public float Defense => health != null ? health.Defense : 0f;
        public Vector2 MoveInput => commandContext.MoveInput;
        public bool JumpPressed => commandContext.JumpPressed;
        public bool JumpHeld => commandContext.JumpHeld;
        public bool AttackRequested => commandContext.AttackRequested;
        public bool InteractRequested => commandContext.InteractRequested;
        public bool SkillRequested => commandContext.SkillRequested;
        public int RequestedSkillSlot => commandContext.RequestedSkillSlot;
        public bool HasMovementInput => commandContext.HasMovementInput;

        public Vector2 Position => transform.position;
        public bool IsGrounded => movement != null && movement.IsGrounded;
        public bool IsAlive => health != null && !health.IsDead;
        public bool IsAttacking => combat != null && combat.IsAttacking;

        public event System.Action OnPlayerInitialized;
        public event System.Action OnPlayerDestroyed;
        public event System.Action<PlayerStateType, PlayerStateType> OnStateChanged;

        protected override void Awake()
        {
            base.Awake();

            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            EnsurePresentationComponent();
            EnsureStatsRuntimeComponent();
            EnsureBuffComponent();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            input = GetComponent<PlayerInputController>();
            skillCaster = FindOptionalInterface<IPlayerSkillCaster>();
            interactor = FindOptionalInterface<IPlayerInteractor>();
            playerStateMachine = new PlayerStateMachine(this);

            SubscribeToEvents();
            playerStateMachine.Initialize();
            OnPlayerInitialized?.Invoke();
        }

        private void EnsurePresentationComponent()
        {
            if (GetComponent<PlayerPresenter>() == null && GetComponent<Animator>() != null)
            {
                // Keep existing prefabs working while presentation is split out of the controller.
                gameObject.AddComponent<PlayerPresenter>();
            }
        }

        private void EnsureStatsRuntimeComponent()
        {
            if (GetComponent<PlayerStatsRuntime>() == null)
            {
                gameObject.AddComponent<PlayerStatsRuntime>();
            }
        }

        private void EnsureBuffComponent()
        {
            if (GetComponent<PlayerBuffController>() == null)
            {
                gameObject.AddComponent<PlayerBuffController>();
            }
        }

        private void SubscribeToEvents()
        {
            if (movement != null)
            {
                movement.OnJump += HandleJump;
                movement.OnLand += HandleLand;
            }

            if (combat != null)
            {
                combat.OnAttack += HandleAttack;
                combat.OnAttackFinished += HandleAttackFinished;
            }

            if (health != null)
            {
                health.OnDeath += HandleDeath;
                health.OnDamaged += HandleDamaged;
            }

            if (playerStateMachine != null)
            {
                playerStateMachine.OnStateChanged += HandleStateChanged;
            }
        }

        protected override void Update()
        {
            base.Update();
            ConsumePendingCommands();
            playerStateMachine?.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            playerStateMachine?.FixedUpdate();
        }

        public void EnqueueCommand(IPlayerCommand command)
        {
            if (command == null)
            {
                return;
            }

            pendingCommands.Enqueue(command);
        }

        private void ConsumePendingCommands()
        {
            commandContext.BeginFrame();

            while (pendingCommands.Count > 0)
            {
                pendingCommands.Dequeue().Execute(commandContext);
            }
        }

        private void HandleJump()
        {
            Framework.Events.EventBus.Publish(new Framework.Events.PlayerJumpedEvent());
        }

        private void HandleLand()
        {
            playerStateMachine?.EvaluateGlobalTransitions();
        }

        private void HandleAttack(int damage)
        {
            Framework.Events.EventBus.Publish(new Framework.Events.PlayerAttackedEvent(damage));
        }

        private void HandleAttackFinished()
        {
            playerStateMachine?.NotifyAttackFinished();
            playerStateMachine?.EvaluateGlobalTransitions();
        }

        private void HandleDamaged(float damage)
        {
            if (!IsAlive) return;
            playerStateMachine?.ForceState(PlayerStateType.Hurt);
        }

        private void HandleDeath()
        {
            Debug.Log("Player died");
            playerStateMachine?.ForceState(PlayerStateType.Dead);
            Framework.Events.EventBus.Publish(new Framework.Events.PlayerDiedEvent(transform.position));
        }

        private void HandleStateChanged(PlayerStateType previousState, PlayerStateType nextState)
        {
            OnStateChanged?.Invoke(previousState, nextState);
        }

        public void TakeDamage(float damage, Vector2 attackerPosition)
        {
            health?.TakeDamage(damage, attackerPosition);
        }

        public void Heal(float amount)
        {
            health?.Heal(amount);
        }

        public void Attack()
        {
            if (!IsAlive || combat == null || !combat.CanAttack) return;
            playerStateMachine?.ForceState(PlayerStateType.Attack);
        }

        public bool CanUseRequestedSkill()
        {
            return SkillRequested
                && skillCaster != null
                && skillCaster.CanUseSkill(RequestedSkillSlot);
        }

        public bool TryUseRequestedSkill()
        {
            return SkillRequested
                && skillCaster != null
                && skillCaster.TryUseSkill(RequestedSkillSlot);
        }

        public bool CanInteract()
        {
            return interactor != null && interactor.CanInteract();
        }

        public bool TryInteract()
        {
            return interactor != null && interactor.TryInteract();
        }

        public void Knockback(Vector2 direction, float force)
        {
            movement?.AddKnockback(direction, force);
        }

        public void Revive(float healthAmount)
        {
            health?.Revive(healthAmount);
            movement?.ResetMovement();
            ResetCommands();
            playerStateMachine?.ForceState(PlayerStateType.Idle);
        }

        public void SetCanMove(bool canMove)
        {
            movement?.SetCanMove(canMove);
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            movement?.SetMoveSpeed(moveSpeed);
        }

        public void SetAttackDamage(float damage)
        {
            combat?.SetAttackDamage(Mathf.RoundToInt(damage));
        }

        public void SetDefense(float value)
        {
            health?.SetDefense(value);
        }

        public void SetMaxHealth(float value, bool restoreToMax = false)
        {
            health?.SetMaxHealth(value, restoreToMax);
        }

        public void ResetPlayer()
        {
            health?.ResetHealth();
            movement?.ResetMovement();
            combat?.ResetCombat();
            ResetCommands();
            playerStateMachine?.ForceState(PlayerStateType.Idle);
        }

        private void ResetCommands()
        {
            pendingCommands.Clear();
            commandContext.ResetAll();
        }

        private T FindOptionalInterface<T>() where T : class
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is T typed)
                {
                    return typed;
                }
            }

            return null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
            OnPlayerDestroyed?.Invoke();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (movement != null)
            {
                movement.OnJump -= HandleJump;
                movement.OnLand -= HandleLand;
            }

            if (combat != null)
            {
                combat.OnAttack -= HandleAttack;
                combat.OnAttackFinished -= HandleAttackFinished;
            }

            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnDamaged -= HandleDamaged;
            }

            if (playerStateMachine != null)
            {
                playerStateMachine.OnStateChanged -= HandleStateChanged;
            }
        }
    }
}
