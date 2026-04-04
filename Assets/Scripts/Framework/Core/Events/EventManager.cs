using Framework.Core.Patterns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Events
{
    public class EventManager : Singleton<EventManager>
    {
        private readonly Dictionary<string, List<Action<object>>> eventDictionary =
            new Dictionary<string, List<Action<object>>>();

        public void AddListener(string eventName, Action<object> listener)
        {
            if (!eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] = new List<Action<object>>();
            }
            eventDictionary[eventName].Add(listener);
        }

        public void RemoveListener(string eventName, Action<object> listener)
        {
            if (eventDictionary.TryGetValue(eventName, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    eventDictionary.Remove(eventName);
                }
            }
        }

        public void TriggerEvent(string eventName, object data = null)
        {
            if (!eventDictionary.TryGetValue(eventName, out var listeners)) return;

            // Snapshot prevents InvalidOperationException if a listener removes itself
            var snapshot = new List<Action<object>>(listeners);
            foreach (var listener in snapshot)
            {
                try
                {
                    listener.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventManager] Exception in listener for '{eventName}': {e}");
                }
            }
        }

        public bool HasListeners(string eventName) =>
            eventDictionary.TryGetValue(eventName, out var l) && l.Count > 0;

        private void OnDestroy()
        {
            eventDictionary.Clear();
        }
    }
}
