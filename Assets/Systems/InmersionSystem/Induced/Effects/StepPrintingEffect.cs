using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Rendering;

namespace Systems.InmersionSystem.Induced.Effects {
    public class StepPrintingEffect : TimedInducedEffect<StepPrintingConfig> {
        private readonly InducedVisionState state;
        public StepPrintingEffect(StepPrintingConfig config, InducedVisionState state) : base(config) { this.state = state; }
        protected override void Apply(float strength) {
            if (!state) return;
            state.StepPrinting = strength;
            state.Persistence = config.trailPersistence;
            state.PrintRate = config.printRate;
        }
        public override void Reset() { if (state) { state.StepPrinting = 0; state.ReleaseHistory(); } }
    }
}
