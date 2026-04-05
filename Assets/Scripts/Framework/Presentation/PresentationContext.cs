using RPG.Core;
using UI.Controllers;

namespace Framework.Presentation
{
    /// <summary>
    /// Mutable bag of presentation-layer service references injected into
    /// <see cref="PresentationDispatcher"/> at startup.
    ///
    /// Command handlers receive this context to update game state and fire
    /// EventBus/UI notifications without holding static references themselves.
    /// </summary>
    public sealed class PresentationContext
    {
        public PlayerProgressManager ProgressManager { get; set; }
        public UIManager             UIManager       { get; set; }
    }
}
