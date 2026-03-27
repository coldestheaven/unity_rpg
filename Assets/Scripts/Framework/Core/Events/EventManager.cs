using Framework.Core.Patterns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Events
{
    public class EventManager : Singleton<EventManager>
    {
        private Dictionary<string, List<Action<object>>> eventDictionary = new Dictionary<string, List<Action<object>>>();

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
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName].Remove(listener);
                if (eventDictionary[eventName].Count == 0)
                {
                    eventDictionary.Remove(eventName);
                }
            }
        }

        public void TriggerEvent(string eventName, object data = null)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                foreach (var listener in eventDictionary[eventName])
                {
                    listener.Invoke(data);
                }
            }
        }

        private void OnDestroy()
        {
            eventDictionary.Clear();
        }
    }
}
