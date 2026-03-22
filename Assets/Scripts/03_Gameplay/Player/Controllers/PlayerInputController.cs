using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家输入控制器
    /// </summary>
    public class PlayerInputController : Framework.Base.MonoBehaviourBase
    {
        private Vector2 moveInput;
        private bool jumpPressed = false;
        private bool jumpHeld = false;
        private bool attackPressed = false;

        public Vector2 MoveInput => moveInput;
        public bool JumpPressed => jumpPressed;
        public bool JumpHeld => jumpHeld;
        public bool AttackPressed => attackPressed;

        protected override void Update()
        {
            base.Update();
            ReadInput();
        }

        private void LateUpdate()
        {
            ResetInputFlags();
        }

        private void ReadInput()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Input.GetButtonDown("Jump"))
            {
                jumpPressed = true;
            }

            jumpHeld = Input.GetButton("Jump");

            if (Input.GetButtonDown("Fire1"))
            {
                attackPressed = true;
            }
        }

        private void ResetInputFlags()
        {
            jumpPressed = false;
            attackPressed = false;
        }
    }
}
