using Systems.InspectionSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StoryTell.MainIntegration
{
    public sealed class DocumentReader : MonoBehaviour
    {
        [SerializeField] private InspectionScript inspection;
        [SerializeField] private GameObject panel, hint;
        [SerializeField] private TMP_Text title, body, controls;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Systems.Missions.FindObjects.LetterManager letter;
        [SerializeField] private InspectableDocument completedLetter;
        private bool readCompleted;
        private float readingTime;
        private InputAction toggle, wheel;
        private GameObject target;
        private bool visible;
        public bool IsVisible => panel.activeInHierarchy;
        public string Content => body.text;
        private void OnEnable()
        {
            toggle ??= new InputAction("ToggleDocumentText",InputActionType.Button,"<Keyboard>/t");
            wheel ??= new InputAction("ScrollDocumentText",InputActionType.Value,"<Mouse>/scroll/y");
            toggle.Enable();wheel.Enable();
        }
        private void OnDisable(){toggle?.Disable();wheel?.Disable();if(panel)panel.SetActive(false);if(hint)hint.SetActive(false);}
        private void OnDestroy(){toggle?.Dispose();wheel?.Dispose();}
        private void LateUpdate()
        {
            GameObject current=inspection.IsInspecting?inspection.CurrentTarget:null;
            var document=current?current.GetComponent<InspectableDocument>():null;
            if(!document||string.IsNullOrWhiteSpace(document.transcription))
            {if(readCompleted&&letter)letter.MarkLetterRead();readCompleted=false;readingTime=0;target=null;panel.SetActive(false);hint.SetActive(false);return;}
            if(target!=current)
            {
                target=current;title.text=document.title;body.text=document.transcription;visible=true;
                Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;inspection.SetDocumentSide(true);
            }
            if(toggle.WasPressedThisFrame())ToggleText();
            panel.SetActive(visible);hint.SetActive(true);
            bool fragment=current.GetComponent<Systems.InteractionSystem.Scripts.Interactables.Pickables.LetterInteractableScript>();
            controls.text=(visible?"[T] Ocultar texto":"[T] Mostrar texto")+"   ·   [E / Esc] "+(fragment?"Guardar fragmento":"Dejar documento")+"\nCLIC IZQ + RATÓN · Girar    WASD · Mover    RUEDA · Leer";
            if(!visible)return;
            if(document==completedLetter){readingTime+=Time.unscaledDeltaTime;if(readingTime>=.5f)readCompleted=true;}
            float height=scroll.content.rect.height-scroll.viewport.rect.height;
            if(height>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+wheel.ReadValue<float>()/120*70/height);
        }
        public void ToggleText(){visible=!visible;inspection.SetDocumentSide(visible);panel.SetActive(visible);}
    }
}
