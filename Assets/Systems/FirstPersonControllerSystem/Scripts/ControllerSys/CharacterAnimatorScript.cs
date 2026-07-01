using System;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Unity.VisualScripting;
using UnityEngine;

namespace Systems.FirstPersonControllerSystem.Scripts.ControllerSys {
    public class CharacterAnimatorScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerMotorScript _playerMotor;

        private int _groundHash;
        private int _walkingHash;
        private int _runningHash;
        private int _crouchingHash;
        private int _armedHash;

        private void Awake() {
            SetupReferences();
            
            _groundHash = Animator.StringToHash("isGrounded");
            _walkingHash = Animator.StringToHash("isWalking");
            _runningHash = Animator.StringToHash("isRunning");
            _crouchingHash = Animator.StringToHash("isCrouching");
            _armedHash = Animator.StringToHash("isArmed");
        }

        private void Update()
        {
            UpdateAnim(
                _playerMotor.IsGrounded, 
                _playerMotor.IsWalking, 
                _playerMotor.IsRunning, 
                false, false
            );
        }


        void UpdateAnim(bool grounded, bool walking, bool running, bool crouching, bool armed) {
            _animator.SetBool(_groundHash, grounded);
            _animator.SetBool(_walkingHash, walking);
            _animator.SetBool(_runningHash, running);
            _animator.SetBool(_crouchingHash, crouching);
            _animator.SetBool(_armedHash, armed);
        }

        void SetupReferences() {
            if (!_animator) _animator = GetComponent<Animator>();
            if (!_playerMotor) _playerMotor = GetComponent<PlayerMotorScript>();
            if (!_characterController) _characterController = GetComponent<CharacterController>();
            
            if (!_playerMotor) Debug.LogWarning("[ANIMATOR] No <player motor> assigned");
            if (!_characterController) Debug.LogWarning("[ANIMATOR] No <character controller> assigned");
            if (!_animator) Debug.LogWarning("[ANIMATOR] No <animator> assigned");
        }
    }
}
