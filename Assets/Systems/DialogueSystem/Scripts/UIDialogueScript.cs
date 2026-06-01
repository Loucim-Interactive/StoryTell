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
            
            for (var i = 0; i < deStylizedText.Length; i++) {
                _dialogueTextMesh.maxVisibleCharacters = i;
                yield return new WaitForSeconds(GetCharSpeed(dialogue.TextSpeed));
            }
            
            if (dialogue.VoiceClip) yield return new WaitWhile(() => _audio.isPlaying);
            else yield return new WaitForSeconds(dialogue.ExtraReadTime);
        }

        public void CleanTexts() {
            _dialogueTextMesh.text = "";
            _nameTextMesh.text = "";
        }
        
        private float GetCharSpeed(ETextSpeed textSpeed) {
            switch (textSpeed) {
                case ETextSpeed.VerySlow:
                    return 0.8f;
                case ETextSpeed.Slow:
                    return 0.6f;
                case ETextSpeed.Medium:
                    return 0.2f;
                case ETextSpeed.Fast:
                    return 0.1f;
                case ETextSpeed.VeryFast:
                    return 0.05f;
                default:
                    return 0.1f;
            }
        }
    }
}
