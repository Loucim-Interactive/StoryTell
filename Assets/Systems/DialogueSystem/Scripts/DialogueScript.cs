using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueSystem.Scripts {
    public class DialogueScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _dialogueOrigin;

        [Header("Settings")]
        [SerializeField] private LayerMask _dialogueLayer;
        [SerializeField] private float _distanceDialogue = 3f;
        [SerializeField] private InputActionReference _dialogueAction;

        private void OnEnable() {
            if (_dialogueAction != null) _dialogueAction.action.Enable();
        }

        private void OnDisable() {
            if (_dialogueAction != null) _dialogueAction.action.Disable();
        }

        private void Start() {
            if (!_dialogueOrigin && Camera.main) _dialogueOrigin = Camera.main.transform;
        }

        private void Update() {
            if (!_dialogueOrigin || !_dialogueAction ||
                !_dialogueAction.action.WasPressedThisFrame()) return;

            if (Physics.Raycast(
                    _dialogueOrigin.position,
                    _dialogueOrigin.forward,
                    out RaycastHit hit,
                    _distanceDialogue,
                    _dialogueLayer)) {
                SpeakerScript speaker = hit.collider.GetComponentInParent<SpeakerScript>();
                if (speaker) speaker.Speak();
            }
        }
    }
}
