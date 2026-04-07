namespace Framework.Presentation
{
    /// <summary>
    /// Framework 层与进度系统（经验值/等级/金币）的通信契约。
    /// <see cref="PresentationContext.ProgressManager"/> 使用此接口，
    /// 使 Framework 无需直接引用 <c>RPG.Core.PlayerProgressManager</c>。
    /// </summary>
    public interface IPresentationProgressReceiver
    {
        void ApplyXPGained(float amount, float currentXP, float xpToNext);
        void ApplyLevelUp(int oldLevel, int newLevel, float xpToNext);
        void ApplyGoldChanged(int newTotal, int delta);
    }

    /// <summary>
    /// Framework 层与技能系统（冷却/法力）的通信契约。
    /// <see cref="PresentationContext.SkillController"/> 使用此接口，
    /// 使 Framework 无需直接引用 <c>RPG.Skills.SkillController</c>。
    /// </summary>
    public interface IPresentationSkillReceiver
    {
        void ApplyCooldownChanged(int slotIndex, float remainingSeconds);
        void ApplyManaChanged(float currentMana, float maxMana);
    }
}
