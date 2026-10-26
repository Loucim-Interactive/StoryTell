using System.Collections;
using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Effects {
    public abstract class TimedInducedEffect<T> : InducedEffect<T> where T : TimedInducedEffectConfig {
        protected TimedInducedEffect(T config) : base(config) { }
        protected abstract void Apply(float strength);
        public override IEnumerator EffectRoutine(float intensity) {
            if (!config) yield break;
            float peak = Mathf.Clamp01(intensity) * Mathf.Clamp01(config.maxIntensity);
            if (peak <= 0) yield break;
            float enter = Mathf.Max(0, config.enterDuration);
            float hold = Mathf.Max(0, config.holdDuration);
            float exit = Mathf.Max(0, config.exitDuration);
            for (float elapsed = 0; elapsed < enter + hold + exit; elapsed += Time.deltaTime) {
                float envelope = elapsed < enter ? elapsed / enter :
                    elapsed < enter + hold ? 1 : 1 - (elapsed - enter - hold) / exit;
                Apply(peak * Mathf.SmoothStep(0, 1, envelope));
                yield return null;
            }
            Reset();
        }
    }
}
