using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace InmersionSystem.Induced.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Induced/RingingEars")]
    public class RingingEarsConfig : InducedEffectConfig
    {
        [Header("Ringing Ears")]
        public AudioClip ringingClip;
        public AnimationCurve volumeCurve; // maps normalized time → volume, future blending hook
    }
}