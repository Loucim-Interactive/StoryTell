using UnityEngine;

namespace InmersionSystem.Ambient.Configs.Base {
    public abstract class AmbientEffectConfig : ScriptableObject
    {
        [Header("Base")]
        public float volume = 1f;
        public bool enabledByDefault = true;
    }
}
