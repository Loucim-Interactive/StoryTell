using System;
using System.Collections;
using Systems.EventSystem.Scripts;
using Systems.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.InmersionSystem {
    [RequireComponent(typeof(ColliderVisualizer))]
    [RequireComponent(typeof(Collider))]
    public class CharacterNarrationZoneScript : MonoBehaviour {
        
        [Header("Narration settings")] 
        [TextArea] [SerializeField] private string characterNarrationText = "Test narrating something about the environment!";
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private bool triggerOnExit;
        [Tooltip("Can the narration be triggered again?")]
        [SerializeField] private bool canRepeat;
        [Tooltip("If set, there will be a cooldown for repeat with cooldown seconds")]
        [SerializeField] private float cooldown = 0.0f;
        
        private Collider _collider;
        private bool _hasTriggered;
        private float _timer;

        private void Awake() {
            if (!_collider) _collider = GetComponent<Collider>();
            if (!_collider) {
                Debug.LogWarning("[CharacterNarrationZoneScript] requires a Collider");
                return;
            }

            if (!_collider.isTrigger) {
                Debug.LogWarning("[CharacterNarrationZoneScript] Collider is not trigger, fixing");
                _collider.isTrigger = true;
            }
        }

        public void OnTriggerEnter(Collider col) {
            if (!col.gameObject.CompareTag("Player") || _hasTriggered || !triggerOnEnter) return;
            Narrate();
        }

        public void OnTriggerExit(Collider col) {
            if (!col.gameObject.CompareTag("Player") || _hasTriggered || !triggerOnExit) return;
            Narrate();
        }

        private IEnumerator Reset() {
            yield return new WaitForSeconds(cooldown);
            _hasTriggered = false;
        }

        private void Narrate() {
            _hasTriggered = true;
            GameEventBus.Raise(GameplayEvents.StateThought, characterNarrationText);
            if (canRepeat) {
                StartCoroutine(Reset());
            }
        }
    }
}
