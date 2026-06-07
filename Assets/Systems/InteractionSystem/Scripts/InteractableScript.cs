using InteractionSystem.Scripts.Utils;
using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace InteractionSystem.Scripts {
    public abstract class InteractableScript : MonoBehaviour {
        [Header("Interaction")]
        public string interactableName = "Interactable";
        public UIInteraction UIInteraction;
        [SerializeField] private bool focusInteraction = true;

        [Header("Sub Interactions")]
        [SerializeField] private bool subInteractions = true;
        [SerializeField] private Collider[] interactionColliders;
        
        private bool _disabledCollidersForSubInteractions;
        
        public bool HasSubInteractions => subInteractions;
        public bool FocusInteraction => focusInteraction;

        protected virtual void OnEnable() {
            GameEventBus.Subscribe(GameplayEvents.EndInspection, Restore);
        }
        
        protected virtual void OnDisable() {
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, Restore);
        }                             
        
        public void Interact() {
            PrepareSubInteractions();
            OnInteract();   
        }
        
        protected virtual void OnInteract() {}
            
        public void SetCollidersEnabled(bool isEnabled) {
            Collider[] colliders = GetInteractionColliders();
            foreach (Collider col in colliders) {
                if (col) col.enabled = isEnabled;
            }
        }
        
        private void PrepareSubInteractions() {
            if (!subInteractions) return;
            SetCollidersEnabled(false);
            _disabledCollidersForSubInteractions = true;
        }
        
        private void Restore() {
            if (!_disabledCollidersForSubInteractions) return;
            SetCollidersEnabled(true);
            _disabledCollidersForSubInteractions = false;
        }
                
        private Collider[] GetInteractionColliders() {
            if (interactionColliders is { Length: > 0 }) return interactionColliders;
            interactionColliders = GetComponents<Collider>();
            return interactionColliders;
        }
    }
}
