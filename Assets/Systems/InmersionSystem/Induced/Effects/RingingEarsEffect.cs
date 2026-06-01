using System.Collections;
using InmersionSystem.Induced.Configs;

namespace InmersionSystem.Induced.Effects {
    public class RingingEarsEffect : InducedEffect<RingingEarsConfig>
    {
        public RingingEarsEffect(RingingEarsConfig config) : base(config) { }

        public override IEnumerator EffectRoutine(float intensity) {
            throw new System.NotImplementedException();
        }
    }
}
