using System;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using UnityEngine;

namespace Systems.FirstPersonControllerSystem.Scripts.ControllerSys
{
    [Serializable]
    public class CameraBobController
    {
        [Header("Bob Settings")]
        [SerializeField] private float walkBobFrequency     = 1.8f;
        [SerializeField] private float sprintBobFrequency   = 2.6f;
        [SerializeField] private float walkBobAmplitudeY    = 0.008f;
        [SerializeField] private float walkBobAmplitudeRoll = 0.6f;
        [SerializeField] private float walkBobAmplitudePitch= 0.3f;
        [SerializeField] private float sprintBobAmplitudeY  = 0.014f;
        [SerializeField] private float sprintBobAmplitudeRoll  = 1.1f;
        [SerializeField] private float sprintBobAmplitudePitch = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float bobSmoothing    = 10f;
        [SerializeField] private float returnSmoothing = 6f;

        // Owns only the bob pivot — look system keeps full ownership of the camera.
        private Transform _bobPivot;
        private PlayerMotorScript _playerMotor;
        private Vector3    _initialLocalPos;
        private Quaternion _initialLocalRot;

        private float   _bobTimer;
        private Vector3 _currentBobPos;
        private Vector3 _bobPosVelocity;
        private Vector3 _currentBobEuler;
        private Vector3 _bobEulerVelocity;

        /// <summary>
        /// Pass the dedicated BobPivot child — NOT the camera itself.
        /// Hierarchy: CameraRig (look) → BobPivot (bob) → Camera
        /// </summary>
        public void Initialize(Transform bobPivot, PlayerMotorScript playerMotor)
        {
            _bobPivot        = bobPivot;
            _playerMotor     = playerMotor;
            _initialLocalPos = bobPivot.localPosition;
            _initialLocalRot = bobPivot.localRotation;
        }

        public void Tick()
        {
            bool isMoving    = _playerMotor.IsMoving;
            bool isSprinting = _playerMotor.IsRunning;
            bool isGrounded  = _playerMotor.IsGrounded;

            if (isMoving && isGrounded)
            {
                float frequency = isSprinting ? sprintBobFrequency  : walkBobFrequency;
                float ampY      = isSprinting ? sprintBobAmplitudeY  : walkBobAmplitudeY;
                float ampRoll   = isSprinting ? sprintBobAmplitudeRoll  : walkBobAmplitudeRoll;
                float ampPitch  = isSprinting ? sprintBobAmplitudePitch : walkBobAmplitudePitch;

                _bobTimer += Time.deltaTime * frequency;

                var targetPos = new Vector3(0f, Mathf.Sin(_bobTimer) * ampY, 0f);

                var targetEuler = new Vector3(
                    Mathf.Cos(_bobTimer)       * ampPitch,
                    0f,
                    Mathf.Sin(_bobTimer * 0.5f) * ampRoll
                );

                _currentBobPos = Vector3.SmoothDamp(
                    _currentBobPos, targetPos, ref _bobPosVelocity, 1f / bobSmoothing);

                _currentBobEuler = Vector3.SmoothDamp(
                    _currentBobEuler, targetEuler, ref _bobEulerVelocity, 1f / bobSmoothing);
            }
            else
            {
                float smoothTime = 1f / returnSmoothing;

                _currentBobPos = Vector3.SmoothDamp(
                    _currentBobPos, Vector3.zero, ref _bobPosVelocity, smoothTime);

                _currentBobEuler = Vector3.SmoothDamp(
                    _currentBobEuler, Vector3.zero, ref _bobEulerVelocity, smoothTime);

                _bobTimer = Mathf.Lerp(_bobTimer,
                    Mathf.Round(_bobTimer / (Mathf.PI * 2f)) * (Mathf.PI * 2f),
                    Time.deltaTime * returnSmoothing);
            }

            _bobPivot.localPosition = _initialLocalPos + _currentBobPos;
            _bobPivot.localRotation = _initialLocalRot * Quaternion.Euler(_currentBobEuler);
        }
    }
}