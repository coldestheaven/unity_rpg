namespace Framework.Presentation
{
    /// <summary>
    /// Mutable bag of presentation-layer service references injected into
    /// <see cref="PresentationDispatcher"/> at startup.
    ///
    /// 所有字段均使用 Framework 内部接口，不引用任何游戏命名空间
    /// （<c>RPG.Core</c>、<c>RPG.Skills</c>、<c>UI.Controllers</c> 等），
    /// 保持 Framework 层的可测试性和可移植性。
    /// </summary>
    public sealed class PresentationContext
    {
        /// <summary>处理经验值、等级、金币变化命令的接收方。</summary>
        public IPresentationProgressReceiver ProgressManager  { get; set; }

        /// <summary>处理技能冷却、法力变化命令的接收方。</summary>
        public IPresentationSkillReceiver    SkillController  { get; set; }
    }
}
