using System.Collections;
using InmersionSystem.Induced;
using InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Configs;

namespace Systems.InmersionSystem.Induced.Effects {
    public class RingingEarsEffect : InducedEffect<RingingEarsConfig>
    {
        public RingingEarsEffect(RingingEarsConfig config) : base(config) { }

        public override IEnumerator EffectRoutine(float intensity) {
            throw new System.NotImplementedException();
        }
    }
}
