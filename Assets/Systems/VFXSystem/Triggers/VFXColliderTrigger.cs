using UnityEngine;

namespace Systems.VFXSystem.New.Triggers {
    [RequireComponent(typeof(Collider))]
    public class VFXColliderTrigger : VFXTriggerBase
    {
        [Header("Detection")]
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private bool triggerOnExit;

        protected override void Awake() {
            base.Awake();

            // Ensure the collider on this object is actually a trigger.
            var col = GetComponent<Collider>();
            if (!col.isTrigger) {
                Debug.LogWarning($"[VFXBoxTrigger] Collider on '{gameObject.name}' is not marked as a Trigger, now fixed.", this);
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other) {
            if (HasFired || !triggerOnEnter) return;
            if (!other.CompareTag("Player")) return;
            Fire();
        }

        private void OnTriggerExit(Collider other) {
            if (HasFired || !triggerOnExit) return;
            if (!other.CompareTag("Player")) return;
            Fire();
        }
    }
}
