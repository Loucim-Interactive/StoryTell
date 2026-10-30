using Systems.DialogueSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.WeaponSystem
{
    [DisallowMultipleComponent,RequireComponent(typeof(PlayerWeaponController))]
    public sealed class PlayerWeaponInput : MonoBehaviour
    {
        [SerializeField]private InputActionReference fireAction;
        private PlayerWeaponController weapon;
        private InteractionScript interaction;
        private InspectionScript inspection;
        private DialogueManagerScript dialogue;
        private InputAction reload,aim;
        private bool ownsAction;
        private bool waitForFireRelease;
        private void Awake(){weapon=GetComponent<PlayerWeaponController>();interaction=GetComponentInParent<InteractionScript>();inspection=GetComponentInParent<InspectionScript>();}
        private void Start(){dialogue=DialogueManagerScript.Instance;}
        private void OnEnable()
        {
            ownsAction=fireAction&&fireAction.action!=null&&!fireAction.action.enabled;if(ownsAction)fireAction.action.Enable();
            waitForFireRelease=true;
            reload??=new InputAction("NativeReload",InputActionType.Button,"<Keyboard>/r");aim??=new InputAction("NativeAim",InputActionType.Button,"<Mouse>/rightButton");reload.Enable();aim.Enable();
        }
        private void OnDisable(){if(ownsAction&&fireAction&&fireAction.action!=null)fireAction.action.Disable();ownsAction=false;reload?.Disable();aim?.Disable();if(weapon)weapon.SetAim(false);}
        private void OnDestroy(){reload?.Dispose();aim?.Dispose();}
        private void Update()
        {
            bool pressed=fireAction&&fireAction.action!=null?fireAction.action.IsPressed():Mouse.current?.leftButton.isPressed==true;
            if((inspection&&inspection.IsInspecting)||(dialogue&&dialogue.IsInConversation)||(interaction&&interaction.CurrentTarget)){weapon.SetAim(false);return;}
            weapon.SetAim(aim.IsPressed());if(reload.WasPressedThisFrame())weapon.TryReload();
            TryFireFromInput(pressed||Mouse.current?.leftButton.isPressed==true,Cursor.lockState==CursorLockMode.Locked);
        }
        // The adapter also allows deterministic verification without an OS cursor lock.
        public bool TryFireFromInput(bool pressed,bool pointerLocked)
        {
            if(!isActiveAndEnabled)return false;
            if(waitForFireRelease){if(!pressed)waitForFireRelease=false;return false;}
            if((inspection&&inspection.IsInspecting)||(dialogue&&dialogue.IsInConversation)||(interaction&&interaction.CurrentTarget))return false;
            return pressed&&pointerLocked&&weapon.TryShoot();
        }
    }
}
