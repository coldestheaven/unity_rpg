using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家控制器 - 重构版
    /// 整合所有玩家子系统,提供统一接口
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : Framework.Base.MonoBehaviourBase
    {
        [Header("Components")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerInputController input;

        [Header("Visual")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public static PlayerController Instance { get; private set; }

        public PlayerMovement Movement => movement;
        public PlayerCombat Combat => combat;
        public PlayerHealth Health => health;
        public PlayerInputController Input => input;

        public Vector2 Position => transform.position;
        public bool IsGrounded => movement != null && movement.IsGrounded;
        public bool IsAlive => health != null && !health.IsDead;
        public bool IsAttacking => combat != null && combat.IsAttacking;

        public event System.Action OnPlayerInitialized;
        public event System.Action OnPlayerDestroyed;

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
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            input = GetComponent<PlayerInputController>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            SubscribeToEvents();
            OnPlayerInitialized?.Invoke();
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
            }

            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        protected override void Update()
        {
            base.Update();
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (animator == null || input == null) return;

            animator.SetFloat("Speed", Mathf.Abs(input.MoveInput.x));
            animator.SetFloat("VerticalSpeed", GetComponent<Rigidbody2D>()?.velocity.y ?? 0f);
            animator.SetBool("IsGrounded", IsGrounded);
        }

        private void HandleJump()
        {
            Framework.Events.EventManager.Instance?.TriggerEvent(Framework.Events.GameEvents.PLAYER_JUMPED);
        }

        private void HandleLand()
        {
            // Handle land logic
        }

        private void HandleAttack(int damage)
        {
            Framework.Events.EventManager.Instance?.TriggerEvent(Framework.Events.GameEvents.PLAYER_ATTACKED);
        }

        private void HandleDeath()
        {
            Debug.Log("Player died");
            Framework.Events.EventManager.Instance?.TriggerEvent(Framework.Events.GameEvents.PLAYER_DIED);
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
            combat?.Attack();
        }

        public void Knockback(Vector2 direction, float force)
        {
            movement?.AddKnockback(direction, force);
        }

        public void Revive(float healthAmount)
        {
            health?.Revive(healthAmount);
            movement?.ResetMovement();
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
            }

            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }
    }
}
