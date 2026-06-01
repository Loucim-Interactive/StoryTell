using InmersionSystem.Ambient.Configs.Base;
using UnityEngine;

namespace InmersionSystem.Ambient.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Ambient/ColdBreath")]
    public class ColdBreathConfig : AmbientEffectConfig
    {
        [Header("Timing")]
        public float breathInterval = 3f;      // seconds between exhales
        public float breathDuration = 1.2f;    // how long one exhale lasts

        [Header("Particles")]
        public float simulationSpeed = 1f;
    
        [Header("Audio")]
        public AudioClip exhaleSound;
        [Range(0f, 1f)] public float exhaleVolume = 0.6f;
    }
}