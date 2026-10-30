using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.DialogueSystem.Scripts;
using Systems.Global;
using Systems.InmersionSystem;
using Systems.Missions.FindObjects;
using Systems.WalkieSystem.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StoryTell.MainIntegration
{
    [Serializable] public sealed class SpeakerBinding { public string id; public SpeakerScript speaker; }
    [Serializable] public sealed class StoryTriggerBinding { public string id; public TriggerZoneScript trigger; }
    [Serializable] public sealed class NarrationBinding { public string id; public CharacterNarrationZoneScript zone; }

    public sealed class CheckpointController : MonoBehaviour
    {
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private LetterManager letter;
        [SerializeField] private MainPresentation presentation;
        [SerializeField] private LevelProgress progress;
        [SerializeField] private UiSessionCoordinator session;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private WalkieInteractionMachine radio;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] safeSpawns;
        [SerializeField] private SpeakerBinding[] speakers;
        [SerializeField] private StoryTriggerBinding[] storyTriggers;
        [SerializeField] private NarrationBinding[] narrations;
        [SerializeField] private MissionAnnouncement feedback;
        [SerializeField] private ObjectiveHud objective;
        [SerializeField] private PauseMenu pause;
        [SerializeField] private GameObject mapPickup;
        private CheckpointStore store;
        private bool pendingSave, started, restoring;
        private static CheckpointData pendingRestart;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPendingRestart()=>pendingRestart=null;
        public CheckpointData LastSaved { get; private set; }
        public bool CanRestart => LastSaved != null;
        public bool IsRestoring => restoring;
        public Transform[] SafeSpawns => safeSpawns;

        private IEnumerator Start()
        {
            // The existing mission managers hide their uncollected objects in Awake/Start.
            yield return null;
            if(!Application.isBatchMode)store = new CheckpointStore(Path.Combine(Application.persistentDataPath,"level1-checkpoint.json"));
            if (pendingRestart != null)
            {
                var saved = pendingRestart; pendingRestart = null;
                Restore(saved); LastSaved = saved; started = true;
            }
            else if(Application.isBatchMode)yield break; // Tests use an isolated store explicitly.
            else if (store.TryRead(out var saved))
            {
                LastSaved = saved;
                pause.ShowContinuePrompt();
            }
            else BeginNewSession();
        }
        private void OnEnable()
        {
            equipment.Changed += RequestSave; letter.Changed += RequestSave;
            progress.Changed += RequestSave; presentation.MapUnlocked += RequestSave;
            presentation.Puzzle.ProgressChanged += RequestSave;
            foreach(var binding in speakers) if(binding.speaker) binding.speaker.ConversationCompleted += RequestSave;
        }
        private void OnDisable()
        {
            equipment.Changed -= RequestSave; letter.Changed -= RequestSave;
            progress.Changed -= RequestSave; presentation.MapUnlocked -= RequestSave;
            presentation.Puzzle.ProgressChanged -= RequestSave;
            foreach(var binding in speakers) if(binding.speaker) binding.speaker.ConversationCompleted -= RequestSave;
        }
        public void UseTestStore(string path) { store = new CheckpointStore(path); started = true; }
        public void BeginNewSession() { started = true; RequestSave(); }
        public void ContinueSavedSession()
        {
            if(LastSaved != null) Restore(LastSaved);
            started = true;
        }
        public void RequestSave() { if(!restoring) pendingSave = true; }
        private bool IsSafe => session.Mode == UiMode.None && progress.Stage != RouteStage.Battle && !dialogue.IsThoughtActive && radio.IsFinished;
        private void LateUpdate()
        {
            if(started && pendingSave && store != null && IsSafe) SaveNow();
        }
        public bool SaveNow()
        {
            if(store == null || !IsSafe || restoring) return false;
            var data = Capture();
            pendingSave = false;
            if(!store.TryWrite(data)) { feedback.ShowMessage("No se pudo guardar el checkpoint."); return false; }
            LastSaved = data;
            feedback.ShowMessage("Checkpoint guardado");
            return true;
        }
        public CheckpointData Capture()
        {
            var data = new CheckpointData
            {
                stage=progress.Stage,equipmentStarted=equipment.MissionStarted,equipmentMask=equipment.CollectedMask,
                letterStarted=letter.MissionStarted,letterCompleted=letter.MissionCompleted,letterMask=letter.CollectedMask,
                puzzleMask=presentation.Puzzle.PlacedMask,mapUnlocked=presentation.HasMap,
                speakers=new SpeakerCheckpoint[speakers.Length]
            };
            for(int i=0;i<speakers.Length;i++) data.speakers[i]=new SpeakerCheckpoint{id=speakers[i].id,index=speakers[i].speaker.ConversationIndex,spoken=speakers[i].speaker.HasSpoken};
            var flags=new List<string>();
            foreach(var binding in storyTriggers) if(binding.trigger.HasTriggered) flags.Add(binding.id);
            data.triggeredZones=flags.ToArray();
            flags.Clear();foreach(var binding in narrations)if(binding.zone.HasTriggered)flags.Add(binding.id);data.narratedZones=flags.ToArray();
            return data;
        }
        public bool Restore(CheckpointData data)
        {
            if(data == null || !data.IsValid || (session.Mode != UiMode.None && session.Mode != UiMode.Pause)) return false;
            restoring=true;
            try
            {
                equipment.RestoreProgress(data.equipmentStarted,data.equipmentMask);
                letter.RestoreProgress(data.letterStarted,data.letterMask,data.letterCompleted,data.puzzleMask==15);
                presentation.Puzzle.RestorePlaced(data.puzzleMask);
                if(data.mapUnlocked){presentation.UnlockMap();mapPickup.SetActive(false);}
                foreach(var saved in data.speakers)
                    foreach(var binding in speakers) if(saved!=null && binding.id==saved.id) binding.speaker.RestoreProgress(saved.index,saved.spoken);
                var flags=new HashSet<string>(data.triggeredZones);
                foreach(var binding in storyTriggers) binding.trigger.RestoreTriggered(flags.Contains(binding.id));
                flags=new HashSet<string>(data.narratedZones);foreach(var binding in narrations)binding.zone.RestoreTriggered(flags.Contains(binding.id));
                progress.Restore(data.stage);
                var spawn=safeSpawns[(int)data.stage];
                var cc=player.GetComponent<CharacterController>();bool enabledBefore=cc.enabled;cc.enabled=false;
                player.SetPositionAndRotation(spawn.position,spawn.rotation);cc.enabled=enabledBefore;
                if(enabledBefore)player.GetComponent<PlayerMotorScript>().ResetMotion();
                objective.ResetFeedback();feedback.Clear();
                pendingSave=false;
                return true;
            }
            finally{restoring=false;}
        }
        public void RestartCheckpoint()
        {
            if(LastSaved==null)return;
            pendingRestart=LastSaved;
            pause.Close();
            SceneManager.LoadSceneAsync(gameObject.scene.path);
        }
    }
}
