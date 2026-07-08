using System;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera
{
    public class CameraControllerScript : ControllerScript
    {
        [Header("References")]
        [SerializeField] private Transform bobPivotTransform;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerMotorScript playerMotor;

        [Header("Configuration")]
        [SerializeField] private float sensitivityX = 0.15f;
        [SerializeField] private float sensitivityY = 0.15f;
        [SerializeField] private float clampAngle   = 80f;

        [Header("Bob Settings")]
        [SerializeField] private CameraBobController bob;

        [Header("LookAt Settings")]
        [SerializeField] private CameraLookAtController look;

        // exposed for sub-controllers
        public float CameraPitch => _cameraPitch;
        public void SetCameraPitch(float pitch) => _cameraPitch = Mathf.Clamp(pitch, -clampAngle, clampAngle);
        public void SetInputLocked(bool locked)  => _inputLocked = locked;

        private float   _cameraPitch;
        private Vector3 _shakeRotationOffset;
        private bool    _inputLocked;
        private UnityEngine.Camera _camera;

        private Coroutine _returnCoroutine;

        protected override void Awake()
        {
            base.Awake();

            if (playerMotor == null)
                playerMotor = GetComponent<PlayerMotorScript>();

            if (cameraTransform == null && UnityEngine.Camera.main != null)
                cameraTransform = UnityEngine.Camera.main.transform;

            _camera = cameraTransform != null ? cameraTransform.GetComponent<UnityEngine.Camera>() : null;

            if (!_camera) {
                Debug.LogError($"{nameof(CameraControllerScript)} needs a camera reference.", this);
                enabled = false;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            bob.Initialize(bobPivotTransform, playerMotor);
            look.Initialize(transform, cameraTransform, this);
        }

        private void OnEnable() {
            GameEventBus.Subscribe<Transform>(GameplayEvents.StartLookAtPoint, HandleLookAtStart);
            GameEventBus.Subscribe(GameplayEvents.EndLookAtPoint, HandleLookAtEnd);
        }

        private void OnDisable() {
            GameEventBus.Unsubscribe<Transform>(GameplayEvents.StartLookAtPoint, HandleLookAtStart);
            GameEventBus.Unsubscribe(GameplayEvents.EndLookAtPoint, HandleLookAtEnd);
        }

        private void Update() {
            if (!_inputLocked)
                UpdateCameraRotation();

            // Always apply pitch to camera (LookAt drives pitch via SetCameraPitch)
            ApplyCameraRotation();
            bob.Tick();
        }

        private void UpdateCameraRotation()
        {
            Vector2 lookInput = ScriptPlayerInput.Look;

            transform.Rotate(Vector3.up * (lookInput.x * sensitivityX));

            _cameraPitch -= lookInput.y * sensitivityY;
            _cameraPitch  = Mathf.Clamp(_cameraPitch, -clampAngle, clampAngle);
        }

        private void ApplyCameraRotation() => cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f) * Quaternion.Euler(_shakeRotationOffset);

        public void SetShakeRotationOffset(Vector3 offset) => _shakeRotationOffset = offset;

        private void HandleLookAtStart(Transform target)
        {
            // Stop any in-progress return
            if (_returnCoroutine != null) {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }

            if (look.LookCoroutine != null) StopCoroutine(look.LookCoroutine);

            SetInputLocked(true);
            look.LookCoroutine = StartCoroutine(look.LookAt(target));
        }

        private void HandleLookAtEnd() {
            if (look.LookCoroutine != null) {
                StopCoroutine(look.LookCoroutine);
                look.LookCoroutine = null;
            }
            _returnCoroutine = StartCoroutine(look.ReturnToSaved());
        }
    }
}