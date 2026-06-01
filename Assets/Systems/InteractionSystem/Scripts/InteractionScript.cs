using EventSystem;
using EventSystem.Scripts;
using InspectionSystem.Scripts;
using InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace InteractionSystem.Scripts {
    public class InteractionScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private Transform interactionOrigin;
        [SerializeField] private InteractionUIManager uiManager;
        
        [SerializeField] private InspectionScript inspectScript; // temporal 

        [Header("Settings")]
        [SerializeField] private bool usePlayerCameraForOrigin = true;
        [SerializeField] private float interactionDistance = 5f;

        private InteractableScript _currentInteractable;
        private bool _currentlyInteracting;
        
        void Start() {
            if (uiManager == null) uiManager = InteractionUIManager.Instance;
            if (usePlayerCameraForOrigin && Camera.main) interactionOrigin = Camera.main.transform;
        }

        private void Update()
        {
            (bool detected, InteractableScript interactable) = DetectInteraction(interactionOrigin);
                
            if (detected  && !_currentlyInteracting) {
                _currentlyInteracting = true;
                _currentInteractable = interactable;
                uiManager.DisplayInteraction(interactable);
                return;
            }

            if (_currentInteractable) {
                bool performed = ActionWasPerformed(_currentInteractable.UIInteraction.interactionType);
                if (performed) {
                    Debug.Log("Interacting..");
                    _currentInteractable.Interact();
                    //inspectScript.StartInspect(_currentInteractable.gameObject); /// temp
                    GameEventBus.Raise(GameplayEvents.MaxZoom);
                    GameEventBus.Raise(GameplayEvents.StartInspection, _currentInteractable.gameObject);
                    ResetInteraction();
                }
            }

            if (detected) {
                if (_currentInteractable != interactable) ResetInteraction();
            }
            else {
                if (_currentInteractable) uiManager.HideInteraction();
                ResetInteraction();
            }
        }
        
        private (bool, InteractableScript) DetectInteraction(Transform origin)
        {
            if (Physics.Raycast(
                    origin.position,
                    origin.forward,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionLayer))
            {
                InteractableScript interactable = hit.collider.GetComponent<InteractableScript>();
                return (true, interactable);
            }

            return (false, null);
        }

        private bool ActionWasPerformed(EInteractions type) {
            switch (type) {
                case EInteractions.LeftClick:
                    return Input.GetMouseButtonDown(0);
                case EInteractions.RightClick:
                    return Input.GetMouseButtonDown(1);
                case EInteractions.MiddleClick:
                    return Input.GetMouseButtonDown(2);
                case EInteractions.Spacebar:
                    return Input.GetKeyDown(KeyCode.Space);
                default:
                    Debug.Log("Unknown interaction type: " + type);
                    return false;
            }
        }

        private void ResetInteraction() {
            _currentInteractable = null;
            _currentlyInteracting = false;
        }
    }
}
