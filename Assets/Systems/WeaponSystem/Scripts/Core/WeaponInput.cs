using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponInput : MonoBehaviour
    {
        [SerializeField] private WeaponController controller;

        private void Awake()
        {
            if (controller == null)
                TryGetComponent(out controller);
        }

        private void Update()
        {
            if (controller == null || !controller.InputEnabled)
                return;

            controller.SetAim(IsAimPressed());
            if (WasFirePressed())
                controller.TryFire();
            if (WasReloadPressed())
                controller.TryReload();
        }

        private static bool WasFirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool IsAimPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.rightButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private static bool WasReloadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                return Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
    }
}
