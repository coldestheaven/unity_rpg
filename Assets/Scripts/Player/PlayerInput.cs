using UnityEngine;

namespace RPG.Player
{
    /// <summary>
    /// 玩家输入处理器 - 统一管理所有输入
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool Skill1Pressed { get; private set; }
        public bool Skill2Pressed { get; private set; }
        public bool Skill3Pressed { get; private set; }

        private void Update()
        {
            HandleMovementInput();
            HandleActionInput();
        }

        private void HandleMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(horizontal, vertical).normalized;

            JumpPressed = Input.GetButtonDown("Jump");
            JumpHeld = Input.GetButton("Jump");
        }

        private void HandleActionInput()
        {
            AttackPressed = Input.GetButtonDown("Fire1");
            InteractPressed = Input.GetButtonDown("Interact");
            DashPressed = Input.GetButtonDown("Dash");
            Skill1Pressed = Input.GetButtonDown("Skill1");
            Skill2Pressed = Input.GetButtonDown("Skill2");
            Skill3Pressed = Input.GetButtonDown("Skill3");
        }

        public void ResetInput()
        {
            JumpPressed = false;
            AttackPressed = false;
            InteractPressed = false;
            DashPressed = false;
            Skill1Pressed = false;
            Skill2Pressed = false;
            Skill3Pressed = false;
        }
    }
}
