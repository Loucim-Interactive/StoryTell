using UnityEngine;
using UnityEngine.InputSystem;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponInput : MonoBehaviour
    {
        [SerializeField] private WeaponController controller;
        [SerializeField] private InputActionReference fireAction, aimAction, reloadAction;
        [SerializeField] private Level1WeaponTrial trial;
        private bool ownsFire, ownsAim, ownsReload;
        public InputActionReference FireAction => fireAction;

        private void Awake() { if (controller == null) TryGetComponent(out controller); }
        private void OnEnable()
        {
            ownsFire = Enable(fireAction); ownsAim = Enable(aimAction); ownsReload = Enable(reloadAction);
        }
        private void OnDisable()
        {
            Disable(fireAction, ownsFire); Disable(aimAction, ownsAim); Disable(reloadAction, ownsReload);
            if (controller != null) controller.SetAim(false);
        }
        private void Update()
        {
            if (controller == null || !controller.InputEnabled) return;
            if (trial != null && !trial.CanUseWeapon) { controller.SetAim(false); return; }
            controller.SetAim(aimAction != null ? aimAction.action.IsPressed() : Mouse.current?.rightButton.isPressed == true);
            if (reloadAction != null ? reloadAction.action.WasPressedThisFrame() : Keyboard.current?.rKey.wasPressedThisFrame == true)
                controller.TryReload();
            if (fireAction != null ? fireAction.action.IsPressed() : Mouse.current?.leftButton.isPressed == true)
                controller.TryFire();
        }
        private static bool Enable(InputActionReference reference)
        {
            if (reference == null || reference.action == null || reference.action.enabled) return false;
            reference.action.Enable(); return true;
        }
        private static void Disable(InputActionReference reference, bool owned)
        {
            if (owned && reference != null && reference.action != null) reference.action.Disable();
        }
    }
}
