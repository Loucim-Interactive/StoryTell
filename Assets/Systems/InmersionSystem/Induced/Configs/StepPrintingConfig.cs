using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/Step Printing")]
    public class StepPrintingConfig : TimedInducedEffectConfig {
        [Tooltip("How long previous images bleed into the current image.")]
        [Range(.02f, 1)] public float trailPersistence = .22f;
        [Tooltip("Rate at which a new image is printed into the trail.")]
        [Range(2, 60)] public float printRate = 12f;
    }
}
