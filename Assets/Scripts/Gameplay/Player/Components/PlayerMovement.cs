using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家移动系统
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : Framework.Base.MonoBehaviourBase, Framework.Interfaces.IMovable
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float fallMultiplier = 2.5f;

        [Header("Jump")]
        [SerializeField] private bool canDoubleJump = false;
        [SerializeField] private int maxJumps = 1;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;
        private PlayerInputController input;
        private SpriteRenderer spriteRenderer;

        private bool isGrounded = false;
        private int currentJumps = 0;
        private bool canMove = true;

        public bool IsGrounded => isGrounded;
        public bool IsMoving => rb != null && Mathf.Abs(rb.velocity.x) > 0.1f;

        public event System.Action OnJump;
        public event System.Action OnLand;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void Update()
        {
            base.Update();
            CheckGrounded();
            HandleJumpInput();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (canMove)
            {
                Move();
                ApplyJumpPhysics();
            }
        }

        public void Move(Vector3 direction = default)
        {
            if (input == null) return;

            Vector2 velocity = rb.velocity;
            velocity.x = input.MoveInput.x * moveSpeed;
            rb.velocity = velocity;

            HandleSpriteDirection();
        }

        public void Stop()
        {
            if (rb != null)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                currentJumps = maxJumps;
                OnLand?.Invoke();
            }
        }

        private void HandleJumpInput()
        {
            if (input != null && input.JumpPressed && canMove)
            {
                TryJump();
            }
        }

        private void TryJump()
        {
            if (isGrounded || currentJumps > 0)
            {
                PerformJump();
            }
        }

        private void PerformJump()
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            currentJumps--;
            OnJump?.Invoke();
        }

        private void ApplyJumpPhysics()
        {
            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.velocity.y > 0 && input != null && !input.JumpHeld)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
        }

        private void HandleSpriteDirection()
        {
            if (spriteRenderer != null && input != null && input.MoveInput.x != 0)
            {
                spriteRenderer.flipX = input.MoveInput.x < 0;
            }
        }

        public void AddKnockback(Vector3 direction, float force)
        {
            if (rb != null)
            {
                rb.AddForce(direction * force, ForceMode2D.Impulse);
            }
        }

        public void ResetMovement()
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            currentJumps = maxJumps;
            canMove = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}
