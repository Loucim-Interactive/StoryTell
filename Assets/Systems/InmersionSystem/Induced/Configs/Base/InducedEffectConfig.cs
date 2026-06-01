using UnityEngine;

namespace InmersionSystem.Induced.Configs.Base {
    public abstract class InducedEffectConfig : ScriptableObject
    {
        [Header("Timing")]
        public float enterDuration = 0.3f;
        public float exitDuration = 0.5f;
    
        [Header("Base")]
        [Range(0f, 1f)] public float maxIntensity = 1f;
    }
}
