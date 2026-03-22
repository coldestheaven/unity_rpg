using UnityEngine;

namespace RPG.Player
{
    /// <summary>
    /// 玩家移动系统 - 从PlayerController中分离
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5f;
        public float jumpForce = 10f;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;

        [Header("跳跃设置")]
        public bool canJump = true;
        public bool canDoubleJump = false;
        public int maxJumps = 1;

        [Header("地面检测")]
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;
        private PlayerInput input;
        private SpriteRenderer spriteRenderer;

        private bool isGrounded;
        private int currentJumps;
        private bool canMove = true;

        public bool IsGrounded => isGrounded;
        public bool CanMove => canMove;

        public event System.Action OnJump;
        public event System.Action OnLand;
        public event System.Action<bool> OnGroundedChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInput>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            CheckGrounded();
            HandleJumpInput();
        }

        private void FixedUpdate()
        {
            Move();
            ApplyJumpPhysics();
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

            if (isGrounded != wasGrounded)
            {
                OnGroundedChanged?.Invoke(isGrounded);
            }
        }

        private void HandleJumpInput()
        {
            if (input.JumpPressed && canMove)
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

            EventManager.Instance?.TriggerEvent("PlayerJumped", null);
        }

        private void Move()
        {
            if (!canMove) return;

            Vector2 velocity = rb.velocity;
            velocity.x = input.MoveInput.x * moveSpeed;
            rb.velocity = velocity;

            HandleSpriteDirection();
        }

        private void HandleSpriteDirection()
        {
            if (input.MoveInput.x != 0)
            {
                spriteRenderer.flipX = input.MoveInput.x < 0;
            }
        }

        private void ApplyJumpPhysics()
        {
            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.velocity.y > 0 && !input.JumpHeld)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        public void SetCanMove(bool value)
        {
            canMove = value;
            if (!canMove)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        public void AddKnockback(Vector2 direction, float force)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        public void ResetMovement()
        {
            rb.velocity = Vector2.zero;
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
