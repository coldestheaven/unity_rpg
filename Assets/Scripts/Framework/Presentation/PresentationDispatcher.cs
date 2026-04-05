using UnityEngine;

namespace Framework.Presentation
{
    /// <summary>
    /// Drains <see cref="PresentationCommandQueue"/> every frame and routes each command
    /// to the correct presentation-layer handler.
    ///
    /// Threading contract:
    ///   Logic thread  →  PresentationCommandQueue.Enqueue(cmd)  (lock-free)
    ///   Main thread   ←  Update() dequeues and dispatches       (Unity thread only)
    ///
    /// Placement:
    ///   Attach to the same DontDestroyOnLoad GameObject as GameManager.
    ///   Populate <see cref="Context"/> before the first frame via
    ///   <c>PresentationDispatcher.Context.ProgressManager = ...</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationDispatcher : MonoBehaviour
    {
        /// <summary>Presentation-layer services available to command handlers.</summary>
        public PresentationContext Context { get; } = new PresentationContext();

        private void Update()
        {
            while (PresentationCommandQueue.TryDequeue(out PresentationCommand cmd))
            {
                try
                {
                    Dispatch(in cmd);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PresentationDispatcher] Exception handling {cmd.Id}: {e}");
                }
            }
        }

        // ── Command routing ───────────────────────────────────────────────────

        private void Dispatch(in PresentationCommand cmd)
        {
            switch (cmd.Id)
            {
                case PresCommandId.XPGained:
                    Context.ProgressManager?.ApplyXPGained(cmd.F0, cmd.F1, cmd.F2);
                    break;

                case PresCommandId.LevelUp:
                    // I0=oldLevel, I1=newLevel, F0=xpToNext
                    Context.ProgressManager?.ApplyLevelUp(cmd.I0, cmd.I1, cmd.F0);
                    break;

                case PresCommandId.GoldChanged:
                    // I0=newTotal, I1=delta
                    Context.ProgressManager?.ApplyGoldChanged(cmd.I0, cmd.I1);
                    break;

                // Health commands: currently handled in-entity via HealthSimulation
                // events.  These slots are reserved for future migration.
                case PresCommandId.DamageResolved:
                case PresCommandId.Healed:
                case PresCommandId.EntityDied:
                case PresCommandId.DoTTick:
                    break;

                case PresCommandId.SkillCooldownChanged:
                    break;

                default:
                    Debug.LogWarning($"[PresentationDispatcher] No handler for command: {cmd.Id}");
                    break;
            }
        }
    }
}
