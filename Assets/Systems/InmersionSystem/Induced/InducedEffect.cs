using System.Collections;
using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced {
    public abstract class InducedEffect<TConfig> : InducedEffect
        where TConfig : InducedEffectConfig
    {
        [SerializeField] protected TConfig config;
        protected InducedEffect(TConfig config) {
            this.config = config;
        }
    }
    
    public abstract class InducedEffect
    {
        public abstract IEnumerator EffectRoutine(float intensity);
    }
}