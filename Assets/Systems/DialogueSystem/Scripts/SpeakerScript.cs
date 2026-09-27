using Systems.DialogueSystem.Scripts;
using UnityEngine;

namespace DialogueSystem.Scripts {
    public class SpeakerScript : MonoBehaviour
    {
        [SerializeField] private bool _canRestart  = true;
        [SerializeField] private bool _spokenAlready  = false;
        [SerializeField] private ConversationSO[] _conversationSos;
        [SerializeField] private DialogueManagerScript _dialogueManagerScript;
        
        private int _convIndex = 0;
        
        private void Start() {
            _dialogueManagerScript = DialogueManagerScript.Instance;
        }
        
        [ContextMenu("Speak Conversation")]
        public void Speak() {
            if (!_dialogueManagerScript ||
                _dialogueManagerScript.IsInConversation ||
                _conversationSos == null || _convIndex >= _conversationSos.Length) return;

            ConversationSO conversation = _conversationSos[_convIndex];
            if (!conversation) return;

            if (_dialogueManagerScript.TryStartConversation(conversation, this)) {
                _spokenAlready = true;
                if (_conversationSos.Length > 1)
                    _convIndex++;
            }
        }

        private void OnDisable() {
            if (_dialogueManagerScript) _dialogueManagerScript.CancelConversation(this);
        }
    }
}
