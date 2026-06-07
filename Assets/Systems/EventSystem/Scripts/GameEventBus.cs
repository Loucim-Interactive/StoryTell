using System;
using System.Collections.Generic;

namespace Systems.EventSystem.Scripts {
    public static class GameEventBus
    {
        private static readonly Dictionary<string, Delegate> Events = new();
        private static readonly Dictionary<(string EventName, Type PayloadType), Delegate> PayloadEvents = new();

        #region Payload Methods

        public static void Subscribe<T>(string eventName, Action<T> callback)
        {
            InsertInPayloadEvents(eventName, callback);
        }

        public static void Unsubscribe<T>(string eventName, Action<T> callback)
        {
            RemoveInPayloadEvents(eventName, callback);
        }
        
        public static void Raise<T>(string eventName, T payload)
        {
            if (PayloadEvents.TryGetValue((eventName, typeof(T)), out Delegate callback))
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

        private static void InsertInPayloadEvents<T>(string eventName, Action<T> callback) {
            var key = (eventName, typeof(T));
            if (PayloadEvents.TryGetValue(key, out Delegate existing))
                PayloadEvents[key] = Delegate.Combine(existing, callback);
            else
                PayloadEvents[key] = callback;
        }
        
        private static void RemoveInPayloadEvents<T>(string eventName, Action<T> callback) {
            var key = (eventName, typeof(T));
            if (!PayloadEvents.TryGetValue(key, out Delegate existing)) return;

            Delegate updated = Delegate.Remove(existing, callback);

            if (updated == null)
                PayloadEvents.Remove(key);
            else
                PayloadEvents[key] = updated;
        }
        #endregion
    }
}
