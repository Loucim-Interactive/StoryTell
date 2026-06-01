using InmersionSystem.Ambient.Configs.Base;
using UnityEngine;

namespace InmersionSystem.Ambient {
    public abstract class AmbientEffect<TConfig> : MonoBehaviour
        where TConfig : AmbientEffectConfig
    {
        [SerializeField] protected TConfig config;
        public abstract void OnEffectStart();
        public abstract void OnEffectStop();
        // each subclass runs its own update/coroutine internally
    }
}