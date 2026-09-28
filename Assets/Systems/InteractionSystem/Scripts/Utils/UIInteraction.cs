using InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Utils {
    [System.Serializable]
    public class UIInteraction  {
        public string label;
        public EInteractions interactionType;
        [Tooltip("Used when no Character Description Lines are filled in.")]
        [TextArea] public string characterDescription;
        [Tooltip("Spoken in order, one line at a time. Blank entries are skipped. Overrides Character Description when populated.")]
        [TextArea] public string[] characterDescriptionLines;
    }
}
