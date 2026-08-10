using System.Collections;
using System.Collections.Generic;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.Global {
    public abstract class TriggerZoneScript : MonoBehaviour
    {
        [Header("Trigger settings")] 
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private bool triggerOnExit;
        [Tooltip("Can be triggered again?")]
        [SerializeField] private bool canRepeat;
        [Tooltip("If set, there will be a cooldown for repeat with -cooldown- seconds")]
        [SerializeField] private float cooldown = 0.0f;
        
        private Collider _collider;
        private bool _hasTriggered;
        private float _timer;

        private void Awake() {
            if (!_collider) _collider = GetComponent<Collider>();
            if (!_collider) {
                Debug.LogWarning("[TriggerZoneScript] requires a Collider");
                return;
            }

            if (!_collider.isTrigger) {
                Debug.LogWarning("[TriggerZoneScript] Collider is not trigger, fixing");
                _collider.isTrigger = true;
            }
            
            Debug.LogWarning("Trigger zones only trigger by player tags!");
        }

        public void OnTriggerEnter(Collider col) {
            if (!col.gameObject.CompareTag("Player") || _hasTriggered || !triggerOnEnter) return;
            Fire();
        }

        public void OnTriggerExit(Collider col) {
            if (!col.gameObject.CompareTag("Player") || _hasTriggered || !triggerOnExit) return;
            Fire();
        }

        private IEnumerator Reset() {
            yield return new WaitForSeconds(cooldown);
            _hasTriggered = false;
        }

        private void Fire() {
            Activate();
            HandleRepeat();
        }

        protected abstract void Activate();

        private void HandleRepeat() {
            if (!canRepeat) return;
            StartCoroutine(Reset());
        }
    }
}
