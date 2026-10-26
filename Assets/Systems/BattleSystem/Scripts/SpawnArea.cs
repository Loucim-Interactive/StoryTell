using UnityEngine;
using UnityEngine.AI;

namespace Systems.BattleSystem {
    [DisallowMultipleComponent]
    public sealed class SpawnArea : MonoBehaviour {
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, new Vector3(20, 4, 20));
        [SerializeField] private bool sampleNavMesh = true;
        [SerializeField] private bool projectToGround;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(.01f)] private float sampleRadius = 2f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
        [SerializeField, Min(1)] private int maxAttempts = 16;

        public bool TryGetPosition(out Vector3 position) {
            for (int attempt = 0; attempt < Mathf.Max(1, maxAttempts); attempt++) {
                Vector3 local = new Vector3(Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y), Random.Range(bounds.min.z, bounds.max.z));
                position = transform.TransformPoint(local);
                if (!sampleNavMesh) {
                    if (!projectToGround) return true;
                    Vector3 top = transform.TransformPoint(new Vector3(local.x, bounds.max.y, local.z));
                    float distance = bounds.size.y * Mathf.Abs(transform.lossyScale.y);
                    if (!Physics.Raycast(top, -transform.up, out var ground, distance, groundMask, QueryTriggerInteraction.Ignore)) continue;
                    position = ground.point;
                    return true;
                }
                if (!NavMesh.SamplePosition(position, out var hit, sampleRadius, navMeshAreaMask)) continue;
                if (!bounds.Contains(transform.InverseTransformPoint(hit.position))) continue;
                position = hit.position;
                return true;
            }
            position = default;
            return false;
        }

        private void OnDrawGizmosSelected() {
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = previous;
        }
    }
}
