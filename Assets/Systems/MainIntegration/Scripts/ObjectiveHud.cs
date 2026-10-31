using Systems.Missions.FindObjects;
using Systems.DialogueSystem.Scripts;
using StoryTell.PhysicalMap;
using Systems.WeaponSystem;
using Systems.InspectionSystem.Scripts;
using TMPro;
using UnityEngine;
using DialogueSystem.Scripts;
namespace StoryTell.MainIntegration
{
    public sealed class ObjectiveHud:MonoBehaviour
    {
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private LetterManager letter;
        [SerializeField] private Transform player;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private InspectionScript inspection;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private LetterAssemblyPuzzle puzzle;
        [SerializeField] private PlayerWeaponController weapon;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private MissionAnnouncement feedback;
        [SerializeField] private Systems.Missions.MissionsUIManager missions;
        [SerializeField] private SpeakerScript mateo;
        [SerializeField] private LevelProgress progress;
        [SerializeField] private UiSessionCoordinator session;
        private bool dirty=true;
        private bool initialized, letterDone, hadMap;
        private int equipmentCount, letterCount;
        public string CurrentObjective {get;private set;}
        // Matches the four authored map references; no future events are disclosed.
        public int DestinationZone {get;private set;}
        private void OnEnable(){equipment.Changed+=MarkDirty;letter.Changed+=MarkDirty;if(mateo)mateo.ConversationCompleted+=MarkDirty;if(progress)progress.Changed+=MarkDirty;}
        private void OnDisable(){equipment.Changed-=MarkDirty;letter.Changed-=MarkDirty;if(mateo)mateo.ConversationCompleted-=MarkDirty;if(progress)progress.Changed-=MarkDirty;}
        private void MarkDirty()=>dirty=true;
        public void ResetFeedback(){initialized=false;dirty=true;}
        private void LateUpdate()
        {
            if(dirty||map.enabled&&!hadMap){dirty=false;RefreshObjectives();}
            bool visible=!(map.IsOpen||inspection.IsInspecting||dialogue.IsInConversation||puzzle.IsOpen||(session&&session.Mode!=UiMode.None));
            if(panel.activeSelf!=visible)panel.SetActive(visible);
        }
        private void RefreshObjectives()
        {
            string objective;
            DestinationZone=0;
            if(!equipment.MissionStarted)objective="Hablá con Diego\nJunto a la carpa de equipamiento.";
            else if(!equipment.MissionCompleted)objective=$"Recogé tu equipo · {equipment.EquippedCount}/3\nEn la mesa de equipamiento.";
            else if(letter.MissionStarted&&!letter.MissionCompleted)
            {
                DestinationZone=1;
                objective=letter.CollectedCount<4?$"Buscá los fragmentos · {letter.CollectedCount}/4\nPreguntales a Simón y José.":"Devolvé la carta a Mateo";
            }
            else if(!letter.MissionStarted&&mateo&&!mateo.HasSpoken&&(!progress||progress.Stage==RouteStage.Camp)){DestinationZone=1;objective="Hablá con Mateo\nEstá en el campamento.";}
            else if(progress&&progress.Stage==RouteStage.Battle){DestinationZone=3;objective="Seguí por el camino";}
            else if(!progress||progress.Stage==RouteStage.Camp){DestinationZone=2;objective="Andá al puesto avanzado";}
            else {DestinationZone=3;objective="Andá al frente\nPor el paso entre los sacos de arena.";}
            CurrentObjective=objective;if(label.text!=objective)label.text=objective;
            if(initialized&&feedback)
            {
                if(equipment.EquippedCount>equipmentCount)feedback.ShowPickup($"Recogiste equipo · {equipment.EquippedCount}/3");
                if(letter.CollectedCount>letterCount)feedback.ShowPickup($"Recogiste un fragmento · {letter.CollectedCount}/4");
                if(letter.MissionCompleted&&!letterDone)feedback.ShowMessage("✓ Carta entregada");
                if(map.enabled&&!hadMap)feedback.ShowPickup("Mapa guardado · [M] Consultar");
            }
            initialized=true;equipmentCount=equipment.EquippedCount;letterCount=letter.CollectedCount;letterDone=letter.MissionCompleted;hadMap|=map.enabled;
            if(missions)missions.ShowOnlyMission(null);
        }
    }
}
