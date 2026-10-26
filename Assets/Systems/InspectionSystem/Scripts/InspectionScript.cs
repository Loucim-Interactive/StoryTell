using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.InspectionSystem.Scripts
{
    public class InspectionScript : MonoBehaviour
    {
        [Header("Config")]
        public bool lockCursor = true;
        public bool needsMouseClick = true;
        public float maxLateralOffset = 0.7f;
        [Header("References")] public Transform inspectAnchor;
        [Header("When inspect On/Off Scripts")]
        [SerializeField] private MonoBehaviour[] onOffScripts;
        [Header("Lateral Movement")] public float moveSpeed = 20f;
        [Header("Rotation")] public float rotateSpeed = 20f;
        public float smoothing = 12f;
        [Header("Input")]
        public InputActionReference inspectAction, rotateDelta, mouseClick, movementAction;
        private GameObject _target;
        private bool _isInspecting, originalKinematic;
        private Transform _originalParent;
        private Vector3 _originalLocalPos, originalScale, _originalInspectorAnchorLocalPos, holdPosition;
        private Quaternion _originalLocalRot, _currentRot;
        private Vector2 _lateralOffset;
        private Vector3 _currentPos;
        private Rigidbody inspectedBody;
        private bool[] enabledScripts;
        private bool[] ownedActions;
        private CursorLockMode originalCursorLock;
        private bool originalCursorVisible;
        private float startedAt;
        private InspectableDocument document;
        private Camera documentCamera;
        private float documentExtent;
        public bool IsInspecting => _isInspecting;
        public GameObject CurrentTarget => _target;
        public int LastInspectionEndFrame { get; private set; } = -1;

        private void Awake() { if (inspectAnchor) _originalInspectorAnchorLocalPos = inspectAnchor.localPosition; }
        public void StartInspect(GameObject obj)
        {
            if (_isInspecting || !obj || !obj.activeInHierarchy || !inspectAnchor || !isActiveAndEnabled) return;
            _target = obj; _isInspecting = true; startedAt = Time.unscaledTime;
            _originalParent = obj.transform.parent;
            _originalLocalPos = obj.transform.localPosition;
            _originalLocalRot = obj.transform.localRotation;
            originalScale = obj.transform.localScale;
            _originalInspectorAnchorLocalPos = inspectAnchor.localPosition;
            inspectedBody = obj.GetComponent<Rigidbody>();
            if (inspectedBody) { originalKinematic = inspectedBody.isKinematic; inspectedBody.isKinematic = true; }
            obj.transform.SetParent(inspectAnchor, false);
            obj.transform.localPosition = Vector3.zero;
            document = obj.GetComponent<InspectableDocument>();
            documentCamera = inspectAnchor.GetComponentInParent<Camera>();
            documentExtent = 0;
            _currentRot = document ? Quaternion.Euler(document.inspectionEulerAngles) : Quaternion.identity;
            obj.transform.localRotation = _currentRot;
            if (document)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                    documentExtent = extent;
                    if (extent > 0.001f) obj.transform.localScale *= document.inspectionSize / extent;
                }
            }
            holdPosition = _originalInspectorAnchorLocalPos + (document ? document.inspectionOffset : Vector3.zero);
            _lateralOffset = Vector2.zero; _currentPos = FitDocument(holdPosition);
            inspectAnchor.localPosition = _currentPos;
            enabledScripts = new bool[onOffScripts?.Length ?? 0];
            for (int i = 0; i < enabledScripts.Length; i++)
                if (onOffScripts[i] && onOffScripts[i] != this) { enabledScripts[i] = onOffScripts[i].enabled; onOffScripts[i].enabled = false; }
            originalCursorLock = Cursor.lockState; originalCursorVisible = Cursor.visible;
            if (lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void StopInspect()
        {
            if (!_isInspecting) return;
            GameObject inspected = _target;
            if (_target)
            {
                _target.transform.SetParent(_originalParent, false);
                _target.transform.localPosition = _originalLocalPos;
                _target.transform.localRotation = _originalLocalRot;
                _target.transform.localScale = originalScale;
                if (inspectedBody) inspectedBody.isKinematic = originalKinematic;
            }
            if (inspectAnchor) inspectAnchor.localPosition = _originalInspectorAnchorLocalPos;
            for (int i = 0; i < (enabledScripts?.Length ?? 0); i++)
                if (onOffScripts[i] && onOffScripts[i] != this) onOffScripts[i].enabled = enabledScripts[i];
            if (lockCursor) { Cursor.lockState = originalCursorLock; Cursor.visible = originalCursorVisible; }
            _target = null; _isInspecting = false;
            LastInspectionEndFrame = Time.frameCount;
            GameEventBus.Raise(GameplayEvents.EndInspection);
            if (inspected) foreach (var target in inspected.GetComponents<IInspectionTarget>()) target.OnInspectionEnded();
            if (inspected && inspected.TryGetComponent<InspectableDocument>(out var doc) && doc.hideAfterInspection) inspected.SetActive(false);
        }
        public void SetDocumentSide(bool showText)
        {
            if (!_isInspecting || document == null) return;
            holdPosition = _originalInspectorAnchorLocalPos + (showText ? document.inspectionOffset : Vector3.zero);
        }
        private void CloseStarted(InputAction.CallbackContext _) { if (_isInspecting && Time.unscaledTime - startedAt > 0.2f) StopInspect(); }
        private void OnEnable()
        {
            GameEventBus.Subscribe<GameObject>(GameplayEvents.StartInspection, StartInspect);
            if (inspectAction?.action != null) inspectAction.action.started += CloseStarted;
            var actions = Actions(); ownedActions = new bool[actions.Length];
            for (int i = 0; i < actions.Length; i++)
                if (actions[i]?.action != null && !actions[i].action.enabled) { ownedActions[i] = true; actions[i].action.Enable(); }
        }
        private void OnDisable()
        {
            StopInspect();
            GameEventBus.Unsubscribe<GameObject>(GameplayEvents.StartInspection, StartInspect);
            if (inspectAction?.action != null) inspectAction.action.started -= CloseStarted;
            var actions = Actions();
            for (int i = 0; i < actions.Length; i++) if (ownedActions != null && ownedActions[i] && actions[i]?.action != null) actions[i].action.Disable();
        }
        private InputActionReference[] Actions() => new[] { inspectAction, rotateDelta, mouseClick, movementAction };
        private void Update()
        {
            if (!_isInspecting) return;
            GameObject inspected = _target;
            if (!_target) { StopInspect(); return; }
            if (Time.unscaledTime - startedAt > 0.2f && (inspectAction?.action?.WasPressedThisFrame() ?? false)) { StopInspect(); return; }
            Vector2 movement = movementAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            float limit = _target.GetComponent<InspectableDocument>() ? Mathf.Min(maxLateralOffset, 0.14f) : maxLateralOffset;
            _lateralOffset = Vector2.ClampMagnitude(_lateralOffset + movement * (moveSpeed * Time.deltaTime), limit);
            _currentPos = FitDocument(holdPosition + new Vector3(_lateralOffset.x, _lateralOffset.y, 0));
            inspectAnchor.localPosition = Vector3.Lerp(inspectAnchor.localPosition, _currentPos, Time.deltaTime * smoothing);
            if (needsMouseClick && !(mouseClick?.action?.IsPressed() ?? false)) return;
            Vector2 delta = rotateDelta?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            float speed = rotateSpeed * Time.deltaTime;
            _currentRot = Quaternion.AngleAxis(-delta.x * speed, Vector3.up) * Quaternion.AngleAxis(delta.y * speed, Vector3.right) * _currentRot;
            _target.transform.localRotation = Quaternion.Lerp(_target.transform.localRotation, _currentRot, Time.deltaTime * smoothing);
        }
        private Vector3 FitDocument(Vector3 position)
        {
            if (!document || !documentCamera || documentExtent <= 0.001f) return position;
            float halfHeight = position.z * Mathf.Tan(documentCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float halfWidth = halfHeight * documentCamera.aspect;
            float size = Mathf.Min(document.inspectionSize, halfWidth * 0.85f, halfHeight * 1.4f);
            _target.transform.localScale = originalScale * (size / documentExtent);
            float margin = size * 0.55f;
            position.x = Mathf.Clamp(position.x, -halfWidth * 0.94f + margin, halfWidth * 0.14f - margin);
            position.y = Mathf.Clamp(position.y, -halfHeight + margin, halfHeight - margin);
            return position;
        }
    }
}
