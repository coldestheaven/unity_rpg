using Framework.Diagnostics;
using UnityEngine;

namespace Framework.Presentation
{
    /// <summary>
    /// Drains <see cref="PresentationCommandQueue"/> every frame and routes each command
    /// to the correct presentation-layer handler.
    ///
    /// Threading contract (complete logic/presentation separation):
    ///   Logic thread  →  PresentationCommandQueue.Enqueue(cmd)  (lock-free, zero GC)
    ///   Main thread   ←  Update() dequeues and dispatches        (Unity thread only)
    ///
    /// The logic layer holds no references to MonoBehaviours or Unity APIs.
    /// The presentation layer holds no references to simulation internals.
    ///
    /// Placement:
    ///   Attach to the same DontDestroyOnLoad GameObject as GameManager (done in Awake).
    ///   Populate <see cref="Context"/> before the first frame — see GameManager.InitializeManagers().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationDispatcher : MonoBehaviour
    {
        /// <summary>Presentation-layer services available to command handlers.</summary>
        public PresentationContext Context { get; } = new PresentationContext();

        private void Update()
        {
            if (PresentationCommandQueue.Count == 0) return;

            using var _pm = ProfilerMarkers.PCQ_Flush.Auto();
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
                // ── Progress ──────────────────────────────────────────────────
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

                // ── Health (per-entity) ────────────────────────────────────────
                case PresCommandId.DamageResolved:
                    // I0=entityId, F0=finalDamage, F1=remainingHP,
                    // F2/F3/F4=sourcePos, I1=damageType, I2=hitKind
                    if (EntityPresentRegistry.TryGet(cmd.I0, out var damagedEntity))
                        damagedEntity.ApplyDamageResolved(cmd.F0, cmd.F1,
                            cmd.F2, cmd.F3, cmd.F4, cmd.I1, cmd.I2);
                    break;

                case PresCommandId.Healed:
                    // I0=entityId, F0=amount, F1=newHP
                    if (EntityPresentRegistry.TryGet(cmd.I0, out var healedEntity))
                        healedEntity.ApplyHealed(cmd.F0, cmd.F1);
                    break;

                case PresCommandId.EntityDied:
                    // I0=entityId, F0=killingDamage
                    if (EntityPresentRegistry.TryGet(cmd.I0, out var deadEntity))
                        deadEntity.ApplyEntityDied(cmd.F0);
                    break;

                case PresCommandId.DoTTick:
                    // I0=entityId, I1=remainingTicks, F0=tickDamage
                    if (EntityPresentRegistry.TryGet(cmd.I0, out var dotEntity))
                        dotEntity.ApplyDoTTick(cmd.F0, cmd.I1);
                    break;

                // ── Skills ────────────────────────────────────────────────────
                case PresCommandId.SkillCooldownChanged:
                    // I0=slotIndex, F0=remainingSeconds
                    Context.SkillController?.ApplyCooldownChanged(cmd.I0, cmd.F0);
                    break;

                case PresCommandId.ManaChanged:
                    // F0=currentMana, F1=maxMana
                    Context.SkillController?.ApplyManaChanged(cmd.F0, cmd.F1);
                    break;

                default:
                    Debug.LogWarning($"[PresentationDispatcher] No handler for command: {cmd.Id}");
                    break;
            }
        }
    }
}
