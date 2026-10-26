using Systems.InmersionSystem.Induced.Configs;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Effects {
    public class MuffledHearingEffect : TimedInducedEffect<MuffledHearingConfig> {
        private readonly AudioLowPassFilter filter;
        private bool captured, wasEnabled;
        private float cutoff;
        public MuffledHearingEffect(MuffledHearingConfig config, AudioLowPassFilter filter) : base(config) { this.filter = filter; }
        protected override void Apply(float strength) {
            if (!filter) return;
            if (!captured) { wasEnabled = filter.enabled; cutoff = filter.cutoffFrequency; captured = true; }
            filter.enabled = true;
            float from = wasEnabled ? cutoff : 22000;
            // Logarithmic interpolation gives a perceptually smooth change in frequency.
            filter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(from), Mathf.Log(Mathf.Min(from, config.cutoffFrequency)), strength));
        }
        public override void Reset() {
            if (filter && captured) { filter.cutoffFrequency = cutoff; filter.enabled = wasEnabled; }
            captured = false;
        }
    }
}
