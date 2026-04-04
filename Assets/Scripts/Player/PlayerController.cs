using UnityEngine;
using RPG.Core;
using Framework.Events;

namespace RPG.Player
{
    /// <summary>
    /// 玩家控制器 - 重构版
    /// 整合所有玩家子系统,提供统一接口
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("引用")]
        public PlayerMovement movement;
        public PlayerCombat combat;
        public PlayerHealth health;
        public PlayerInput input;
        public PlayerState state;

        [Header("动画")]
        public Animator animator;

        private SpriteRenderer spriteRenderer;
        private bool isInitialized = false;

        public static PlayerController Instance { get; private set; }

        public Vector2 Position => transform.position;
        public bool IsGrounded => movement != null && movement.IsGrounded;
        public bool IsAlive => health != null && health.IsAlive();
        public bool IsAttacking => combat != null && combat.IsAttacking;
        public bool CanMove => movement != null && movement.CanMove;

        public event System.Action OnPlayerInitialized;
        public event System.Action OnPlayerDestroyed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            input = GetComponent<PlayerInput>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            state = GetComponent<PlayerState>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            ValidateComponents();
            SubscribeToEvents();
            isInitialized = true;

            OnPlayerInitialized?.Invoke();

            EventManager.Instance?.TriggerEvent("PlayerInitialized", null);
        }

        private void ValidateComponents()
        {
            if (input == null)
            {
                Debug.LogError("PlayerController: PlayerInput component missing!");
            }

            if (movement == null)
            {
                Debug.LogError("PlayerController: PlayerMovement component missing!");
            }

            if (combat == null)
            {
                Debug.LogError("PlayerController: PlayerCombat component missing!");
            }

            if (health == null)
            {
                Debug.LogError("PlayerController: PlayerHealth component missing!");
            }

            if (animator == null)
            {
                Debug.LogError("PlayerController: Animator component missing!");
            }
        }

        private void SubscribeToEvents()
        {
            if (movement != null)
            {
                movement.OnJump += HandleJump;
                movement.OnLand += HandleLand;
                movement.OnGroundedChanged += HandleGroundedChanged;
            }

            if (combat != null)
            {
                combat.OnAttack += HandleAttack;
                combat.OnHit += HandleHit;
            }

            if (health != null)
            {
                health.OnPlayerDeath += HandleDeath;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (movement != null)
            {
                movement.OnJump -= HandleJump;
                movement.OnLand -= HandleLand;
                movement.OnGroundedChanged -= HandleGroundedChanged;
            }

            if (combat != null)
            {
                combat.OnAttack -= HandleAttack;
                combat.OnHit -= HandleHit;
            }

            if (health != null)
            {
                health.OnPlayerDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (animator == null || input == null) return;

            animator.SetFloat("Speed", Mathf.Abs(input.MoveInput.x));
            animator.SetFloat("VerticalSpeed", GetComponent<Rigidbody2D>()?.velocity.y ?? 0f);
            animator.SetBool("IsGrounded", IsGrounded);
        }

        #region Event Handlers

        private void HandleJump()
        {
            Debug.Log("Player jumped");
        }

        private void HandleLand()
        {
            Debug.Log("Player landed");
        }

        private void HandleGroundedChanged(bool grounded)
        {
            Debug.Log($"Player grounded: {grounded}");
        }

        private void HandleAttack(int combo)
        {
            Debug.Log($"Player attack combo: {combo}");
        }

        private void HandleHit(int damage)
        {
            Debug.Log($"Player dealt damage: {damage}");
        }

        private void HandleDeath()
        {
            Debug.Log("Player died");
            SetCanMove(false);
        }

        #endregion

        #region Public Methods

        public void TakeDamage(int damage, Vector2 attackerPosition)
        {
            health?.TakeDamage(damage, attackerPosition);
        }

        public void Heal(int amount)
        {
            health?.Heal(amount);
        }

        public void Attack()
        {
            combat?.Attack();
        }

        public void Knockback(Vector2 direction, float force)
        {
            movement?.AddKnockback(direction, force);
        }

        public void SetMoveSpeed(float speed)
        {
            movement?.SetMoveSpeed(speed);
        }

        public void SetCanMove(bool canMove)
        {
            movement?.SetCanMove(canMove);
        }

        public void SetAttackDamage(int damage)
        {
            combat?.SetAttackDamage(damage);
        }

        public void Revive(int healthAmount)
        {
            health?.Revive(healthAmount);
            SetCanMove(true);
        }

        public void ResetPlayer()
        {
            health?.ResetHealth();
            movement?.ResetMovement();
            combat?.ResetCombat();
            input?.ResetInput();
        }

        #endregion

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            OnPlayerDestroyed?.Invoke();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (movement != null)
            {
                // Handled by PlayerMovement
            }

            if (combat != null)
            {
                // Handled by PlayerCombat
            }
        }
    }
}
