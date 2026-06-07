using InmersionSystem;
using Systems.EventSystem.Scripts;
using Systems.InmersionSystem.Induced;
using UnityEngine;

namespace Systems.InmersionSystem.Elements {
    public class BombScript : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private InmersionHandlerScript handler;
        
        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float intensity = 0.8f;
        [SerializeField] private bool sendExplosionPosition = true;
        [SerializeField] private bool useTrigger = true;
        [SerializeField] private bool triggerOnlyWithPlayer = true;

        private void Start() {
            if (handler == null) {
                handler = InmersionHandlerScript.Instance;
            }

            if (handler == null) {
                handler = FindFirstObjectByType<InmersionHandlerScript>();
            }
        }

        public void Explode() {
            if (handler == null) {
                Debug.LogWarning("[BombScript] No InmersionHandlerScript found. Explosion camera shake was skipped.", this);
            }
            else {
                handler.Trigger(InmersiveElements.EInducedEffect.ShakeVision, intensity);
            }

            GameEventBus.Raise(GameplayEvents.Explosion, intensity);
            if (sendExplosionPosition) GameEventBus.Raise(GameplayEvents.Explosion, transform.position);

            Debug.Log("Exploding");
        }
        
        private void OnTriggerEnter(Collider other) {
            if (!useTrigger) return;
            if (triggerOnlyWithPlayer && !other.CompareTag("Player")) return;
            Explode();
        }
    }
}
