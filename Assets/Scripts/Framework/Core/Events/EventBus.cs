using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Events
{
    /// <summary>
    /// Type-safe generic event bus implementing the Observer pattern.
    ///
    /// Use EventBus for all new code. It is a compile-time-checked alternative to the
    /// string-keyed EventManager: no magic strings, no object casting, no missed unsubscribes.
    ///
    /// Usage:
    ///   EventBus.Subscribe&lt;PlayerLevelUpEvent&gt;(OnLevelUp);
    ///   EventBus.Publish(new PlayerLevelUpEvent { NewLevel = 5 });
    ///   EventBus.Unsubscribe&lt;PlayerLevelUpEvent&gt;(OnLevelUp);
    ///
    /// Always unsubscribe in OnDisable / OnDestroy to avoid ghost listeners.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers =
            new Dictionary<Type, List<Delegate>>();

        /// <summary>Registers a handler. Duplicate registrations are silently ignored.</summary>
        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            var type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }

            if (!list.Contains(handler))
                list.Add(handler);
        }

        /// <summary>Removes a previously registered handler.</summary>
        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                    _handlers.Remove(typeof(TEvent));
            }
        }

        /// <summary>
        /// Publishes an event to all current subscribers.
        /// Dispatches to a snapshot, so listeners may unsubscribe themselves during the call.
        /// Per-listener exceptions are caught and logged so a broken handler never drops others.
        /// </summary>
        public static void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
                return;

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<TEvent>)handler)(gameEvent);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Exception in handler for {typeof(TEvent).Name}: {e}");
                }
            }
        }

        public static bool HasSubscribers<TEvent>() where TEvent : IGameEvent =>
            _handlers.TryGetValue(typeof(TEvent), out var list) && list.Count > 0;

        /// <summary>Clears all handlers for a specific event type.</summary>
        public static void Clear<TEvent>() where TEvent : IGameEvent =>
            _handlers.Remove(typeof(TEvent));

        /// <summary>Clears all registered handlers. Call on major scene transitions.</summary>
        public static void ClearAll() => _handlers.Clear();
    }
}
