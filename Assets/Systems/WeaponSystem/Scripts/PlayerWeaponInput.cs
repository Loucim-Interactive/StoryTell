using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.WeaponSystem {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerWeaponController))]
    public sealed class PlayerWeaponInput : MonoBehaviour {
        [SerializeField] private InputActionReference fireAction;
        private PlayerWeaponController weapon;
        private bool ownsAction;

        private void Awake() => weapon = GetComponent<PlayerWeaponController>();
        private void OnEnable() {
            ownsAction = fireAction && fireAction.action != null && !fireAction.action.enabled;
            if (ownsAction) fireAction.action.Enable();
        }
        private void OnDisable() {
            if (ownsAction && fireAction && fireAction.action != null) fireAction.action.Disable();
            ownsAction = false;
        }
        private void Update() {
            bool pressed = fireAction && fireAction.action != null
                ? fireAction.action.IsPressed() : Mouse.current?.leftButton.isPressed == true;
            if (pressed && Cursor.lockState == CursorLockMode.Locked) weapon.TryShoot();
        }
    }
}
