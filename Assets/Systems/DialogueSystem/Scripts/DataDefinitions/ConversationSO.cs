using UnityEngine;

namespace DialogueSystem.Scripts {
    [CreateAssetMenu(fileName = "ConversationSO", menuName = "DialogueSystem/ConversationSO")]
    public class ConversationSO : ScriptableObject
    {
        [SerializeField] private bool _autoNext = true;
        public DialogueSO[] Dialogues;
        
        public bool AutoNext => _autoNext;
    }
}
