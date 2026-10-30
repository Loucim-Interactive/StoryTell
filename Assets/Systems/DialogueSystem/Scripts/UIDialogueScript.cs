using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueSystem.Scripts
{
    public class UIDialogueScript : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TextMeshProUGUI _dialogueTextMesh, _nameTextMesh;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private float waitSecondsAfterDisplay=4;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text controls;
        private InputAction advance;
        private bool requested, activeLine, manualLine;
        private int version, consumedFrame=-1;
        private float readyAt;
        public bool IsTyping {get;private set;}
        public bool IsWaitingForAdvance {get;private set;}
        public int Page => _dialogueTextMesh.pageToDisplay;
        public int PageCount => Mathf.Max(1,_dialogueTextMesh.textInfo.pageCount);
        public TMP_Text Body => _dialogueTextMesh;
        private void OnEnable(){advance??=new InputAction("AdvanceDialogue",InputActionType.Button);if(advance.bindings.Count==0){advance.AddBinding("<Keyboard>/e");advance.AddBinding("<Keyboard>/space");advance.AddBinding("<Keyboard>/enter");}advance.Enable();}
        private void OnDisable(){advance?.Disable();StopDialogue();}
        private void OnDestroy(){advance?.Dispose();}
        public void RequestAdvance(){if(activeLine&&manualLine&&Time.unscaledTime>=readyAt)requested=true;}
        private bool ConsumeAdvance()
        {
            if(!manualLine||Time.unscaledTime<readyAt||consumedFrame==Time.frameCount)return false;
            if(!requested&&!(advance?.WasPressedThisFrame()??false))return false;
            requested=false;consumedFrame=Time.frameCount;return true;
        }
        public IEnumerator DisplayDialogue(DialogueSO line)=>DisplayDialogue(line,false);
        public IEnumerator DisplayDialogue(DialogueSO line,bool manual,bool choicesFollow=false)
        {
            if(!line)yield break;
            yield return Display(DialogueFormatter.Format(line.DialogueText??""),line.SpeakerName.ToString(),line.FontAsset?line.FontAsset:_defaultFont,line.VoiceClip,GetCharSpeed(line.TextSpeed),line.ExtraReadTime,manual,!choicesFollow);
        }
        public IEnumerator DisplayDialogue(string text,AudioClip voiceClip)=>Display(DialogueFormatter.Format(text??""),"",_defaultFont,voiceClip,.025f,waitSecondsAfterDisplay,false);
        public IEnumerator DisplayDialogue(string text)=>DisplayDialogue(text,(AudioClip)null);
        public IEnumerator DisplayDialogue(string text,string speaker)=>Display(DialogueFormatter.Format(text??""),speaker,_defaultFont,null,.025f,waitSecondsAfterDisplay,false);
        private IEnumerator Display(string text,string speaker,TMP_FontAsset font,AudioClip voice,float interval,float hold,bool manual,bool waitForAdvance=true)
        {
            StopDialogue();int token=++version;activeLine=true;manualLine=manual;readyAt=Time.unscaledTime+.18f;
            _dialogueTextMesh.font=font;_dialogueTextMesh.text=text??"";_dialogueTextMesh.pageToDisplay=1;_dialogueTextMesh.maxVisibleCharacters=0;
            _nameTextMesh.text=speaker??"";_nameTextMesh.gameObject.SetActive(!string.IsNullOrEmpty(speaker));if(panel)panel.alpha=1;
            _dialogueTextMesh.ForceMeshUpdate();int pages=PageCount;
            if(_audio&&voice){_audio.clip=voice;_audio.Play();}
            for(int page=0;page<PageCount&&token==version;page++)
            {
                _dialogueTextMesh.pageToDisplay=page+1;
                int first=_dialogueTextMesh.textInfo.pageCount>0?_dialogueTextMesh.textInfo.pageInfo[page].firstCharacterIndex:0;
                int last=_dialogueTextMesh.textInfo.pageCount>0?_dialogueTextMesh.textInfo.pageInfo[page].lastCharacterIndex+1:_dialogueTextMesh.textInfo.characterCount;
                int visible=first;float clock=0;IsTyping=true;IsWaitingForAdvance=false;Hint(manual?"[E / Espacio] Completar texto · [Esc] Salir":"");
                while(visible<last&&token==version)
                {
                    page=Mathf.Min(page,PageCount-1);_dialogueTextMesh.pageToDisplay=page+1;last=PageEnd(page);
                    if(ConsumeAdvance()){visible=last;_dialogueTextMesh.maxVisibleCharacters=visible;break;}
                    clock+=Time.unscaledDeltaTime;int count=Mathf.FloorToInt(clock/interval);if(count>0){clock-=count*interval;visible=Mathf.Min(last,visible+count);_dialogueTextMesh.maxVisibleCharacters=visible;}yield return null;
                }
                IsTyping=false;if(token!=version)yield break;last=PageEnd(page);_dialogueTextMesh.maxVisibleCharacters=last;
                if(manual&&(waitForAdvance||page<PageCount-1))
                {
                    IsWaitingForAdvance=true;Hint("[E / Espacio] "+(page<PageCount-1?"Página siguiente":"Continuar")+(PageCount>1?$" · {page+1}/{PageCount}":"")+" · [Esc] Salir");
                    while(token==version&&!ConsumeAdvance()){page=Mathf.Min(page,PageCount-1);_dialogueTextMesh.pageToDisplay=page+1;_dialogueTextMesh.maxVisibleCharacters=PageEnd(page);yield return null;}
                    IsWaitingForAdvance=false;
                }
                else
                {
                    float until=Time.unscaledTime+Mathf.Max(.35f,hold);while(token==version&&(Time.unscaledTime<until||_audio&&_audio.isPlaying))yield return null;
                }
            }
            if(token==version){IsTyping=false;IsWaitingForAdvance=false;activeLine=false;Hint("");}
        }
        private void Hint(string text){if(controls)controls.text=text;}
        private int PageEnd(int page)=>_dialogueTextMesh.textInfo.pageCount>0?_dialogueTextMesh.textInfo.pageInfo[Mathf.Clamp(page,0,_dialogueTextMesh.textInfo.pageCount-1)].lastCharacterIndex+1:_dialogueTextMesh.textInfo.characterCount;
        public void CleanTexts()=>StopDialogue();
        public void StopDialogue()
        {
            version++;requested=false;activeLine=false;manualLine=false;IsTyping=false;IsWaitingForAdvance=false;
            if(_audio)_audio.Stop();if(_dialogueTextMesh){_dialogueTextMesh.text="";_dialogueTextMesh.maxVisibleCharacters=int.MaxValue;_dialogueTextMesh.pageToDisplay=1;}if(_nameTextMesh)_nameTextMesh.text="";if(panel)panel.alpha=0;Hint("");
        }
        private static float GetCharSpeed(ETextSpeed speed)=>speed switch{ETextSpeed.VerySlow=>.08f,ETextSpeed.Slow=>.055f,ETextSpeed.Medium=>.035f,ETextSpeed.Fast=>.025f,ETextSpeed.VeryFast=>.012f,_=>.035f};
    }
}
