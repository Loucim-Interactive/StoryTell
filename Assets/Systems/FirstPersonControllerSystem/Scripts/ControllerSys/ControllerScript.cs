using FirstPersonControllerSystem.Scripts.Input;
using UnityEngine;

namespace FirstPersonControllerSystem.Scripts.ControllerSys {
    [RequireComponent(typeof(InputScript))]
    public class ControllerScript : MonoBehaviour
    {
        public InputScript ScriptPlayerInput { get; private set; }

        protected void SetupReferences() {
            ScriptPlayerInput = GetComponent<InputScript>();
        }
        
        protected virtual void Awake() {
            SetupReferences();
        }
    }
}
