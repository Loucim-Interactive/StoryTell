using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;

namespace DialogueSystem.Scripts {
    public class UIDialogueScript : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TextMeshProUGUI _dialogueTextMesh;
        [SerializeField] private TextMeshProUGUI _nameTextMesh;
        [SerializeField] private AudioSource _audio;

        public IEnumerator DisplayDialogue(DialogueSO dialogue) {
            CleanTexts();
            _dialogueTextMesh.font = dialogue.FontAsset ?? _defaultFont;
            _nameTextMesh.text = dialogue.SpeakerName.ToString();
            
            string stylizedText = DialogueFormatter.Format(dialogue.DialogueText);
            string deStylizedText = DialogueFormatter.Format(dialogue.DialogueText, true);
            
            _dialogueTextMesh.maxVisibleCharacters = 0;
            _dialogueTextMesh.text = stylizedText;

            if (dialogue.VoiceClip) {
                _audio.clip = dialogue.VoiceClip;
                _audio.Play();
            }
            
            for (var i = 0; i < deStylizedText.Length + 1; i++) {
                _dialogueTextMesh.maxVisibleCharacters = i;
                yield return new WaitForSeconds(GetCharSpeed(dialogue.TextSpeed));
            }
            
            if (dialogue.VoiceClip) yield return new WaitWhile(() => _audio.isPlaying);
            else yield return new WaitForSeconds(dialogue.ExtraReadTime);
        }
        
        public IEnumerator DisplayDialogue(string dialogue, AudioClip voiceClip) {
            CleanTexts();
            _dialogueTextMesh.font = _defaultFont;
            _nameTextMesh.text = "";
            
            _dialogueTextMesh.maxVisibleCharacters = 0;
            _dialogueTextMesh.text = dialogue;

            if (voiceClip) {
                _audio.clip = voiceClip;
                _audio.Play();
            }
            
            for (var i = 0; i < dialogue.Length + 1; i++) {
                _dialogueTextMesh.maxVisibleCharacters = i;
                yield return new WaitForSeconds(GetCharSpeed(ETextSpeed.Fast));
            }
            
            if (voiceClip) yield return new WaitWhile(() => _audio.isPlaying);
            else yield return new WaitForSeconds(4); // hardcoded for now
        }
        
        public void CleanTexts() {
            _dialogueTextMesh.text = "";
            _nameTextMesh.text = "";
        }
        
        private float GetCharSpeed(ETextSpeed textSpeed) {
            switch (textSpeed) { // in seconds
                case ETextSpeed.VerySlow:
                    return 0.8f;
                case ETextSpeed.Slow:
                    return 0.6f;
                case ETextSpeed.Medium:
                    return 0.2f;
                case ETextSpeed.Fast:
                    return 0.025f;
                case ETextSpeed.VeryFast:
                    return 0.01f;
                default:
                    return 0.02f;
            }
        }
    }
}
