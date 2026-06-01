using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonControllerSystem.Scripts.Input {
    public class InputScript : MonoBehaviour, PlayerLocomotionInput.IPlayerActions, PlayerLocomotionInput.IUIActions {
        public static InputScript Instance { get; private set; }        
        public Vector2 Scroll { get; private set; }
        public Vector2 Look { get; private set; }
        public Vector2 Move { get; private set; }
        public bool Jumped { get; private set; }
        
        private PlayerLocomotionInput _playerLocomotionInput;

        private void Awake() {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
        }
        private void OnEnable() {
            _playerLocomotionInput = new PlayerLocomotionInput();
            _playerLocomotionInput.Player.SetCallbacks(this);
            _playerLocomotionInput.Player.Enable();
            _playerLocomotionInput.UI.SetCallbacks(this);
            _playerLocomotionInput.UI.Enable();

        }

        private void OnDisable() {
            _playerLocomotionInput.Player.Disable();
            _playerLocomotionInput.UI.Disable();
        }
        public void LateUpdate() {
            Jumped = false;
            Scroll = Vector2.zero;
        }

        public void OnMove(InputAction.CallbackContext context) {
            Move = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context) {
            Look = context.ReadValue<Vector2>();
        }
        
        public void OnScrollWheel(InputAction.CallbackContext context) {
            Scroll = context.ReadValue<Vector2>();
        }


        public void OnAttack(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }

        public void OnInteract(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }

        public void OnCrouch(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }

        public void OnJump(InputAction.CallbackContext context) {
            if (context.performed) Jumped = true;
        }
        
        public void OnSprint(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }

        #region Unused

        public void OnPrevious(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }

        public void OnNext(InputAction.CallbackContext context) {
            //throw new System.NotImplementedException();
        }
        
        public void OnNavigate(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnSubmit(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnCancel(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnPoint(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnClick(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnRightClick(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnMiddleClick(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) {
            // throw new System.NotImplementedException();
        }
        #endregion
    }
}
