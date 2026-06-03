using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.SoundSystem.Scripts {
    public class ObjectSoundScript : SoundScript
    {   
        [Header("Object Ground Check Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private bool useColliderForGroundCheck;
        [SerializeField] private float sphereGroundCheckRadius;

        private bool _wasGrounded;
        private bool _isGrounded;
        private float _timeInAir;
        private bool _currentlyPlayingSound;
        
        public void Update() {
            if (!useColliderForGroundCheck) _isGrounded = SphereInGroundCheck(); // else handle with 
            if (!_isGrounded) {
                _timeInAir += Time.deltaTime;
                _wasGrounded = false;
            }

            if (_isGrounded && !_wasGrounded) {
                PlaySound();
                _wasGrounded = true;
            }
        }
        
        private void OnCollisionEnter(Collision collision) 
        {
            if (!useColliderForGroundCheck) return;
            if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0) {
                _isGrounded = true;
            }
        }

        private void OnCollisionExit(Collision collision) 
        {
            if (!useColliderForGroundCheck) return;
            if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0) {
                _isGrounded = false;
            }
        }

        private bool SphereInGroundCheck() {
            return Physics.CheckSphere(transform.position, sphereGroundCheckRadius, groundLayer); 
        }
    }
}
