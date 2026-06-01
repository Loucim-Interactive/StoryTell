using System;
using UnityEngine;

namespace FirstPersonControllerSystem.Scripts.ControllerSys
{
    [Serializable]
    public class CameraBobController
    {
        [Header("Bob Settings")]
        [SerializeField] private float walkBobFrequency = 1.8f;
        [SerializeField] private float sprintBobFrequency = 2.6f;
        [SerializeField] private float walkBobAmplitudeY = 0.008f;
        [SerializeField] private float walkBobAmplitudeX = 0.004f;
        [SerializeField] private float sprintBobAmplitudeY = 0.014f;
        [SerializeField] private float sprintBobAmplitudeX = 0.007f;

        [Header("Smoothing")]
        [SerializeField] private float bobSmoothing = 10f;
        [SerializeField] private float returnSmoothing = 6f;

        private Transform _cameraTransform;
        private PlayerMotorScript _playerMotor;
        private Vector3 _initialLocalPos;
        private float _bobTimer;
        private Vector3 _currentBob;
        private Vector3 _bobVelocity;

        public void Initialize(Transform cameraTransform, PlayerMotorScript playerMotor)
        {
            _cameraTransform = cameraTransform;
            _playerMotor = playerMotor;
            _initialLocalPos = cameraTransform.localPosition;
        }

        public void Tick()
        {
            bool isMoving = _playerMotor.IsMoving;
            bool isSprinting = false; // temp
            bool isGrounded = _playerMotor.IsGrounded;

            if (isMoving && isGrounded)
            {
                float frequency = isSprinting ? sprintBobFrequency : walkBobFrequency;
                float ampY = isSprinting ? sprintBobAmplitudeY : walkBobAmplitudeY;
                float ampX = isSprinting ? sprintBobAmplitudeX : walkBobAmplitudeX;

                _bobTimer += Time.deltaTime * frequency;

                var targetBob = new Vector3(
                    Mathf.Sin(_bobTimer * 0.5f) * ampX,
                    Mathf.Sin(_bobTimer) * ampY,
                    0f
                );

                _currentBob = Vector3.SmoothDamp(
                    _currentBob, targetBob, ref _bobVelocity, 1f / bobSmoothing);
            }
            else
            {
                _currentBob = Vector3.SmoothDamp(
                    _currentBob, Vector3.zero, ref _bobVelocity, 1f / returnSmoothing);

                _bobTimer = Mathf.Lerp(_bobTimer,
                    Mathf.Round(_bobTimer / (Mathf.PI * 2f)) * (Mathf.PI * 2f),
                    Time.deltaTime * returnSmoothing);
            }

            _cameraTransform.localPosition = _initialLocalPos + _currentBob;
        }
    }
}