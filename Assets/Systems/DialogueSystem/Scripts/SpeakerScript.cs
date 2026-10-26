using System.Collections;
using StoryTell.MainIntegration;
using Systems.DialogueSystem.Scripts;
using UnityEngine;

namespace DialogueSystem.Scripts {
    public class SpeakerScript : MonoBehaviour
    {
        [SerializeField] private bool _canRestart  = true;
        [SerializeField] private bool _spokenAlready  = false;
        [SerializeField] private ConversationSO[] _conversationSos;
        [SerializeField] private DialogueManagerScript _dialogueManagerScript;
        
        [SerializeField] private NpcConversationFacing conversationFacing;
        private Coroutine facingRoutine;
        private IEnumerator FaceWhileSpeaking() { if (conversationFacing && Camera.main) conversationFacing.Begin(Camera.main.transform); try { while (_dialogueManagerScript.IsConversationOwnedBy(this)) yield return null; } finally { if (conversationFacing) conversationFacing.End(); facingRoutine = null; } }
        private int _convIndex = 0;

        // Changes the next interaction without interrupting a conversation already playing.
        public void SetConversation(ConversationSO conversation) {
            if (!conversation) return;
            _conversationSos = new[] { conversation };
            _convIndex = 0;
            _spokenAlready = false;
        }
        
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
                if (conversationFacing) facingRoutine = StartCoroutine(FaceWhileSpeaking());
                _spokenAlready = true;
                if (_conversationSos.Length > 1)
                    _convIndex++;
            }
        }

        private void OnDisable() {
            if (_dialogueManagerScript) _dialogueManagerScript.CancelConversation(this);
            if (facingRoutine != null) StopCoroutine(facingRoutine);
            if (conversationFacing) conversationFacing.End();
        }
    }
}
