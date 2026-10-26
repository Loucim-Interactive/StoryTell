using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Rendering;

namespace Systems.InmersionSystem.Induced.Effects {
    public class BlurryVisionEffect : TimedInducedEffect<BlurryVisionConfig> {
        private readonly InducedVisionState state;
        public BlurryVisionEffect(BlurryVisionConfig config, InducedVisionState state) : base(config) { this.state = state; }
        protected override void Apply(float strength) { if (state) state.Blur = config.blurRadius * strength; }
        public override void Reset() { if (state) state.Blur = 0; }
    }
}
