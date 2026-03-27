namespace Gameplay.Player
{
    public interface IPlayerSkillCaster
    {
        bool IsCastingSkill { get; }
        bool CanUseSkill(int slotIndex);
        bool TryUseSkill(int slotIndex);
    }

    public interface IPlayerInteractor
    {
        bool IsInteracting { get; }
        bool CanInteract();
        bool TryInteract();
    }
}
