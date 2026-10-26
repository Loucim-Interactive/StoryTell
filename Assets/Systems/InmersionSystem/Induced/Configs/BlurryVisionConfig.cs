using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/Blurry Vision")]
    public class BlurryVisionConfig : TimedInducedEffectConfig {
        [Range(0, 12)] public float blurRadius = 5f;
    }
}
