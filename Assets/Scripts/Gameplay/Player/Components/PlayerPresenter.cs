using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPresenter : Framework.Base.MonoBehaviourBase
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<PlayerController>();
            movement = GetComponent<PlayerMovement>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void Update()
        {
            base.Update();
            UpdateAnimation();
            UpdateFacing();
        }

        private void UpdateAnimation()
        {
            if (animator == null || controller == null)
            {
                return;
            }

            float horizontalSpeed = movement != null && movement.CanMove ? Mathf.Abs(controller.MoveInput.x) : 0f;
            animator.SetFloat("Speed", horizontalSpeed);
            animator.SetFloat("VerticalSpeed", movement != null ? movement.VerticalVelocity : 0f);
            animator.SetBool("IsGrounded", controller.IsGrounded);
            animator.SetBool("IsAttacking", controller.CurrentState == PlayerStateType.Attack);
            animator.SetBool("IsHurt", controller.CurrentState == PlayerStateType.Hurt);
            animator.SetBool("IsDead", controller.CurrentState == PlayerStateType.Dead);
            animator.SetInteger("State", (int)controller.CurrentState);
        }

        private void UpdateFacing()
        {
            if (spriteRenderer == null || controller == null)
            {
                return;
            }

            if (Mathf.Abs(controller.MoveInput.x) > 0.01f)
            {
                spriteRenderer.flipX = controller.MoveInput.x < 0f;
            }
        }
    }
}
