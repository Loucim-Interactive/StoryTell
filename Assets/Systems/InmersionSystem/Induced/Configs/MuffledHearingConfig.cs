using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/Muffled Hearing")]
    public class MuffledHearingConfig : TimedInducedEffectConfig {
        [Range(10, 22000)] public float cutoffFrequency = 650f;
    }
}
