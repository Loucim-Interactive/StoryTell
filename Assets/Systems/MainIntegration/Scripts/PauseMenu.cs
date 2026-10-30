using SceneSystem.Scripts;
using Systems.DialogueSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.WalkieSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(400)]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private UiSessionCoordinator session;
        [SerializeField] private MainPresentation presentation;
        [SerializeField] private InspectionScript inspection;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private WalkieTalkieScript radio;
        [SerializeField] private CheckpointController checkpoints;
        [SerializeField] private RuntimePerformanceMonitor performance;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text title,continueLabel,status;
        [SerializeField] private Button continueButton,restartButton,newGameButton;
        [SerializeField] private Slider[] sliders;
        [SerializeField] private TMP_Text[] values;
        private InputAction escape;
        private bool initialPrompt;
        private float previousTimeScale;
        private bool previousAudioPause;
        public bool IsOpen { get; private set; }
        private void OnEnable()
        {
            escape??=new InputAction("PauseOrClose",InputActionType.Button,"<Keyboard>/escape");escape.Enable();
            continueButton.onClick.AddListener(Continue);restartButton.onClick.AddListener(Restart);newGameButton.onClick.AddListener(NewGame);
            for(int i=0;i<sliders.Length;i++){int index=i;sliders[i].onValueChanged.AddListener(value=>SetOption(index,value));}
            SyncOptions();
        }
        private void OnDisable()
        {
            Close();escape?.Disable();if(continueButton)continueButton.onClick.RemoveListener(Continue);if(restartButton)restartButton.onClick.RemoveListener(Restart);if(newGameButton)newGameButton.onClick.RemoveListener(NewGame);
            foreach(var slider in sliders)if(slider)slider.onValueChanged.RemoveAllListeners();
        }
        private void OnDestroy()=>escape?.Dispose();
        private void Update()
        {
            if(!escape.WasPressedThisFrame())return;
            if(IsOpen){if(!initialPrompt)Close();return;}
            if(session.LastClosedFrame==Time.frameCount)return;
            switch(session.Mode)
            {
                case UiMode.Map:presentation.Map.SetOpen(false,true);return;
                case UiMode.Inspection:inspection.StopInspect();return;
                case UiMode.Dialogue:dialogue.CancelConversation();return;
                case UiMode.Puzzle:presentation.Puzzle.Close();return;
                case UiMode.Radio:radio.ExitWalkie();return;
            }
            Open();
        }
        public bool Open()
        {
            if(IsOpen)return true;
            if(SceneTransitionManager.Instance&&SceneTransitionManager.Instance.IsTransitioning)return false;
            if(!session.TryAcquire(UiMode.Pause,this))return false;
            IsOpen=true;previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;
            Time.timeScale=0;AudioListener.pause=true;panel.SetActive(true);
            title.text=initialPrompt?"CONTINUAR PARTIDA":"PAUSA";
            continueLabel.text=initialPrompt?"Continuar desde el checkpoint":"Continuar";
            newGameButton.gameObject.SetActive(initialPrompt);restartButton.gameObject.SetActive(!initialPrompt);
            restartButton.interactable=checkpoints.CanRestart;
            status.text=initialPrompt?"Hay una partida guardada.":"[Esc] Volver · Los checkpoints se guardan automáticamente.";
            continueButton.Select();return true;
        }
        public void ShowContinuePrompt(){initialPrompt=true;Open();}
        public void Continue(){if(initialPrompt){checkpoints.ContinueSavedSession();initialPrompt=false;}Close();}
        public void NewGame(){if(!initialPrompt)return;initialPrompt=false;Close();checkpoints.BeginNewSession();}
        public void ExportPerformance(){status.text=performance.Export()?"Informe guardado en la carpeta Performance de tus datos de juego.":"No se pudo guardar el informe.";}
        private void Restart(){if(!initialPrompt)checkpoints.RestartCheckpoint();}
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;if(panel)panel.SetActive(false);Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;if(session)session.Release(this);
        }
        private void SetOption(int index,float value)
        {
            var option=ComfortSettings.Current;
            switch(index){case 0:option.sensitivity=value;break;case 1:option.textSize=value;break;case 2:option.motion=value;break;case 3:option.darkness=value;break;case 4:option.master=value;break;case 5:option.voices=value;break;case 6:option.effects=value;break;}
            ComfortSettings.Apply(option);UpdateValue(index,value);
        }
        private void UpdateValue(int index,float value){values[index].text=index<2?value.ToString("0.00")+"×":Mathf.RoundToInt(value*100)+" %";}
        private void SyncOptions()
        {
            var s=ComfortSettings.Current;float[] data={s.sensitivity,s.textSize,s.motion,s.darkness,s.master,s.voices,s.effects};
            for(int i=0;i<data.Length;i++){sliders[i].SetValueWithoutNotify(data[i]);UpdateValue(i,data[i]);}
        }
    }
}
