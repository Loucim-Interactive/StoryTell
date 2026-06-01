using UnityEngine;

namespace FirstPersonControllerSystem.Scripts.ControllerSys {
    public class CameraControllerScript : ControllerScript
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerMotorScript playerMotor;
        
        [Header("Configuration")]
        [SerializeField] private float sensitivityX = 0.15f;
        [SerializeField] private float sensitivityY = 0.15f;
        [SerializeField] private float clampAngle = 80f;
        
        [Header("Bob Settings")]
        [SerializeField] private CameraBobController bob;

        private float _cameraPitch;
        private float _targetFov;
        private Camera _camera;

        protected override void Awake() {
            base.Awake();
            if (playerMotor == null) playerMotor = GetComponent<PlayerMotorScript>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            _camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            if (!_camera) {
                Debug.LogError($"{nameof(CameraControllerScript)} needs a camera reference.", this);
                enabled = false;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            bob.Initialize(cameraTransform, playerMotor);

        }

        private void Update() {
            UpdateCameraRotation();
            bob.Tick();
        }

        private void UpdateCameraRotation() {
            Vector2 look = ScriptPlayerInput.Look;

            // Horizontal — rotate the whole player body
            transform.Rotate(Vector3.up * (look.x * sensitivityX));

            // Vertical — rotate only the camera, clamped
            _cameraPitch -= look.y * sensitivityY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -clampAngle, clampAngle);
            cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }
    }
}
