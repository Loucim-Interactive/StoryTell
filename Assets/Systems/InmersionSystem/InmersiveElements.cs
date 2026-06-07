using System;
using UnityEngine;

namespace InmersionSystem {
    public class InmersiveElements
    {
        public enum EAmbientEffect {
            ColdBreath,
            Footsteps,
            GearRattle,
            Shivering,
        }

        public enum EInducedEffect {
            TunnelVision,
            BlurryVision,
            ShakeVision,
            RingingEars,
            MuffledHearing,
            PanicBreathing,
        }
        
        public enum ESurfaceType {
            Concrete,
            Rocks,
            Grass,
            Mud,
            Snow,
            Metal,
            Wood,
        }
        
        [Serializable]
        public class SurfaceSoundSet
        {
            public ESurfaceType surface;
            public AudioClip[] clips; // randomized per step
            [Range(0f, 1f)] public float volume = 1f;
        }
    }
}
