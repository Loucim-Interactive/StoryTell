using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Systems.BattleSystem {
    [DisallowMultipleComponent]
    public sealed class ShadowSpawner : MonoBehaviour {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private Material blackUnlitMaterial;
        [SerializeField, Min(.01f)] private float capsuleRadius = .4f;
        [SerializeField, Min(.02f)] private float capsuleHeight = 1.8f;
        [SerializeField] private Vector2 lifetimeRange = new Vector2(3, 7);
        private readonly List<GameObject> shadows = new();
        public bool IsConfigured => spawnArea && blackUnlitMaterial;

        public int Spawn(int count) {
            if (!IsConfigured) return 0;
            shadows.RemoveAll(shadow => !shadow);
            float radius = Mathf.Max(.01f, capsuleRadius);
            float height = Mathf.Max(radius * 2, capsuleHeight);
            float minimumLifetime = Mathf.Max(.01f, Mathf.Min(lifetimeRange.x, lifetimeRange.y));
            float maximumLifetime = Mathf.Max(minimumLifetime, Mathf.Max(lifetimeRange.x, lifetimeRange.y));
            int spawned = 0;
            for (int i = 0; i < count; i++) {
                if (!spawnArea.TryGetPosition(out var position)) continue;
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shadow.name = "Battle Shadow";
                shadow.transform.position = position + Vector3.up * (height * .5f);
                shadow.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
                var renderer = shadow.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = blackUnlitMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                shadows.Add(shadow);
                Destroy(shadow, Random.Range(minimumLifetime, maximumLifetime));
                spawned++;
            }
            if (spawned < count) Debug.LogWarning($"Spawned {spawned}/{count} battle shadows. Check the spawn bounds and baked NavMesh.", this);
            return spawned;
        }

        private void OnDestroy() {
            foreach (var shadow in shadows) if (shadow) Destroy(shadow);
            shadows.Clear();
        }
    }
}
