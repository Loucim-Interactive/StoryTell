using UnityEngine;

namespace Systems.Utils {
    public class ColliderVisualizer : MonoBehaviour
    {
        public Color solidColor = Color.green;
        public Color triggerColor = Color.yellow;

        void OnDrawGizmos()
        {
            if (!TryGetComponent(out Collider col)) return;

            Gizmos.color = col.isTrigger ? triggerColor : solidColor;

            if (col is BoxCollider box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius);
            }
        }
    }
}
