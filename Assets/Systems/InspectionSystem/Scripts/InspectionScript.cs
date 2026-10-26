using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.InspectionSystem.Scripts {
    public class InspectionScript : MonoBehaviour {
        #region Inspector Variables
        [Header("Config")] 
        public bool lockCursor = true;
        public bool needsMouseClick = true;
        public float maxLateralOffset = 0.7f;

        [Header("References")] 
        public Transform inspectAnchor; // Empty child of camera, e.g. (0, -0.2, 0.6)
        
        [Header("When inspect On/Off Scripts")] 
        [SerializeField] private MonoBehaviour[] onOffScripts;
        
        [Header("Lateral Movement")] 
        public float moveSpeed = 20f;

        [Header("Rotation")] 
        public float rotateSpeed = 20f;
        public float smoothing = 12f;

        [Header("Input")] 
        public InputActionReference inspectAction; // E — toggle
        public InputActionReference rotateDelta; // Mouse delta
        public InputActionReference mouseClick; // Mouse click
        public InputActionReference movementAction; // AWSD input

        private GameObject _target;
        private bool _isInspecting;

        // Cached restore data
        private Vector3 _originalInspectorAnchorLocalPos;
        private Transform _originalParent;
        private Vector3 _originalLocalPos;
        private Quaternion _originalLocalRot;

        // Smooth rotation/pos
        private Quaternion _currentRot;
        private Vector3 _currentPos;
        private Vector2 _lateralOffset;
        #endregion
        
        private Vector3 _previousMousePosition;
        
        // Call this from your detection layer when player presses interact
        #region Public API 
        public void StartInspect(GameObject obj) {
            if (!obj || !obj.activeInHierarchy || !inspectAnchor || !isActiveAndEnabled) return;
            if (_isInspecting && _target == obj) return;
            if (_isInspecting) StopInspect();

            _target = obj;
            _isInspecting = true;

            _originalParent = obj.transform.parent;
            _originalLocalPos = obj.transform.localPosition;
            _originalLocalRot = obj.transform.localRotation;
            
            obj.transform.SetParent(inspectAnchor, worldPositionStays: false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            _currentRot = obj.transform.rotation;
            _lateralOffset = Vector2.zero;
            _currentPos = _originalInspectorAnchorLocalPos;
            inspectAnchor.transform.localPosition = _currentPos;
            Freeze(true, _target);
        }

        public void StopInspect() {
            if (!_isInspecting) return;
            GameObject inspected = _target;
            _target = null;
            _isInspecting = false;

            if (inspected) {
                inspected.transform.SetParent(_originalParent, worldPositionStays: false);
                inspected.transform.localPosition = _originalLocalPos;
                inspected.transform.localRotation = _originalLocalRot;
            }
            if (inspectAnchor) inspectAnchor.localPosition = _originalInspectorAnchorLocalPos;
            Freeze(false, inspected);
            GameEventBus.Raise(GameplayEvents.EndInspection);
            if (inspected) {
                foreach (var target in inspected.GetComponents<IInspectionTarget>())
                    target.OnInspectionEnded();
            }
        }
        #endregion

        #region Lifecycle

        private void Awake() {
            if (inspectAnchor) _originalInspectorAnchorLocalPos = inspectAnchor.localPosition;
        }
        
        private void OnEnable() {
            GameEventBus.Subscribe<GameObject>(GameplayEvents.StartInspection, StartInspect);
            if (inspectAction) inspectAction.action.performed += OnInspectPerformed;
            EnableInputActions(true);
        }
        
        private void OnDisable() {
            GameEventBus.Unsubscribe<GameObject>(GameplayEvents.StartInspection, StartInspect);
            if (inspectAction) inspectAction.action.performed -= OnInspectPerformed;
            StopInspect();
            EnableInputActions(false);
        }

        private void OnInspectPerformed(InputAction.CallbackContext context) {
            if (_isInspecting) StopInspect();
        }

        private void Update() {
            if (!ShouldUpdate()) return;
            HandleLateralOffset();
            if (needsMouseClick && !mouseClick.action.IsPressed()) return;
            HandleRotation();
        }
        #endregion

        #region Functionality

        private bool ShouldUpdate() {
            if (!_isInspecting) return false;
            if (!_target || !_target.activeInHierarchy) {
                StopInspect();
                return false;
            }
            return true;
        }
        
        private void RotateTarget(Quaternion targetRot) {
            // Smooth damp toward target rotation for that floaty CS:GO feel
            _target.transform.localRotation = Quaternion.Lerp(
                _target.transform.localRotation,
                targetRot,
                Time.deltaTime * smoothing
            );
        }
        
        private void CalculateRotateTarget(Vector2 delta) {
            if (delta.sqrMagnitude > 0.01f) {
                float speed = rotateSpeed * Time.deltaTime;
                _currentRot = (Quaternion.AngleAxis(-delta.x * speed, Vector3.up)
                             * Quaternion.AngleAxis(delta.y * speed, Vector3.right)
                             * _currentRot);
            }
        }

        private void HandleRotation() {
            Vector2 delta = rotateDelta.action.ReadValue<Vector2>();
            CalculateRotateTarget(delta); 
            RotateTarget(_currentRot);
        }

        private void HandleLateralOffset() {
            Vector2 delta = movementAction.action.ReadValue<Vector2>();
            CalculateMoveAnchor(delta);
            MoveAnchor(_currentPos);
        }
        
        private void MoveAnchor(Vector3 targetPos) {
            // Smooth damp toward target pos for that floaty CS:GO feel
            inspectAnchor.transform.localPosition = Vector3.Lerp(
                inspectAnchor.transform.localPosition,
                targetPos,
                Time.deltaTime * smoothing
            );
        }

        private void CalculateMoveAnchor(Vector2 delta) {
            if (delta.sqrMagnitude > 0.01f) {
                float speed = moveSpeed * Time.deltaTime;
                _lateralOffset += delta * speed;
                _lateralOffset = Vector2.ClampMagnitude(_lateralOffset, maxLateralOffset);
                _currentPos = _originalInspectorAnchorLocalPos + new Vector3(_lateralOffset.x, _lateralOffset.y, 0f);
            }
        }
        private void Freeze(bool freeze, GameObject target) {
            var rb = target ? target.GetComponent<Rigidbody>() : null;
            if (rb) rb.isKinematic = freeze;
            if (onOffScripts != null)
                foreach (var script in onOffScripts)
                    if (script && script != this) script.enabled = !freeze;
            if (lockCursor) Cursor.lockState = freeze ? CursorLockMode.Locked : CursorLockMode.None;
        }
        #endregion

        private void EnableInputActions(bool enabled) {
            if (enabled) {
                if (inspectAction) inspectAction.action.Enable();
                if (rotateDelta) rotateDelta.action.Enable();
                if (mouseClick) mouseClick.action.Enable();
                if (movementAction) movementAction.action.Enable();
            }
            else {
                if (inspectAction) inspectAction.action.Disable();
                if (rotateDelta) rotateDelta.action.Disable();
                if (mouseClick) mouseClick.action.Disable();
                if (movementAction) movementAction.action.Disable();
            }
        }
    }
}
