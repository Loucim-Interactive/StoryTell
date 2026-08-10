using System;

namespace Systems.DecisionSystem
{
    /// <summary>
    /// Plain, serializable timer owned and ticked externally by DecisionManager.
    /// No MonoBehaviour, no coroutines — a single source of truth for "time left".
    /// </summary>
    [Serializable]
    public class DecisionTimer
    {
        private float duration;
        private float remaining;

        public bool IsRunning { get; private set; }
        public bool IsExpired => IsRunning && remaining <= 0f;
        public float Remaining => remaining;
        public float Duration => duration;

        /// <summary>0 = just started, 1 = about to expire. Handy for fill bars.</summary>
        public float Progress01 => duration > 0f ? 1f - (remaining / duration) : 0f;

        public void Start(float seconds)
        {
            duration = seconds;
            remaining = seconds;
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            remaining = UnityEngine.Mathf.Max(0f, remaining - deltaTime);
        }

        public void Stop()
        {
            IsRunning = false;
            remaining = 0f;
        }
    }
}