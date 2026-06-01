using TMPro;
using UnityEngine;

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

        public ESpeakers SpeakerName => _speaker;
        public ETextSpeed TextSpeed => _textSpeed;
        public string DialogueText => _dialogueText;
        public Transform LookAtTarget => _lookAtTarget;
        public AudioClip VoiceClip => _voiceClip;
        public TMP_FontAsset FontAsset => _fontAsset;
        public DialogueSO SubDialogueSO => _subDialogueSO;
        public float ExtraReadTime => _extraReadTime;
    }
}
