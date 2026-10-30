using System.IO;
using SceneSystem.Scripts;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

namespace StoryTell.MainIntegration
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuScreenController:MonoBehaviour
    {
        [SerializeField] private bool campaignSelection;
        [SerializeField] private StyleSheet theme;
        [SerializeField] private SceneTransitionProfile transition;
        private UIDocument document;
        private VisualElement options,frame;
        private bool loading;
        public VisualElement Root=>document.rootVisualElement;
        public static string SavePath=>Application.isBatchMode?Path.Combine(Application.temporaryCachePath,"StoryTellMenuTests","checkpoint.json"):Path.Combine(Application.persistentDataPath,"level1-checkpoint.json");
        public static bool HasSave=>new CheckpointStore(SavePath).TryRead(out _);
        private void OnEnable()
        {
            document=GetComponent<UIDocument>();Root.Clear();document.visualTreeAsset.CloneTree(Root);if(!Root.styleSheets.Contains(theme))Root.styleSheets.Add(theme);
            Root.style.position=Position.Absolute;Root.style.left=0;Root.style.right=0;Root.style.top=0;Root.style.bottom=0;
            Time.timeScale=1;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            AudioListener.volume=ComfortSettings.Current.master;
            if(campaignSelection)BindCampaign();else BindMain();
            Root.RegisterCallback<GeometryChangedEvent>(Fit);
            Root.RegisterCallback<KeyDownEvent>(KeyDown);
            Root.schedule.Execute(()=>{Fit(null);var first=Root.Q<Button>("continue-button");if(first!=null&&!first.enabledInHierarchy)first=Root.Q<Button>("part-1-button");first?.Focus();});
        }
        private void OnDisable(){var root=document?document.rootVisualElement:null;if(root!=null){root.UnregisterCallback<GeometryChangedEvent>(Fit);root.UnregisterCallback<KeyDownEvent>(KeyDown);}}
        private void Bind(string name,System.Action action){var button=Root.Q<Button>(name);if(button!=null)button.clicked+=action;}
        private void BindMain()
        {
            Bind("continue-button",ContinueGame);Bind("new-game-button",OpenSelection);Bind("exit-button",Exit);
            options=Root.Q("options-overlay");Bind("options-button",()=>options.style.display=DisplayStyle.Flex);Bind("close-options-button",()=>options.style.display=DisplayStyle.None);
            var volume=Root.Q<Slider>("volume-slider");volume.SetValueWithoutNotify(ComfortSettings.Current.master);volume.RegisterValueChangedCallback(e=>{var setting=ComfortSettings.Current;setting.master=e.newValue;ComfortSettings.Apply(setting);AudioListener.volume=setting.master;});
            var fullscreen=Root.Q<Toggle>("fullscreen-toggle");fullscreen.SetValueWithoutNotify(Screen.fullScreen);fullscreen.RegisterValueChangedCallback(e=>Screen.fullScreen=e.newValue);
        }
        private void BindCampaign()
        {
            frame=Root.Q("notebook-frame");
            Root.Q<Image>("notebook-background").image=Resources.Load<Texture2D>("StoryMenus/Images/CampaignNotebook");Root.Q<Image>("notebook-background").scaleMode=ScaleMode.ScaleToFit;
            Root.Q<Image>("campaign-photo").image=Resources.Load<Texture2D>("StoryMenus/Images/MalvinasPhoto");Root.Q<Image>("campaign-photo").scaleMode=ScaleMode.ScaleToFit;
            Bind("continue-button",()=>Play(false));Bind("start-button",()=>Play(true));Bind("restart-button",()=>Play(true));Bind("part-1-button",()=>Play(true));Bind("back-button",Back);
            Root.Q<Button>("continue-button").SetEnabled(HasSave);Root.Q<Button>("restart-button").SetEnabled(HasSave);
            for(int i=2;i<=3;i++){var button=Root.Q<Button>("part-"+i+"-button");button.SetEnabled(false);button.text="PARTE "+i+" · PRÓXIMAMENTE";}
            Root.Q<Label>("campaign-status").text=HasSave?"CHECKPOINT DISPONIBLE":"LISTO PARA COMENZAR";
        }
        private void Fit(GeometryChangedEvent _)
        {
            if(frame==null)return;float scale=Mathf.Min(Root.resolvedStyle.width/1536,Root.resolvedStyle.height/1024)*.97f;
            if(float.IsNaN(scale)||scale<=0)return;frame.style.scale=new Scale(new Vector3(scale,scale,1));
        }
        private void KeyDown(KeyDownEvent e){if(e.keyCode!=KeyCode.Escape)return;if(options!=null&&options.resolvedStyle.display!=DisplayStyle.None)options.style.display=DisplayStyle.None;else if(campaignSelection)Back();}
        public void ContinueGame(){if(HasSave)Play(false);else OpenSelection();}
        public void OpenSelection()=>Navigate("GameSelect");
        public void Back()=>Navigate("MainMenu");
        public void Play(bool fresh){if(loading)return;CheckpointController.SetMenuLaunch(fresh);Navigate("Level1");}
        private void Navigate(string scene)
        {
            if(loading||!Application.CanStreamedLevelBeLoaded(scene))return;
            var manager=SceneTransitionManager.Instance;
            if(manager&&manager.IsTransitioning)return;
            if(!manager)manager=new GameObject("Scene Transitions").AddComponent<SceneTransitionManager>();
            manager.SetDefaultProfileIfMissing(transition);
            loading=true;Root.SetEnabled(false);manager.TransitionToScene(scene,transition);
        }
        private static void Exit(){
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
    }
}
