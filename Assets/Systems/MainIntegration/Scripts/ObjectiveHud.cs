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
        private bool initialized, equipmentDone, letterDone, hadMap;
        private int equipmentCount, letterCount;
        public string CurrentObjective {get;private set;}
        // Matches the four authored map references; no future events are disclosed.
        public int DestinationZone {get;private set;}
        private void LateUpdate()
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
            else if(!letter.MissionStarted&&mateo&&!mateo.HasSpoken&&player.position.z<375){DestinationZone=1;objective="Hablá con Mateo\nEstá en el campamento.";}
            else if(weapon.EquippedWeapon!=null){DestinationZone=3;objective="Seguí por el camino";}
            else if(player.position.z<375){DestinationZone=2;objective="Andá al puesto avanzado";}
            else {DestinationZone=3;objective="Andá al frente\nPor el paso entre los sacos de arena.";}
            CurrentObjective=objective;if(label.text!=objective)label.text=objective;
            if(initialized&&feedback)
            {
                if(equipment.EquippedCount>equipmentCount)feedback.ShowMessage($"Equipo recogido · {equipment.EquippedCount}/3");
                if(equipment.MissionCompleted&&!equipmentDone)feedback.ShowMessage("✓ Equipo completo");
                if(letter.CollectedCount>letterCount)feedback.ShowMessage($"Fragmento recogido · {letter.CollectedCount}/4");
                if(letter.CollectedCount==4&&letterCount<4)feedback.ShowMessage("Devolvé la carta a Mateo");
                if(letter.MissionCompleted&&!letterDone)feedback.ShowMessage("✓ Carta entregada");
                if(map.enabled&&!hadMap)feedback.ShowMessage("Mapa guardado · [M] Consultar");
            }
            initialized=true;equipmentCount=equipment.EquippedCount;letterCount=letter.CollectedCount;equipmentDone=equipment.MissionCompleted;letterDone=letter.MissionCompleted;hadMap|=map.enabled;
            if(missions)missions.ShowOnlyMission(!equipment.MissionCompleted&&equipment.MissionStarted?equipment.MissionTitle:letter.MissionStarted&&!letter.MissionCompleted?letter.MissionTitle:null);
            panel.SetActive(!(map.IsOpen||inspection.IsInspecting||dialogue.IsInConversation||puzzle.IsOpen));
        }
    }
}
