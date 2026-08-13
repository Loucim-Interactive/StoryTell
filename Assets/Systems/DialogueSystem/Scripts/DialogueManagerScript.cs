using System;
using System.Collections;
using DialogueSystem.Scripts;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.DialogueSystem.Scripts {
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

        private void OnEnable() {
            GameEventBus.Subscribe<string>(GameplayEvents.StateThought, StateThought);
        }
        
        private void OnDisable() {
            GameEventBus.Unsubscribe<string>(GameplayEvents.StateThought, StateThought);
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
        
        public void StateThought(string thought) {
            //_isInConversation = true;
            Coroutine dialogueRoutine = StartCoroutine(PlayThought(thought, ""));
        }
        
        public void StateThought(string thought, string caller) {
            //_isInConversation = true;
            Coroutine dialogueRoutine = StartCoroutine(PlayThought(thought, caller));
        }
        
        private IEnumerator PlayThought(string thought, string caller) {
            Coroutine text = StartCoroutine(uiDialogueScript.DisplayDialogue(thought, caller));
            yield return text;
            uiDialogueScript.CleanTexts();
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
