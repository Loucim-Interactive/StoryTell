using UnityEngine;

namespace InmersionSystem.Induced.Configs.Base {
    public abstract class TimedInducedEffectConfig : InducedEffectConfig {
        [Min(0)] public float holdDuration = 2f;
    }
}
