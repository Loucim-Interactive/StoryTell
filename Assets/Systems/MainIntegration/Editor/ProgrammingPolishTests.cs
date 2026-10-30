#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.DialogueSystem.Scripts;
using Systems.EventSystem.Scripts;
using Systems.InmersionSystem;
using Systems.InspectionSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.WeaponSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public sealed class ProgrammingPolishTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    const string Scene="Assets/Scenes/Level1.unity";
    static string TestPath=>Path.GetFullPath("Library/Programming-checkpoint-test.json");

    [Test]public void StoreRejectsInvalidStatesAndRecoversBackupAfterCorruptWrite()
    {
        string path=Path.Combine(Path.GetTempPath(),"StoryTell-test-"+Guid.NewGuid()+".json");
        try
        {
            var store=new CheckpointStore(path);Assert.That(store.TryRead(out _),Is.False);
            Assert.That(store.TryWrite(new CheckpointData{letterCompleted=true}),Is.False);
            Assert.That(store.TryWrite(new CheckpointData{stage=RouteStage.Battle}),Is.False);
            Assert.That(store.TryWrite(new CheckpointData{version=12}),Is.False);
            Assert.That(store.TryWrite(new CheckpointData()),Is.True);
            Assert.That(store.TryWrite(new CheckpointData{equipmentStarted=true,equipmentMask=3}),Is.True);
            File.WriteAllText(path,"{broken");Assert.That(store.TryRead(out var recovered),Is.True);Assert.That(recovered.equipmentMask,Is.Zero);
            File.WriteAllText(path+".bak","{broken");Assert.That(store.TryRead(out _),Is.False);
        }
        finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
    [UnityTest]public IEnumerator CheckpointRestoresItemsLetterPuzzleSpeakersMapAndNarrationInFreshScene()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
        var eq=One<EquipmentManager>();eq.RestoreProgress(true,7);var letter=One<LetterManager>();letter.RestoreProgress(true,15,false,false);
        var presentation=One<MainPresentation>();
        var controller=One<CheckpointController>();controller.UseTestStore(TestPath);
        Assert.That(presentation.BeginLetterAssembly(),Is.True);
        var pieces=Object.FindObjectsByType<LetterPuzzlePiece>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var piece in pieces)if(piece.slotIndex==0||piece.slotIndex==2){piece.transform.position=presentation.Puzzle.Slot(piece.slotIndex).position;piece.rectTransform.localRotation=Quaternion.identity;Assert.That(presentation.Puzzle.TrySnap(piece),Is.True);}
        presentation.Puzzle.Close();One<MapPickup>().Interact();
        var mateo=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).Single(s=>s.name.Contains("Mateo"));mateo.RestoreProgress(0,true);
        var zone=Object.FindObjectsByType<CharacterNarrationZoneScript>(FindObjectsSortMode.None).First();
        Assert.That((bool)zone,Is.True,"Narration zone must be alive");
        string zoneName=zone.name;
        zone.RestoreTriggered(true);
        yield return null;yield return null;
        Assert.That(controller.LastSaved,Is.Not.Null,"Closing a partially assembled puzzle must request an autosave.");Assert.That(new CheckpointStore(TestPath).TryRead(out var data),Is.True);Assert.That(data.puzzleMask,Is.EqualTo(5));
        var previousId=controller.GetInstanceID();controller.RestartCheckpoint();float until=Time.unscaledTime+15;
        while(One<CheckpointController>()==null||One<CheckpointController>().GetInstanceID()==previousId){Assert.That(Time.unscaledTime,Is.LessThan(until));yield return null;}
        yield return null;yield return null;yield return null;
        Assert.That(new CheckpointStore(TestPath).TryRead(out data),Is.True);
        Assert.That(One<EquipmentManager>().EquippedCount,Is.EqualTo(3));Assert.That(One<LetterManager>().CollectedCount,Is.EqualTo(4));Assert.That(One<LetterManager>().MissionCompleted,Is.False);
        Assert.That(One<MainPresentation>().HasMap,Is.True);Assert.That(One<MainPresentation>().Puzzle.PlacedCount,Is.EqualTo(2));Assert.That(One<MainPresentation>().Puzzle.PlacedMask,Is.EqualTo(5));
        bool restoredNarration=false;foreach(var candidate in Object.FindObjectsByType<CharacterNarrationZoneScript>(FindObjectsSortMode.None))if(candidate.name==zoneName)restoredNarration=candidate.HasTriggered;Assert.That(restoredNarration,Is.True);
        Assert.That(One<ObjectiveHud>().CurrentObjective,Is.EqualTo("Devolvé la carta a Mateo"));Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));Assert.That(One<PlayerControllerScript>().enabled,Is.True);
        Assert.That(One<MainPresentation>().BeginLetterAssembly(),Is.True);yield return null;Assert.That(One<MainPresentation>().Puzzle.PlacedCount,Is.EqualTo(2));One<MainPresentation>().Puzzle.Close();
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator ModalOwnershipRejectsOverlapAndRestoresOriginalControlState()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
        var session=One<UiSessionCoordinator>();var presentation=One<MainPresentation>();var pause=One<PauseMenu>();var inspection=One<InspectionScript>();var dialogue=One<DialogueManagerScript>();var player=One<PlayerControllerScript>();
        One<MapPickup>().Interact();presentation.Map.SetOpen(true,true);yield return null;Assert.That(session.Mode,Is.EqualTo(UiMode.Map));Assert.That(pause.Open(),Is.False);
        var doc=Object.FindObjectsByType<InspectableDocument>(FindObjectsSortMode.None).First(d=>d.name.StartsWith("Diario1"));inspection.StartInspect(doc.gameObject);Assert.That(inspection.IsInspecting,Is.False);
        presentation.Map.SetOpen(false,true);player.enabled=false;var speaker=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).First(s=>s.name.Contains("Diego"));speaker.Speak();yield return null;
        Assert.That(session.Mode,Is.EqualTo(UiMode.Dialogue));Assert.That(pause.Open(),Is.False);dialogue.CancelConversation();yield return null;Assert.That(player.enabled,Is.False,"A view must not enable a control that was disabled before opening it.");player.enabled=true;
        inspection.StartInspect(doc.gameObject);Assert.That(session.Mode,Is.EqualTo(UiMode.Inspection));Assert.That(pause.Open(),Is.False);Assert.That(player.enabled,Is.False);inspection.StopInspect();Assert.That(player.enabled,Is.True);
        One<LetterManager>().RestoreProgress(true,15,false,false);Assert.That(presentation.BeginLetterAssembly(),Is.True);Assert.That(session.Mode,Is.EqualTo(UiMode.Puzzle));Assert.That(pause.Open(),Is.False);presentation.Puzzle.Close();Assert.That(player.enabled,Is.True);
        Assert.That(pause.Open(),Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(player.enabled,Is.False);presentation.Map.SetOpen(true,true);Assert.That(presentation.Map.IsOpen,Is.False);
        pause.Close();Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(AudioListener.pause,Is.False);Assert.That(player.enabled,Is.True);Assert.That(session.Mode,Is.EqualTo(UiMode.None));
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator RouteStagesUseZonesAndRestartReloadsLastSafeCheckpoint()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
        var progress=One<LevelProgress>();Assert.That(progress.Reach(RouteStage.Front),Is.False);
        One<EquipmentManager>().RestoreProgress(true,7);var mateo=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).Single(s=>s.name.Contains("Mateo"));mateo.RestoreProgress(0,true);
        Assert.That(progress.Reach(RouteStage.Outpost),Is.True);Assert.That(progress.Reach(RouteStage.Camp),Is.False);yield return null;Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Andá al frente"));
        var controller=One<CheckpointController>();controller.UseTestStore(TestPath);Assert.That(controller.SaveNow(),Is.True);
        var initial=controller.GetInstanceID();One<PauseMenu>().Open();controller.RestartCheckpoint();float until=Time.unscaledTime+15;
        while(One<CheckpointController>()==null||One<CheckpointController>().GetInstanceID()==initial){Assert.That(Time.unscaledTime,Is.LessThan(until));yield return null;}
        yield return null;yield return null;yield return null;
        Assert.That(One<LevelProgress>().Stage,Is.EqualTo(RouteStage.Outpost));Assert.That(One<EquipmentManager>().MissionCompleted,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));Assert.That(One<PlayerControllerScript>().enabled,Is.True);
        var saved=One<CheckpointController>();saved.UseTestStore(TestPath);GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;Assert.That(One<LevelProgress>().Stage,Is.EqualTo(RouteStage.Battle));Assert.That(saved.SaveNow(),Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator PauseOptionsScaleTextAndPerformanceSamplingExcludesPause()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;yield return Wait(.25f);
        var monitor=One<RuntimePerformanceMonitor>();Assert.That(monitor.Capture().samples,Is.GreaterThan(0));var pause=One<PauseMenu>();Assert.That(pause.Open(),Is.True);var count=monitor.Capture().samples;yield return Wait(.1f);Assert.That(monitor.Capture().samples,Is.EqualTo(count));
        var slider=Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Single(s=>s.name=="Option_1");slider.value=1.25f;Assert.That(ComfortSettings.Current.textSize,Is.EqualTo(1.25f));
        var title=Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="ObjectiveTitle");
        yield return null;yield return null;
        var textAudit=new System.Text.StringBuilder();foreach(var text in pause.GetComponentsInChildren<TMP_Text>(true))textAudit.AppendLine(text.name+" "+text.fontSize);
        foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))if(text.transform.IsChildOf(One<CanvasSafeArea>().transform)&&text.name.StartsWith("OptionLabel_")){text.ForceMeshUpdate();Assert.That(text.fontSize,Is.GreaterThan(20));Assert.That(text.textInfo.characterCount,Is.GreaterThan(0));textAudit.AppendLine($"{text.name}: size={text.fontSize} chars={text.textInfo.characterCount} color={text.color} culled={text.canvasRenderer.cull}");}
        File.WriteAllText("Library/Programming-pause-text-audit.txt",textAudit.ToString());
        yield return MainIntegrationTests.Capture("Library/Programming-pause-1280x720.png",1280,720);yield return MainIntegrationTests.Capture("Library/Programming-pause-1280x1024.png",1280,1024);
        pause.Close();yield return null;yield return null;Assert.That(title.fontSize,Is.EqualTo(27.5f).Within(.01));ComfortSettings.Apply(new ComfortValues(),false);yield return null;Assert.That(monitor.Capture().samples,Is.GreaterThan(count));
        File.WriteAllText("Library/Programming-performance-editor.json",JsonUtility.ToJson(monitor.Capture(),true));yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator SafeSpawnsAndCriticalEntrancesHaveClearPlayerCapsules()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Physics.SyncTransforms();
        var controller=One<CheckpointController>();
        Assert.That((bool)controller,Is.True,"Checkpoint controller must be alive");
        var spawns=controller.SafeSpawns;
        Assert.That(spawns,Is.Not.Null,"Safe spawn array");
        var player=One<PlayerControllerScript>();
        Assert.That((bool)player,Is.True,"Player must be alive");
        var cc=player.GetComponent<CharacterController>();
        var report=new System.Text.StringBuilder();
        for(int i=0;i<spawns.Length;i++)
        {
            var spawn=spawns[i];Assert.That(spawn,Is.Not.Null,"Safe spawn "+i);Vector3 center=spawn.position+cc.center;float height=cc.height*.5f-cc.radius;
            var overlaps=Physics.OverlapCapsule(center+Vector3.up*height,center-Vector3.up*height,cc.radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            var blockers=new System.Collections.Generic.List<Collider>();foreach(var hit in overlaps)if(!hit.transform.IsChildOf(cc.transform))blockers.Add(hit);
            report.AppendLine(spawn.name+": "+string.Join(",",blockers.Select(c=>c.name)));Assert.That(blockers,Is.Empty,report.ToString());
        }
        File.WriteAllText("Library/Programming-collision-audit.txt",report.ToString());yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator EscapeClosesMapBeforePauseAndClosingMenuDoesNotCarryClickIntoGunfire()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
        var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;var oldBackground=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var presentation=One<MainPresentation>();var pause=One<PauseMenu>();One<MapPickup>().Interact();presentation.Map.SetOpen(true,true);
            yield return Escape(keyboard);Assert.That(presentation.Map.IsOpen,Is.False);Assert.That(pause.IsOpen,Is.False);
            yield return Escape(keyboard);Assert.That(pause.IsOpen,Is.True);yield return Escape(keyboard);Assert.That(pause.IsOpen,Is.False);
            var weapon=One<PlayerWeaponController>();weapon.EquipWeapon("FAL");yield return null;yield return null;int rounds=weapon.RoundsInMagazine;Cursor.lockState=CursorLockMode.Locked;
            Assert.That(pause.Open(),Is.True);InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return null;pause.Close();
            var input=One<PlayerWeaponInput>();Assert.That(input.TryFireFromInput(true,true),Is.False);Assert.That(weapon.RoundsInMagazine,Is.EqualTo(rounds));
            InputSystem.QueueStateEvent(mouse,new MouseState());Assert.That(input.TryFireFromInput(false,true),Is.False);yield return null;yield return null;Assert.That(input.TryFireFromInput(true,true),Is.True);Assert.That(weapon.RoundsInMagazine,Is.EqualTo(rounds-1));
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;InputSystem.settings.backgroundBehavior=oldBackground;}
        yield return new ExitPlayMode();
    }
    static IEnumerator Escape(Keyboard keyboard)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        Time.timeScale=1;AudioListener.pause=false;ComfortSettings.Apply(new ComfortValues(),false);
        if(Application.isPlaying)yield return new ExitPlayMode();
        foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(TestPath+suffix))File.Delete(TestPath+suffix);
    }
}
#endif
