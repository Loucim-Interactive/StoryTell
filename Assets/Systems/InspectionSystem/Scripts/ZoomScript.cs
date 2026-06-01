using EventSystem.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InspectionSystem.Scripts {
    public class ZoomScript : MonoBehaviour
    {        
        [Header("References")]
        [SerializeField] private InputActionReference scrollAction;
        [SerializeField] private Transform cameraTransform;

        [Header("Configuration")]
        [SerializeField] private float minFovZoom = 35f;
        [SerializeField] private float maxFovZoom = 60f;
        [SerializeField] private float zoomStep = 5f;
        [SerializeField] private float zoomSmoothing = 12f;

        private float _targetFov;
        private float _defaultFov;
        private Camera _camera;

        private void OnEnable() {
            GameEventBus.Subscribe(GameplayEvents.MaxZoom, SetFovToMaxZoom);
            GameEventBus.Subscribe(GameplayEvents.EndInspection, ResetFov);
            scrollAction.action.Enable();
        }
        
        private void OnDisable() {
            GameEventBus.Unsubscribe(GameplayEvents.MaxZoom, SetFovToMaxZoom);
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, ResetFov);
            scrollAction.action.Disable();
        }
        
        
        private void Awake() {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            _camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            if (!_camera) {
                Debug.LogError($"{nameof(ZoomScript)} needs a camera reference.", this);
                enabled = false;
                return;
            }

            ValidateZoomRange();
            _defaultFov = Mathf.Clamp(_camera.fieldOfView, minFovZoom, maxFovZoom);
            _targetFov = Mathf.Clamp(_camera.fieldOfView, minFovZoom, maxFovZoom);
            
        }

        private void Update() {
            HandleZoom();
        }

        private void HandleZoom() {
            float scroll = scrollAction.action.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) > 0.01f) {
                _targetFov -= Mathf.Sign(scroll) * zoomStep;
                _targetFov = Mathf.Clamp(_targetFov, minFovZoom, maxFovZoom);
            }

            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView,
                _targetFov,
                Time.deltaTime * zoomSmoothing
            );
        }

        private void ValidateZoomRange() {
            if (maxFovZoom <= 0f) maxFovZoom = _camera.fieldOfView;
            if (minFovZoom <= 0f) minFovZoom = Mathf.Max(1f, _camera.fieldOfView - 25f);
            if (maxFovZoom <= minFovZoom) maxFovZoom = minFovZoom + 1f;
        }

        private void SetFovToMaxZoom() {
            _targetFov = minFovZoom;
        }

        private void ResetFov() {
            _targetFov = _defaultFov;
        }
    }
}
