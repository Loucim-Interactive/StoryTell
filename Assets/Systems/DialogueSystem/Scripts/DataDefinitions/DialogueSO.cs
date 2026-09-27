using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace DialogueSystem.Scripts {
    [CreateAssetMenu(fileName = "DialogueSO", menuName = "DialogueSystem/DialogueSO")]
    public class DialogueSO : ScriptableObject {
        [Header("Settings")]
        [SerializeField] private float _extraReadTime = 1f;
        [SerializeField] private ETextSpeed _textSpeed = ETextSpeed.Medium;
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Header("Dialogue")]
        [SerializeField] private ESpeakers _speaker;
        [TextArea] [SerializeField] private string _dialogueText;
        [SerializeField] private AudioClip _voiceClip;
        [SerializeField] private DialogueSO _subDialogueSO;

        [Header("Player response (optional)")]
        [Tooltip("Shown after this line's typing, voice, and reading time finish. Empty keeps the existing linear flow.")]
        [SerializeField] private List<DialogueChoice> choices = new();
        public IReadOnlyList<DialogueChoice> Choices => choices;

        public ESpeakers SpeakerName => _speaker;
        public ETextSpeed TextSpeed => _textSpeed;
        public string DialogueText => _dialogueText;
        public Transform LookAtTarget => _lookAtTarget;
        public AudioClip VoiceClip => _voiceClip;
        public TMP_FontAsset FontAsset => _fontAsset;
        public DialogueSO SubDialogueSO => _subDialogueSO;
        public float ExtraReadTime => _extraReadTime;
    }

    public enum DialogueChoiceOutcome { Continue, Branch, End }

    [Serializable]
    public class DialogueChoice
    {
        [SerializeField] private string label;
        [SerializeField] private DialogueChoiceOutcome outcome;
        [Tooltip("For Branch: replaces the rest of the current conversation. It may contain more choices.")]
        [SerializeField] private ConversationSO nextConversation;
        [SerializeField] private UnityEvent onSelected;

        public string Label => label;
        public DialogueChoiceOutcome Outcome => outcome;
        public ConversationSO NextConversation => nextConversation;
        public void RaiseSelected() => onSelected?.Invoke();
    }
}
