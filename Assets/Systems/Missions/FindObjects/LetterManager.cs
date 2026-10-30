using System.Collections.Generic;
using DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using UnityEngine;

namespace Systems.Missions.FindObjects {
    public class LetterManager : PickableManager<LetterInteractableScript>
    {
        [Header("Refs")]
        [SerializeField] private MissionsUIManager _missionsUI;
        [SerializeField] private LetterInteractableScript[] fragments;
        [SerializeField] private SpeakerScript missionGiver;
        [SerializeField] private ConversationSO searchingConversation;
        [SerializeField] private ConversationSO returnConversation;
        [SerializeField] private ConversationSO completedConversation;

        [Header("Mission")]
        [SerializeField] private string missionTitle = "La carta de Mateo";
        [SerializeField] private string returnObjective = "Devolverla";

        public bool LetterAssembled { get; private set; }
        public bool MarkLetterAssembled() { if (_collected.Count != fragments.Length || LetterAssembled) return false; LetterAssembled = true; Changed?.Invoke(); return true; }
        public event System.Action Changed;
        private readonly HashSet<LetterInteractableScript> _collected = new();
        public bool MissionStarted { get; private set; }
        public string MissionTitle => missionTitle;
        public bool MissionCompleted { get; private set; }
        public int CollectedCount => _collected.Count;

        private void Awake() {
            if (fragments == null) return;
            foreach (var fragment in fragments)
                if (fragment) fragment.gameObject.SetActive(false);
        }

        protected override void OnPickup(LetterInteractableScript obj) {
            if (!MissionStarted || !obj || fragments == null || System.Array.IndexOf(fragments, obj) < 0 || !_collected.Add(obj)) return;
            // Inspection pickups arrive only after the object has been restored to the world.
            obj.gameObject.SetActive(false);
            if (MissionStarted) RefreshProgress();
            Changed?.Invoke();
        }

        public void StartMission() {
            if (MissionStarted || fragments == null || fragments.Length == 0) return;
            if (!_missionsUI) _missionsUI = FindFirstObjectByType<MissionsUIManager>();
            if (!_missionsUI) {
                Debug.LogWarning("The letter mission needs a MissionsUIManager.", this);
                return;
            }

            MissionStarted = true;
            foreach (var fragment in fragments)
                if (fragment) fragment.gameObject.SetActive(true);
            _missionsUI.AddMission(missionTitle);
            for (int i = 0; i < fragments.Length; i++)
                _missionsUI.AddItemToMission(missionTitle, FragmentObjective(i));
            _missionsUI.AddItemToMission(missionTitle, returnObjective);
            _missionsUI.ShowMissionsUI();
            RefreshProgress();
            Changed?.Invoke();
        }

        public void CompleteMission() {
            if (!MissionStarted || MissionCompleted || _collected.Count != fragments.Length) return;
            MissionCompleted = true;
            _missionsUI.SetItemCompleted(missionTitle, returnObjective);
            if (missionGiver) missionGiver.SetConversation(completedConversation);
            Changed?.Invoke();
        }
        public int CollectedMask { get { int mask=0;for(int i=0;i<fragments.Length;i++)if(_collected.Contains(fragments[i]))mask|=1<<i;return mask; } }
        public void RestoreProgress(bool started,int mask,bool complete,bool assembled)
        {
            if(!started)return;
            StartMission();
            for(int i=0;i<fragments.Length;i++)if((mask&(1<<i))!=0)OnPickup(fragments[i]);
            if(assembled)MarkLetterAssembled();
            if(complete)CompleteMission();
        }

        private void RefreshProgress() {
            for (int i = 0; i < fragments.Length; i++)
                _missionsUI.SetItemCompleted(missionTitle, FragmentObjective(i), _collected.Contains(fragments[i]));
            if (missionGiver) missionGiver.SetConversation(
                _collected.Count == fragments.Length ? returnConversation : searchingConversation);
        }

        private string FragmentObjective(int index) => $"Pieza {index + 1}";
    }
}
