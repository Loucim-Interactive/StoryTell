using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace DialogueSystem.Scripts {
    public class DialogueManagerScript : MonoBehaviour {
        
        public UIDialogueScript uiDialogueScript;
        public static DialogueManagerScript Instance { get; private set; }
        public bool IsInConversation => _isInConversation;

        private bool _isInConversation = false;
        private ConversationSO _currentConversation;
        
        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(this.gameObject);
            }

            if (!uiDialogueScript) uiDialogueScript = GameObject.FindAnyObjectByType<UIDialogueScript>();
            if (uiDialogueScript) uiDialogueScript.CleanTexts();
        }
        
        public IEnumerator PlayConversation(ConversationSO conversation) {
            _currentConversation = conversation;
            _isInConversation = true;
            foreach (var dialogue in conversation.Dialogues) {
                Coroutine dialogueRoutine = StartCoroutine(ProcessDialogue(dialogue));
                yield return dialogueRoutine;
            }

            FinishConversation();
        }

        private void FinishConversation() {
            _isInConversation = false;
            _currentConversation = null;
            uiDialogueScript.CleanTexts();
        }

        private IEnumerator ProcessDialogue(DialogueSO dialogue) {
            Coroutine text = StartCoroutine(uiDialogueScript.DisplayDialogue(dialogue));
            yield return text;
        }

    }
}
