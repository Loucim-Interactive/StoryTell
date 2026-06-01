using System.Collections;
using UnityEngine;

namespace DialogueSystem.Scripts {
    public class SpeakerScript : MonoBehaviour
    {
        [SerializeField] private bool _spokenAlready  = false;
        [SerializeField] private ConversationSO[] _conversationSos;
        [SerializeField] private DialogueManagerScript _dialogueManagerScript;
        
        private int _convIndex = 0;
        private bool _isSpeaking = false;
        
        private void Start() {
            _dialogueManagerScript = DialogueManagerScript.Instance;
        }
        
        [ContextMenu("Speak Conversation")]
        public void Speak() {
            if (_isSpeaking || !_dialogueManagerScript ||
                _dialogueManagerScript.IsInConversation ||
                _conversationSos == null || _convIndex >= _conversationSos.Length) return;

            ConversationSO conversation = _conversationSos[_convIndex];
            if (!conversation) return;

            _spokenAlready = true;
            _isSpeaking = true;
            _convIndex++;
            StartCoroutine(SpeakConversation(conversation));
        }

        private IEnumerator SpeakConversation(ConversationSO conversation) {
            yield return _dialogueManagerScript.PlayConversation(conversation);
            _isSpeaking = false;
        }
    }
}
