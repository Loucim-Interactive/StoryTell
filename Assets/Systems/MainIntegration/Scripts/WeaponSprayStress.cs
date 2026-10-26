using System;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [Serializable]
    public sealed class WeaponSprayStress
    {
        [SerializeField, Min(1)] private int lowVisibilityShots = 15;
        [SerializeField, Min(2)] private int completionShots = 20;
        [SerializeField, Min(0)] private float recoveryDelay = 0.8f;
        [SerializeField, Min(0)] private float recoveryPerSecond = 4f;
        public float Heat { get; private set; }
        private float lastShotTime = float.NegativeInfinity;
        public bool Completed { get; private set; }
        public float Darkness => Completed ? 1f : Heat <= lowVisibilityShots
            ? Mathf.Clamp01(Heat / lowVisibilityShots) * 0.85f
            : Mathf.Lerp(0.85f, 1f, Mathf.InverseLerp(lowVisibilityShots, Mathf.Max(lowVisibilityShots + 1, completionShots), Heat));
        public void RecordShot(float now, bool canComplete = true)
        {
            if (Completed) return;
            Heat = canComplete ? Heat + 1 : Mathf.Min(lowVisibilityShots, Heat + 1);
            lastShotTime = now;
            Completed = canComplete && Heat >= Mathf.Max(lowVisibilityShots + 1, completionShots);
        }
        public void Tick(float now, float delta)
        {
            if (!Completed && now - lastShotTime > recoveryDelay)
                Heat = Mathf.Max(0, Heat - recoveryPerSecond * delta);
        }
    }
}
