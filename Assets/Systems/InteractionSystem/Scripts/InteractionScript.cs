using InteractionSystem.Scripts.Utils;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Systems.InteractionSystem.Scripts
{
    public class InteractionScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private Transform interactionOrigin;
        [SerializeField] private InteractionUIManager uiManager;
        [SerializeField] private InspectionScript inspectScript;

        [Header("Settings")]
        [SerializeField] private bool usePlayerCameraForOrigin = true;
        [SerializeField] private float interactionDistance = 5f;

        public InteractableScript CurrentTarget => _currentInteractable;
        [SerializeField] private UnityEngine.InputSystem.InputActionReference interactAction;
        private bool ownsInteractAction;
        private InteractableScript _currentInteractable;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[64];

        private void OnEnable()
        {
            ownsInteractAction = interactAction != null && interactAction.action != null && !interactAction.action.enabled;
            if (ownsInteractAction) interactAction.action.Enable();
        }

        private void Start()
        {
            if (inspectScript == null) inspectScript = GetComponent<InspectionScript>();
            if (uiManager == null) uiManager = InteractionUIManager.Instance;
            if (usePlayerCameraForOrigin && Camera.main != null)
                interactionOrigin = Camera.main.transform;
        }

        private void Update()
        {
            // E also closes inspection: do not interact with the restored object again in that frame.
            if (inspectScript != null && inspectScript.LastInspectionEndFrame == Time.frameCount) { ResetInteraction(); return; }
            InteractableScript candidate = DetectInteraction();
            if (candidate != _currentInteractable)
            {
                ResetInteraction();
                _currentInteractable = candidate;
                if (candidate != null && uiManager != null)
                    uiManager.DisplayInteraction(candidate);
            }

            if (_currentInteractable == null || _currentInteractable.UIInteraction == null ||
                !ActionWasPerformed(_currentInteractable.UIInteraction.interactionType)) return;

            // Interaction can disable/destroy its own object or this component.
            InteractableScript target = _currentInteractable;
            target.Interact();
            if (target != null) HandleInteractableActions(target);
            ResetInteraction();
        }

        private InteractableScript DetectInteraction()
        {
            if (interactionOrigin == null) return null;
            // Test occlusion against the world first: an interaction layer mask alone
            // would allow the player to interact through trench walls.
            float unobstructedDistance = interactionDistance;
            int count = Physics.RaycastNonAlloc(interactionOrigin.position, interactionOrigin.forward,
                obstructionHits, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit obstruction = obstructionHits[i];
                if (obstruction.collider.transform.IsChildOf(transform)) continue;
                unobstructedDistance = Mathf.Min(unobstructedDistance, obstruction.distance + 0.001f);
            }
            // Interactive trigger colliders count, while narrative trigger volumes
            // on other layers do not obstruct the ray.
            if (!Physics.Raycast(interactionOrigin.position, interactionOrigin.forward,
                    out RaycastHit hit, unobstructedDistance, interactionLayer,
                    QueryTriggerInteraction.Collide)) return null;
            InteractableScript target = hit.collider.GetComponentInParent<InteractableScript>();
            return target != null && target.isActiveAndEnabled && target.UIInteraction != null ? target : null;
        }

        private static void HandleInteractableActions(InteractableScript target)
        {
            if (target.FocusInteraction) GameEventBus.Raise(GameplayEvents.MaxZoom);
            if (target.InspectInteractable) GameEventBus.Raise(GameplayEvents.StartInspection, target.gameObject);
        }

        private bool ActionWasPerformed(EInteractions type)
        {
#if ENABLE_INPUT_SYSTEM
            if (type == EInteractions.EKeypress) return interactAction != null && interactAction.action != null && interactAction.action.WasPressedThisFrame();
            if (Mouse.current != null)
            {
                switch (type)
                {
                    case EInteractions.LeftClick: return Mouse.current.leftButton.wasPressedThisFrame;
                    case EInteractions.RightClick: return Mouse.current.rightButton.wasPressedThisFrame;
                    case EInteractions.MiddleClick: return Mouse.current.middleButton.wasPressedThisFrame;
                }
            }
            if (type == EInteractions.Spacebar && Keyboard.current != null)
                return Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (type)
            {
                case EInteractions.LeftClick: return Input.GetMouseButtonDown(0);
                case EInteractions.RightClick: return Input.GetMouseButtonDown(1);
                case EInteractions.MiddleClick: return Input.GetMouseButtonDown(2);
                case EInteractions.Spacebar: return Input.GetKeyDown(KeyCode.Space);
            }
#endif
            return false;
        }

        private void ResetInteraction()
        {
            _currentInteractable = null;
            if (uiManager != null) uiManager.HideInteraction();
        }

        private void OnDisable()
        {
            if (ownsInteractAction && interactAction != null && interactAction.action != null) interactAction.action.Disable();
            ownsInteractAction = false;
            ResetInteraction();
        }
    }
}
