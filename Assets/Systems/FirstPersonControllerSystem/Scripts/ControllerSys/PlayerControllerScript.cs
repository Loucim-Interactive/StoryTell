using UnityEngine;

namespace FirstPersonControllerSystem.Scripts.ControllerSys {
    public class PlayerControllerScript : ControllerScript
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;

        private PlayerMotorScript _playerMotor;
    
        protected override void Awake() {
            base.Awake();
            _playerMotor = GetComponent<PlayerMotorScript>();
        }
    
        private void Update() {
            _playerMotor.UpdateGroundedState();
            _playerMotor.UpdateGravity();
            _playerMotor.UpdateMovement(GetMovementVelocity());
        }

        private Vector3 GetMovementVelocity() {
            Vector2 input = ScriptPlayerInput.Move;
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            return move * walkSpeed;
        }
    }
}
