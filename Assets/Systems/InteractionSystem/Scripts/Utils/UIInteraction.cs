using InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Utils {
    [System.Serializable]
    public class UIInteraction  {
        public string label;
        public EInteractions interactionType;
        [TextArea] public string characterDescription;
    }
}
