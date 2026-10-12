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
                (_spokenAlready && !_canRestart) ||
                _conversationSos == null || _conversationSos.Length == 0) return;

            if (_convIndex >= _conversationSos.Length)
                _convIndex = 0;

            ConversationSO conversation = _conversationSos[_convIndex];
            if (!conversation) return;

            if (_dialogueManagerScript.TryStartConversation(conversation, this)) {
                _convIndex++;
                if (_convIndex >= _conversationSos.Length) {
                    _spokenAlready = true;
                    if (_canRestart) _convIndex = 0;
                }
            }
        }

        private void OnDisable() {
            if (_dialogueManagerScript) _dialogueManagerScript.CancelConversation(this);
        }
    }
}
