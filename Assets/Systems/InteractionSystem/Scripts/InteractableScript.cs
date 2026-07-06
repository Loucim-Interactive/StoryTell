using InteractionSystem.Scripts.Utils;
using Systems.EventSystem.Scripts;
using Systems.InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts {
    public abstract class InteractableScript : MonoBehaviour {
        [Header("Interaction Settings")]
        public string interactableName = "Interactable";
        public UIInteraction UIInteraction;
        [Header("Interaction actions")]
        [SerializeField] private bool stateDescription = true;
        [SerializeField] private bool focusInteraction = true;
        [SerializeField] private bool inspectInteraction = true;

        [Header("Sub Interactions")]
        [SerializeField] private bool subInteractions = true;
        [SerializeField] private Collider[] interactionColliders;
        
        private bool _disabledCollidersForSubInteractions;
        
        public bool HasSubInteractions => subInteractions;
        public bool InspectInteractable => inspectInteraction;
        public bool FocusInteraction => focusInteraction;
        public bool StateInteractableDescription => stateDescription;

        protected virtual void OnEnable() {
            GameEventBus.Subscribe(GameplayEvents.EndInspection, Restore);
        }
        
        protected virtual void OnDisable() {
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, Restore);
        }                             
        
        public void Interact() {
            PrepareSubInteractions();
            FireGeneralActions();
            OnInteract();   
        }
        
        protected virtual void OnInteract() {}
            
        public void SetCollidersEnabled(bool isEnabled) {
            Collider[] colliders = GetInteractionColliders();
            foreach (Collider col in colliders) {
                if (col) col.enabled = isEnabled;
            }
        }
        
        private void FireGeneralActions() { // this fires some global stuff for the interactable
            if (stateDescription) GameEventBus.Raise(GameplayEvents.StateThought, UIInteraction.characterDescription);
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
