using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using Systems.DialogueSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.Missions;
using Systems.Missions.FindObjects;
using Systems.WalkieSystem.Scripts;
using Systems.WeaponSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(150)]
    public sealed class MainPresentation : MonoBehaviour
    {
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private InspectionScript inspection;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private LetterManager letter;
        [SerializeField] private LetterAssemblyPuzzle puzzle;
        [SerializeField] private MissionsUIManager missions;
        [SerializeField] private PlayerControllerScript movement;
        [SerializeField] private CameraControllerScript look;
        [SerializeField] private InteractionScript interaction;
        [SerializeField] private WalkieTalkieScript radio;
        [SerializeField] private PlayerWeaponController weapon;
        [SerializeField] private GameObject mapLegend, assemblyHint;
        [SerializeField] private TMP_Text assemblyHintText;
        [SerializeField] private InspectableDocument completedLetter;
        [SerializeField,TextArea] private string[] letterParts;
        [SerializeField] private GameObject combatInstructions;
        [SerializeField] private UiSessionCoordinator session;
        private bool originalInstructionsActive;
        private InputAction assemble;
        private bool mapInteractionLocked, interactionBeforeMap, puzzleLocked, movingBefore, interactingBefore, mapBefore, radioBefore;
        public bool HasMap { get; private set; }
        public event System.Action MapUnlocked;
        public bool BlocksWeapon => session&&session.Mode!=UiMode.None||inspection.IsInspecting||puzzle.IsOpen||map.IsOpen||dialogue.IsInConversation||(radio&&radio.IsEquipped);
        public PhysicalMapController Map => map;
        public LetterAssemblyPuzzle Puzzle => puzzle;
        public string LetterText => completedLetter.transcription;
        public string LetterFragmentText(int index) => letterParts[index];
        private void OnEnable(){assemble ??= new InputAction("ReadOrAssembleLetter",InputActionType.Button,"<Keyboard>/l");assemble.Enable();if(map){map.MapOpened+=MapOpened;map.MapClosed+=MapClosed;}}
        private void Start(){map.SetOpen(false,true);map.enabled=false;originalInstructionsActive=combatInstructions&&combatInstructions.activeSelf;}
        private void OnDisable()
        {
            assemble?.Disable();if(puzzleLocked)EndLetterAssembly();ReleaseMapInteraction();if(missions)missions.SetOverlaySuppressed(false);if(map)map.SetOpen(false,true);
            if(map){map.MapOpened-=MapOpened;map.MapClosed-=MapClosed;}if(session)session.Release(this);
        }
        private void OnDestroy(){assemble?.Dispose();}
        public void UnlockMap(){if(HasMap)return;HasMap=true;map.enabled=true;MapUnlocked?.Invoke();}
        private void MapOpened(){if(session&&!session.TryAcquire(UiMode.Map,this))map.SetOpen(false,true);}
        private void MapClosed(){if(session)session.Release(this);}
        private void LateUpdate()
        {
            bool modal=inspection.IsInspecting||puzzle.IsOpen||dialogue.IsInConversation||(session&&(session.Mode==UiMode.Pause||session.Mode==UiMode.Radio));
            if((modal||(radio&&radio.IsEquipped))&&map.IsOpen)map.SetOpen(false,true);
            bool open=HasMap&&map.enabled&&map.IsOpen;
            if(combatInstructions)combatInstructions.SetActive(originalInstructionsActive&&!modal&&!open&&weapon.EquippedWeapon==null);
            if(!session&&open&&!mapInteractionLocked){interactionBeforeMap=interaction.enabled;interaction.enabled=false;mapInteractionLocked=true;}
            if(!open)ReleaseMapInteraction();
            missions.SetOverlaySuppressed(open||modal);
            mapLegend.SetActive(open);
            bool ready=letter.CollectedCount==4&&!modal&&!open;
            assemblyHint.SetActive(ready);
            assemblyHintText.text=letter.LetterAssembled?"[L] Leer carta":"[L] Reconstruir carta";
            weapon.SetPresentationBlocked(BlocksWeapon);
            map.SetKeyboardInputEnabled(HasMap&&!modal&&!(radio&&radio.IsEquipped));
            if(ready&&assemble.WasPressedThisFrame())
            {
                if(!letter.LetterAssembled)BeginLetterAssembly();
                else {map.SetOpen(false,true);completedLetter.gameObject.SetActive(true);Systems.EventSystem.Scripts.GameEventBus.Raise(Systems.EventSystem.Scripts.GameplayEvents.StartInspection,completedLetter.gameObject);}
            }
        }
        private void ReleaseMapInteraction(){if(!mapInteractionLocked)return;interaction.enabled=interactionBeforeMap;mapInteractionLocked=false;}
        public bool BeginLetterAssembly()
        {
            if(letter.CollectedCount!=4||puzzle.IsOpen||inspection.IsInspecting||dialogue.IsInConversation)return false;
            if(session)
            {
                map.SetOpen(false,true);
                if(!session.TryAcquire(UiMode.Puzzle,this))return false;
                puzzleLocked=true;puzzle.Open();return true;
            }
            map.SetOpen(false,true);ReleaseMapInteraction();movingBefore=movement.enabled;interactingBefore=interaction.enabled;mapBefore=map.enabled;radioBefore=radio&&radio.enabled;
            movement.enabled=false;interaction.enabled=false;look.SetInputLocked(true);map.enabled=false;if(radio)radio.enabled=false;puzzleLocked=true;puzzle.Open();return true;
        }
        public void CompleteLetterAssembly(){if(puzzle.Complete)letter.MarkLetterAssembled();}
        public void EndLetterAssembly()
        {
            if(!puzzleLocked)return;puzzleLocked=false;
            if(session){session.Release(this);return;}
            movement.enabled=movingBefore;interaction.enabled=interactingBefore;look.SetInputLocked(false);map.enabled=mapBefore;if(radio)radio.enabled=radioBefore;
        }
    }
}
