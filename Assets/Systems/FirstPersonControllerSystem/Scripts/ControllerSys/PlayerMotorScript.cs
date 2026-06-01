using UnityEngine;

namespace FirstPersonControllerSystem.Scripts.ControllerSys {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotorScript : ControllerScript
    {
        [Header("Movement")]
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float groundedGravity = -2f;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isGrounded;

        protected override void Awake() {
            base.Awake();
            _characterController = GetComponent<CharacterController>();
        }

        public void UpdateGroundedState() {
            _isGrounded = _characterController.isGrounded;
        }

        public void UpdateGravity() {
            if (_isGrounded && _velocity.y < 0f) {
                _velocity.y = groundedGravity;
                return;
            }
            _velocity.y += gravity * Time.deltaTime;
        }

        public void UpdateMovement(Vector3 horizontalVelocity) {
            _velocity.x = horizontalVelocity.x;
            _velocity.z = horizontalVelocity.z;
            _characterController.Move(_velocity * Time.deltaTime);
        }
        
        public bool IsGrounded => _characterController.isGrounded;
        public bool IsMoving => new Vector3(
            _characterController.velocity.x, 0, 
            _characterController.velocity.z).sqrMagnitude > 0.01f;
    }
}
