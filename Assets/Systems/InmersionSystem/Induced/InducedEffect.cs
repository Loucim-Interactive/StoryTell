using System.Collections;
using InmersionSystem.Induced.Configs.Base;

namespace InmersionSystem.Induced {
    public abstract class InducedEffect<TConfig>
        where TConfig : InducedEffectConfig
    {
        protected TConfig Config;
        protected InducedEffect(TConfig config)
        {
            this.Config = config;
        }

        public abstract IEnumerator EffectRoutine(float intensity);
    }
}