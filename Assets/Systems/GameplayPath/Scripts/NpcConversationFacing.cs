using UnityEngine;

namespace StoryTell.GameplayPath
{
    [DisallowMultipleComponent]
    public sealed class NpcConversationFacing : MonoBehaviour
    {
        [SerializeField, Min(1)] private float turnSpeed = 160;
        private Transform listener;
        private Quaternion originalRotation;
        private bool hasOriginal, returning;
        public bool IsFacingListener => listener != null;

        public void Begin(Transform target)
        {
            if (!hasOriginal) { originalRotation = transform.rotation; hasOriginal = true; }
            listener = target; returning = false;
        }

        public void End() { listener = null; returning = hasOriginal; }

        private void LateUpdate()
        {
            if (listener != null)
            {
                Vector3 direction = Vector3.ProjectOnPlane(listener.position - transform.position, Vector3.up);
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.unscaledDeltaTime);
            }
            else if (returning)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, originalRotation, turnSpeed * Time.unscaledDeltaTime);
                if (Quaternion.Angle(transform.rotation, originalRotation) < 0.1f)
                { transform.rotation = originalRotation; returning = hasOriginal = false; }
            }
        }

        private void OnDisable()
        {
            if (hasOriginal) transform.rotation = originalRotation;
            listener = null; returning = hasOriginal = false;
        }
    }
}
