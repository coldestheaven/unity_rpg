using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家输入控制器
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputController : Framework.Base.MonoBehaviourBase
    {
        private PlayerController controller;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<PlayerController>();
        }

        protected override void Update()
        {
            base.Update();
            ProduceCommands();
        }

        private void ProduceCommands()
        {
            if (controller == null)
            {
                return;
            }

            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            controller.EnqueueCommand(new MoveInputCommand(moveInput));
            controller.EnqueueCommand(new SetJumpHeldCommand(Input.GetButton("Jump")));

            if (Input.GetButtonDown("Jump"))
            {
                controller.EnqueueCommand(new JumpCommand());
            }

            if (Input.GetButtonDown("Fire1"))
            {
                controller.EnqueueCommand(new AttackCommand());
            }

            if (ReadButtonDown("Interact", KeyCode.E))
            {
                controller.EnqueueCommand(new InteractCommand());
            }

            if (ReadButtonDown("Skill1", KeyCode.Alpha1))
            {
                controller.EnqueueCommand(new UseSkillCommand(0));
            }
            else if (ReadButtonDown("Skill2", KeyCode.Alpha2))
            {
                controller.EnqueueCommand(new UseSkillCommand(1));
            }
            else if (ReadButtonDown("Skill3", KeyCode.Alpha3))
            {
                controller.EnqueueCommand(new UseSkillCommand(2));
            }
        }

        private bool ReadButtonDown(string buttonName, KeyCode fallbackKey)
        {
            try
            {
                if (Input.GetButtonDown(buttonName))
                {
                    return true;
                }
            }
            catch (System.Exception)
            {
                // Fall back to a keyboard binding when the legacy input axis is not defined.
            }

            return Input.GetKeyDown(fallbackKey);
        }
    }
}
