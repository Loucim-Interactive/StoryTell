#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.DecisionSystem.UI;
using Systems.DialogueSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.Missions;
using Systems.Missions.FindObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class GuidanceTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    static IEnumerator Talk(SpeakerScript speaker)
    {
        speaker.Speak();yield return Wait(.25f);var manager=One<DialogueManagerScript>();var decisions=One<DecisionManagerScript>();float until=Time.unscaledTime+25;
        while(manager.IsInConversation&&Time.unscaledTime<until)
        {
            if(decisions.HasRequest){yield return Wait(.25f);decisions.Submit(0);}
            else manager.uiDialogueScript.RequestAdvance();
            yield return Wait(.22f);
        }
        Assert.That(manager.IsInConversation,Is.False,"The native conversation must finish without losing its selected action.");yield return null;
    }
    [UnityTest]public IEnumerator NativeMissionRouteUpdatesChecklistHintsGateAndSimpleDestinations()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var hud=One<ObjectiveHud>();var route=One<RouteBoundary>();var eq=One<EquipmentManager>();var letter=One<LetterManager>();
        Assert.That(hud.CurrentObjective,Does.Contain("Hablá con Diego"));Assert.That(route.CanProceed,Is.False);Assert.That(hud.DestinationZone,Is.Zero);
        var speakers=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None);
        yield return Talk(speakers.Single(s=>s.name.Contains("Diego")));Assert.That(eq.MissionStarted,Is.True);
        var equipment=(EquipmentInteractableScript[])new SerializedObject(eq).FindProperty("equipment").GetValueArray();
        foreach(var item in equipment){item.OnInspectionEnded();yield return null;}
        Assert.That(eq.MissionCompleted,Is.True);Assert.That(route.CanProceed,Is.True);yield return null;
        Assert.That(hud.CurrentObjective,Does.Contain("Hablá con Mateo"));Assert.That(hud.DestinationZone,Is.EqualTo(1));
        yield return Talk(speakers.Single(s=>s.name.Contains("Mateo")));Assert.That(letter.MissionStarted,Is.True);Assert.That(route.CanProceed,Is.False);yield return null;
        Assert.That(hud.CurrentObjective,Does.Contain("0/4"));Assert.That(hud.DestinationZone,Is.EqualTo(1));
        var fragments=Object.FindObjectsByType<LetterInteractableScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.name.StartsWith("MateoLetterFragment")).OrderBy(f=>f.name).ToArray();
        var hints=Object.FindObjectsByType<LetterHintProvider>(FindObjectsSortMode.None);Assert.That(hints.Length,Is.GreaterThanOrEqualTo(2));var previous=hints.Select(h=>h.GetHint()).ToArray();Assert.That(previous.All(h=>h),Is.True);
        for(int i=0;i<fragments.Length;i++)
        {
            fragments[i].OnInspectionEnded();yield return null;yield return null;
            Assert.That(letter.CollectedCount,Is.EqualTo(i+1));
            if(i<3){Assert.That(hud.CurrentObjective,Does.Contain($"{i+1}/4"));for(int j=0;j<hints.Length;j++){var next=hints[j].GetHint();Assert.That(next,Is.Not.Null);Assert.That(next,Is.Not.EqualTo(previous[j]));previous[j]=next;}}
        }
        Assert.That(hud.CurrentObjective,Is.EqualTo("Devolvé la carta a Mateo"));Assert.That(route.CanProceed,Is.False);
        yield return Talk(speakers.Single(s=>s.name.Contains("Mateo")));Assert.That(letter.MissionCompleted,Is.True);yield return null;Assert.That(route.CanProceed,Is.True);Assert.That(hud.CurrentObjective,Is.EqualTo("Andá al puesto avanzado"));
        Assert.That(hints.All(h=>h.GetHint()==null),Is.True);
        One<MapPickup>().Interact();One<MainPresentation>().Map.SetOpen(true,true);yield return null;yield return null;var overlay=One<MapZoneOverlay>();overlay.Refresh();Assert.That(overlay.ActiveDestination,Is.EqualTo(2));Assert.That(One<MissionsUIManager>().IsVisible,Is.False);
        yield return MainIntegrationTests.Capture("Library/Guidance-map.png",1280,720);
        One<MainPresentation>().Map.SetOpen(false,true);yield return null;yield return null;
        yield return MainIntegrationTests.Capture("Library/Guidance-objective.png",1280,720);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator FeedbackWaitsWhileHiddenAndNeverDisclosesFutureEvents()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var feedback=One<MissionAnnouncement>();var missions=One<MissionsUIManager>();missions.HideMissionsUI();feedback.ShowMessage("Fragmento recogido · 2/4");yield return Wait(4);
        missions.ShowMissionsUI();yield return null;yield return null;Assert.That(feedback.CurrentMessage,Is.EqualTo("Fragmento recogido · 2/4"));
        var hud=One<ObjectiveHud>();Assert.That(hud.CurrentObjective,Does.Not.Contain("ataque"));Assert.That(hud.CurrentObjective,Does.Not.Contain("explosión"));
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
static class GuidancePropertyArray
{
    public static EquipmentInteractableScript[] GetValueArray(this SerializedProperty p){var result=new EquipmentInteractableScript[p.arraySize];for(int i=0;i<result.Length;i++)result[i]=(EquipmentInteractableScript)p.GetArrayElementAtIndex(i).objectReferenceValue;return result;}
}
#endif
