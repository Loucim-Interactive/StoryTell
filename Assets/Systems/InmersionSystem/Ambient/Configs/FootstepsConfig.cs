using InmersionSystem.Ambient.Configs.Base;
using UnityEngine;

namespace InmersionSystem.Ambient.Configs {
    [CreateAssetMenu(menuName = "Inmersion/Ambient/Footsteps")]
    public class FootstepsConfig : AmbientEffectConfig {
        [Header("Timing")] 
        public float walkStepInterval = 0.5f;
        public float sprintStepInterval = 0.3f;

        [Header("Surface Sounds")] 
        public InmersiveElements.SurfaceSoundSet[] surfaces;
    }
}