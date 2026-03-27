using UnityEngine;

namespace Gameplay.Player
{
    public interface IPlayerCommand
    {
        void Execute(PlayerCommandContext context);
    }

    public sealed class PlayerCommandContext
    {
        public Vector2 MoveInput { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool AttackRequested { get; private set; }
        public bool InteractRequested { get; private set; }
        public int RequestedSkillSlot { get; private set; } = -1;

        public bool HasMovementInput => Mathf.Abs(MoveInput.x) > 0.01f || Mathf.Abs(MoveInput.y) > 0.01f;
        public bool SkillRequested => RequestedSkillSlot >= 0;

        public void BeginFrame()
        {
            MoveInput = Vector2.zero;
            JumpHeld = false;
            JumpPressed = false;
            AttackRequested = false;
            InteractRequested = false;
            RequestedSkillSlot = -1;
        }

        public void ResetAll()
        {
            BeginFrame();
        }

        public void SetMoveInput(Vector2 value)
        {
            MoveInput = value;
        }

        public void SetJumpHeld(bool value)
        {
            JumpHeld = value;
        }

        public void RequestJump()
        {
            JumpPressed = true;
        }

        public void RequestAttack()
        {
            AttackRequested = true;
        }

        public void RequestInteract()
        {
            InteractRequested = true;
        }

        public void RequestSkill(int slotIndex)
        {
            RequestedSkillSlot = slotIndex;
        }
    }

    public sealed class MoveInputCommand : IPlayerCommand
    {
        private readonly Vector2 moveInput;

        public MoveInputCommand(Vector2 moveInput)
        {
            this.moveInput = moveInput;
        }

        public void Execute(PlayerCommandContext context)
        {
            context.SetMoveInput(moveInput);
        }
    }

    public sealed class SetJumpHeldCommand : IPlayerCommand
    {
        private readonly bool jumpHeld;

        public SetJumpHeldCommand(bool jumpHeld)
        {
            this.jumpHeld = jumpHeld;
        }

        public void Execute(PlayerCommandContext context)
        {
            context.SetJumpHeld(jumpHeld);
        }
    }

    public sealed class JumpCommand : IPlayerCommand
    {
        public void Execute(PlayerCommandContext context)
        {
            context.RequestJump();
        }
    }

    public sealed class AttackCommand : IPlayerCommand
    {
        public void Execute(PlayerCommandContext context)
        {
            context.RequestAttack();
        }
    }

    public sealed class InteractCommand : IPlayerCommand
    {
        public void Execute(PlayerCommandContext context)
        {
            context.RequestInteract();
        }
    }

    public sealed class UseSkillCommand : IPlayerCommand
    {
        private readonly int slotIndex;

        public UseSkillCommand(int slotIndex)
        {
            this.slotIndex = slotIndex;
        }

        public void Execute(PlayerCommandContext context)
        {
            context.RequestSkill(slotIndex);
        }
    }
}
