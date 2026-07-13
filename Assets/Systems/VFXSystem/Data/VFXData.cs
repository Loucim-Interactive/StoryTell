using UnityEngine;

namespace Systems.VFXSystem.New {
    [CreateAssetMenu(fileName = "VFXData", menuName = "VFX/VFX Data")]
    public class VFXData : ScriptableObject
    {
        [Header("VFX Data")]
        [Tooltip("The prefab for the effect, needs to have a particle system")]
        public GameObject vfxPrefab; // prefabs needs a particle system & needs an audio source
        [Tooltip("The audio clip for the effect.")]
        public AudioClip vfxSoundClip; 
    }
}
