using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/Shake Vision")]
    public class ShakeVisionConfig : InducedEffectConfig
    {
        [Header("Shake")]
        public float maxMagnitude = 6f;        // max angle offset in degrees
        public float traumaDecaySpeed = 1.8f;  // how fast trauma drains passively
        public float maxDuration = 2.5f;       // duration at intensity 1

        [Header("Trauma")]
        [Tooltip("Shake magnitude = trauma^traumaExponent. Higher = more contrast between weak and strong shakes.")]
        public float traumaExponent = 2f;
    }
}
