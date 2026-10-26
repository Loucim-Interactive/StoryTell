using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Rendering;

namespace Systems.InmersionSystem.Induced.Effects {
    public class TunnelVisionEffect : TimedInducedEffect<TunnelVisionConfig> {
        private readonly InducedVisionState state;
        public TunnelVisionEffect(TunnelVisionConfig config, InducedVisionState state) : base(config) { this.state = state; }
        protected override void Apply(float strength) {
            if (!state) return;
            state.Tunnel = strength;
            state.ClearRadius = config.clearRadius;
            state.Softness = config.softness;
        }
        public override void Reset() { if (state) state.Tunnel = 0; }
    }
}
