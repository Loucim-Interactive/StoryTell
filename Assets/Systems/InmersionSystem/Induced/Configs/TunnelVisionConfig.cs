using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/Tunnel Vision")]
    public class TunnelVisionConfig : TimedInducedEffectConfig {
        [Range(.05f, .9f)] public float clearRadius = .28f;
        [Range(.01f, .8f)] public float softness = .35f;
    }
}
