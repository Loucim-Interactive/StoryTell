using System;
using System.Collections;
using UnityEngine;

namespace Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera
{
    [Serializable]
    public class CameraLookAtController
    {
        [Header("Look At Settings")]
        [SerializeField] private float turnSpeed   = 3f;   // how fast it rotates toward target
        [SerializeField] private float returnSpeed = 4f;   // how fast it returns

        // Set by CameraControllerScript before use
        private Transform _playerBody;
        private Transform _cameraTransform;
        private CameraControllerScript _owner;

        private Quaternion _savedBodyRot;
        private float      _savedPitch;

        public Coroutine LookCoroutine;

        public void Initialize(Transform playerBody, Transform cameraTransform, CameraControllerScript owner)
        {
            _playerBody      = playerBody;
            _cameraTransform = cameraTransform;
            _owner           = owner;
        }

        public IEnumerator LookAt(Transform target)
        {
            // Save where we were
            _savedBodyRot = _playerBody.rotation;
            _savedPitch   = _owner.CameraPitch;

            // --- Phase 1: rotate toward target ---
            while (true)
            {
                Vector3 toTarget    = target.position - _cameraTransform.position;
                Quaternion fullLook = Quaternion.LookRotation(toTarget);

                float targetYaw   = fullLook.eulerAngles.y;
                float targetPitch = -fullLook.eulerAngles.x; // Unity pitch is inverted for camera

                // Rotate body yaw
                Quaternion desiredBodyRot = Quaternion.Euler(0f, targetYaw, 0f);
                _playerBody.rotation = Quaternion.Slerp(
                    _playerBody.rotation, desiredBodyRot, Time.deltaTime * turnSpeed);

                // Drive pitch through owner so clamp logic is respected
                _owner.SetCameraPitch(Mathf.LerpAngle(_owner.CameraPitch, targetPitch, Time.deltaTime * turnSpeed));

                // Close enough — hold frame, wait for EndLookAt
                float yawDelta   = Quaternion.Angle(_playerBody.rotation, desiredBodyRot);
                float pitchDelta = Mathf.Abs(Mathf.DeltaAngle(_owner.CameraPitch, targetPitch));

                if (yawDelta < 0.5f && pitchDelta < 0.5f)
                    break;

                yield return null;
            }

            // --- Phase 2: hold until EndLookAt interrupts ---
            // (coroutine is stopped externally by EndLookAt)
            while (true) yield return null;
        }

        public IEnumerator ReturnToSaved()
        {
            while (true)
            {
                _playerBody.rotation = Quaternion.Slerp(
                    _playerBody.rotation, _savedBodyRot, Time.deltaTime * returnSpeed);

                _owner.SetCameraPitch(Mathf.LerpAngle(_owner.CameraPitch, _savedPitch, Time.deltaTime * returnSpeed));

                float yawDelta   = Quaternion.Angle(_playerBody.rotation, _savedBodyRot);
                float pitchDelta = Mathf.Abs(Mathf.DeltaAngle(_owner.CameraPitch, _savedPitch));

                if (yawDelta < 0.5f && pitchDelta < 0.5f)
                {
                    // Snap clean at the end
                    _playerBody.rotation = _savedBodyRot;
                    _owner.SetCameraPitch(_savedPitch);
                    _owner.SetInputLocked(false);
                    yield break;
                }

                yield return null;
            }
        }
    }
}