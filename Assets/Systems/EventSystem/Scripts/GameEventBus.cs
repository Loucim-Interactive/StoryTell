using System;
using System.Collections.Generic;

namespace EventSystem.Scripts {
    public static class GameEventBus
    {
        private static readonly Dictionary<string, Delegate> Events = new();

        #region Payload Methods

        public static void Subscribe<T>(string eventName, Action<T> callback)
        {
            InsertInEvents(eventName, callback);
        }

        public static void Unsubscribe<T>(string eventName, Action<T> callback)
        {
            RemoveInEvents(eventName, callback);
        }
        
        public static void Raise<T>(string eventName, T payload)
        {
            if (Events.TryGetValue(eventName, out Delegate callback))
                ((Action<T>)callback)?.Invoke(payload);
        }
        
        #endregion

        #region No payload methods
        public static void Subscribe(string eventName, Action callback)
        {
            InsertInEvents(eventName, callback);
        }

        public static void Unsubscribe(string eventName, Action callback)
        {
            RemoveInEvents(eventName, callback);
        }
        
        public static void Raise(string eventName)
        {
            if (Events.TryGetValue(eventName, out Delegate callback))
                ((Action)callback)?.Invoke();
        }
        
        #endregion

        #region Logic Methods
        private static void InsertInEvents(string eventName, Delegate callback) {
            if (Events.TryGetValue(eventName, out Delegate existing))
                Events[eventName] = Delegate.Combine(existing, callback);
            else
                Events[eventName] = callback;
        }
        
        private static void RemoveInEvents(string eventName, Delegate callback) {
            if (!Events.TryGetValue(eventName, out Delegate existing)) return;

            Delegate updated = Delegate.Remove(existing, callback);

            if (updated == null)
                Events.Remove(eventName);
            else
                Events[eventName] = updated;
        }
        #endregion
    }
}