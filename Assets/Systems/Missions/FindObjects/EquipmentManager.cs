using System.Collections.Generic;
using DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using UnityEngine;

namespace Systems.Missions.FindObjects {
    public class EquipmentManager : PickableManager<EquipmentInteractableScript> {
        [Header("Refs")]
        [SerializeField] private MissionsUIManager _missionsUI;
        [SerializeField] private string _missionName = "Equiparse";
        [SerializeField] private EquipmentInteractableScript[] equipment;
        [SerializeField] private SpeakerScript missionGiver;
        [SerializeField] private ConversationSO searchingConversation;
        [SerializeField] private ConversationSO completedConversation;

        private readonly HashSet<EquipmentInteractableScript> _equipped = new();
        public bool MissionStarted { get; private set; }
        public string MissionTitle => _missionName;
        public bool MissionCompleted { get; private set; }
        public int EquippedCount => _equipped.Count;
        public bool IsEquipped(EquipmentInteractableScript item) => _equipped.Contains(item);

        private void Awake() {
            if (equipment == null) return;
            foreach (var item in equipment)
                if (item) item.gameObject.SetActive(false);
        }

        protected override void OnPickup(EquipmentInteractableScript item) {
            if (!MissionStarted || !item || equipment == null ||
                System.Array.IndexOf(equipment, item) < 0 || !_equipped.Add(item)) return;

            item.gameObject.SetActive(false);
            _missionsUI.SetItemCompleted(_missionName, item.interactableName);
            MissionCompleted = _equipped.Count == equipment.Length;
            if (MissionCompleted && missionGiver) missionGiver.SetConversation(completedConversation);
        }

        public void StartMission() {
            if (MissionStarted || equipment == null || equipment.Length == 0) return;
            foreach (var item in equipment) {
                if (!item) {
                    Debug.LogWarning("Assign all equipment mission items before starting.", this);
                    return;
                }
            }
            if (!_missionsUI) _missionsUI = FindFirstObjectByType<MissionsUIManager>();
            if (!_missionsUI) {
                Debug.LogWarning("The equipment mission needs a MissionsUIManager.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(_missionName)) _missionName = "Equiparse";
            MissionStarted = true;
            _missionsUI.AddMission(_missionName);
            foreach (var item in equipment) {
                item.gameObject.SetActive(true);
                _missionsUI.AddItemToMission(_missionName, item.interactableName);
            }
            _missionsUI.ShowMissionsUI();
            if (missionGiver) missionGiver.SetConversation(searchingConversation);
        }
    }
}
