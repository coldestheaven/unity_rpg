using System.Collections.Generic;

namespace Framework.Presentation
{
    /// <summary>
    /// Maps entity instance IDs (Unity <c>GetInstanceID()</c>) to their
    /// <see cref="IEntityPresenter"/> so that <see cref="PresentationDispatcher"/>
    /// can route per-entity health commands without holding MonoBehaviour references.
    ///
    /// Threading:
    ///   Registration and lookup both occur on the Unity main thread, so the internal
    ///   dictionary requires no synchronisation.
    ///
    /// Usage:
    ///   DamageableBase.Awake()   → EntityPresentRegistry.Register(GetInstanceID(), this);
    ///   DamageableBase.OnDestroy → EntityPresentRegistry.Unregister(GetInstanceID());
    /// </summary>
    public static class EntityPresentRegistry
    {
        private static readonly Dictionary<int, IEntityPresenter> _map
            = new Dictionary<int, IEntityPresenter>(64);

        /// <summary>
        /// Registers or replaces the presenter for <paramref name="entityId"/>.
        /// Call from the entity's Awake on the main thread.
        /// </summary>
        public static void Register(int entityId, IEntityPresenter presenter)
            => _map[entityId] = presenter;

        /// <summary>
        /// Removes the presenter for <paramref name="entityId"/>.
        /// Call from the entity's OnDestroy on the main thread.
        /// </summary>
        public static void Unregister(int entityId)
            => _map.Remove(entityId);

        /// <summary>Attempts to retrieve the presenter for <paramref name="entityId"/>.</summary>
        internal static bool TryGet(int entityId, out IEntityPresenter presenter)
            => _map.TryGetValue(entityId, out presenter);
    }
}
