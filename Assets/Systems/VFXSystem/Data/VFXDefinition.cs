using UnityEngine;

namespace Systems.VFXSystem.New {
    [CreateAssetMenu(fileName = "VFXDefinition", menuName = "VFX/VFX Definition")]
    public class VFXDefinition : ScriptableObject {
        public VFXTypes type;
        public VFXData data;
    }
}
