using UnityEngine;

namespace Systems.VFXSystem.New {
    public static class VFXManager {
        
        private static VFXRegistry registry;
        private const string RegistryResourcePath = "VFXRegistry"; // expects an asset at Resources/VFXRegistry.asset

        private static VFXRegistry Registry {
            get {
                if (registry == null) {
                    registry = Resources.Load<VFXRegistry>(RegistryResourcePath);
                    if (registry == null)
                        Debug.LogWarning($"[VFXManager] No VFXRegistry found at Resources/{RegistryResourcePath}. Call VFXManager.Initialize() explicitly, or type-based lookups will fail.");
                }
                return registry;
            }
        }

        /// Optional explicit assignment (e.g. from a bootstrap script), so you're not
        /// forced to rely on the Resources.Load fallback above.
        public static void Initialize(VFXRegistry reg) {
            registry = reg;
        }
         
        public static void PlayVFXAt(VFXData data, Vector3 position) {
            if (!data) {
                Debug.LogWarning("[VFXManager] PlayVFXAt called with null VFXData.");
                return;
            }

            if (!data.vfxPrefab) {
                Debug.LogWarning("[VFXManager] VFXData has no vfxPrefab assigned.");
                return;
            }

            GameObject instance = SpawnInstance(data.vfxPrefab, position);

            var ps = instance.GetComponent<ParticleSystem>();
            var audioSrc = instance.GetComponent<AudioSource>();

            if (!ps) {
                Debug.LogWarning($"[VFXManager] '{data.vfxPrefab.name}' has no ParticleSystem component.", instance);
                ReleaseInstance(instance, 0f);
                return;
            }
            
            if (!audioSrc) {
                Debug.LogWarning($"[VFXManager] '{data.vfxPrefab.name}' has no AudioSource component.", instance);
                ReleaseInstance(instance, 0f);
                return;
            }

            audioSrc.clip = data.vfxSoundClip;

            ps.Play();
            audioSrc.Play();

            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            ReleaseInstance(instance, lifetime);
        }


        public static void PlayVFXAt(VFXDefinition def, Vector3 position) => PlayVFXAt(def.data, position);

        public static void PlayVFXAt(VFXTypes type, Vector3 position) {
            if (!registry) Debug.LogWarning($"[VFXManagerScript] No VFXRegistry assigned");;
            if (registry.TryGet(type, out var def)) {
                PlayVFXAt(def.data, position);
            } else {
                Debug.LogWarning($"[VFXManagerScript] No VFXDefinition registered for type '{type}'");
            }
        }
        
        private static GameObject SpawnInstance(GameObject prefab, Vector3 position) {
            // TODO: swap for pool.Get(prefab, position) once pooling exists
            return Object.Instantiate(prefab, position, Quaternion.identity);
        }

        private static void ReleaseInstance(GameObject instance, float delay) {
            // TODO: swap for pool.Release(instance) once pooling exists
            Object.Destroy(instance, delay);
        }
    }
}
