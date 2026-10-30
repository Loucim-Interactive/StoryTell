#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.MainIntegration.Editor;
using Systems.DecisionSystem.UI;
using Systems.DialogueSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.WalkieSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class DialoguePolishTests
{
    const string Scene="Assets/Scenes/Level1.unity";
    static T[] All<T>()where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    static ConversationSO Conversation(string text)
    {
        var line=ScriptableObject.CreateInstance<DialogueSO>();var data=new SerializedObject(line);data.FindProperty("_dialogueText").stringValue=text;data.FindProperty("_speaker").enumValueIndex=(int)ESpeakers.Mateo;data.FindProperty("_extraReadTime").floatValue=.1f;data.ApplyModifiedProperties();var c=ScriptableObject.CreateInstance<ConversationSO>();c.Dialogues=new[]{line};return c;
    }
    [Test]public void ResponsiveLayoutUsesPositiveTextRectsAndConsistentFonts()
    {
        EditorSceneManager.OpenScene(Scene);var canvas=All<Canvas>().Single();var scaler=canvas.GetComponent<CanvasScaler>();Assert.That(scaler.uiScaleMode,Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));Assert.That(scaler.referenceResolution,Is.EqualTo(new Vector2(1920,1080)));Assert.That(All<CanvasSafeArea>().Length,Is.EqualTo(1));
        Canvas.ForceUpdateCanvases();var ui=All<UIDialogueScript>().Single();Assert.That(ui.Body.rectTransform.rect.height,Is.GreaterThan(80));Assert.That(ui.Body.overflowMode,Is.EqualTo(TextOverflowModes.Page));
        foreach(var t in All<TMP_Text>())Assert.That(t.font.name,Does.StartWith("InterTight"),t.name);
        var name=All<TMP_Text>().Single(t=>t.name=="NameText");Assert.That(name.rectTransform.rect.height,Is.GreaterThan(20));
        var placements=JsonUtility.FromJson<DialoguePolishAuthoring.Placements>(File.ReadAllText(DialoguePolishAuthoring.Data+"/NpcPlacements.json"));Assert.That(placements.actors.Length,Is.EqualTo(9));foreach(var actor in placements.actors){Assert.That(actor.after.x,Is.EqualTo(actor.before.x).Within(.001));Assert.That(actor.after.z,Is.EqualTo(actor.before.z).Within(.001));}
    }
    [UnityTest]public IEnumerator ManualDialogueRevealsPagesAndRestoresControlsOnCancel()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var manager=Object.FindAnyObjectByType<DialogueManagerScript>();var ui=manager.uiDialogueScript;var player=Object.FindAnyObjectByType<PlayerControllerScript>();var speaker=All<SpeakerScript>().First(s=>s.name.Contains("Mateo"));
        var conversation=Conversation(string.Join(" ",Enumerable.Repeat("<emphasis>La carta</emphasis> está cerca del refugio. Preguntá a tus compañeros antes de seguir hacia el frente.",12)));speaker.SetConversation(conversation);speaker.Speak();yield return Wait(.25f);
        Assert.That(manager.IsInConversation,Is.True);Assert.That(player.enabled,Is.False);Assert.That(ui.IsTyping,Is.True);Assert.That(ui.PageCount,Is.GreaterThan(1));
        yield return Wait(.2f);ui.RequestAdvance();yield return null;yield return null;Assert.That(ui.IsWaitingForAdvance,Is.True);Assert.That(ui.Page,Is.EqualTo(1));
        yield return MainIntegrationTests.Capture("Library/DialoguePolish-1280x720.png",1280,720);yield return MainIntegrationTests.Capture("Library/DialoguePolish-1280x1024.png",1280,1024);
        ui.RequestAdvance();yield return null;yield return null;Assert.That(ui.Page,Is.EqualTo(2));Assert.That(manager.IsInConversation,Is.True);
        manager.CancelConversation(speaker);yield return null;yield return null;Assert.That(ui.Body.text,Is.Empty);Assert.That(player.enabled,Is.True);Assert.That(manager.LastConversationCompleted,Is.False);
        speaker.Speak();yield return null;Assert.That(manager.IsInConversation,Is.True,"Cancellation must not consume the NPC's conversation.");manager.CancelConversation(speaker);yield return null;
        Object.Destroy(conversation.Dialogues[0]);Object.Destroy(conversation);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator NativeEquipmentChoicesStartMissionAndObjectiveUpdates()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var objective=Object.FindAnyObjectByType<ObjectiveHud>();Assert.That(objective.CurrentObjective,Does.Contain("Diego"));
        var speaker=All<SpeakerScript>().Single(s=>s.name.Contains("Diego"));speaker.Speak();var manager=Object.FindAnyObjectByType<DialogueManagerScript>();var decision=Object.FindAnyObjectByType<DecisionManagerScript>();float until=Time.unscaledTime+15;
        while(!decision.HasRequest&&Time.unscaledTime<until){manager.uiDialogueScript.RequestAdvance();yield return Wait(.22f);}
        Assert.That(decision.HasRequest,Is.True,"Native Diego dialogue must still reach its choices.");yield return Wait(.25f);
        yield return MainIntegrationTests.Capture("Library/DialoguePolish-choices.png",1280,720);File.WriteAllText("Library/DialoguePolish-decision-before.txt",JsonUtility.ToJson(decision,true)+"\nchoosing="+typeof(DecisionManagerScript).GetField("_isChoosing",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(decision)+" ready="+typeof(DecisionManagerScript).GetField("_submitReadyAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(decision)+" time="+Time.unscaledTime+"\n"+string.Join("\n",decision.Options??new string[0]));decision.Submit(0);Assert.That(decision.HasRequest,Is.False,"Submit must consume the displayed response request.");
        until=Time.unscaledTime+10;while(manager.IsInConversation&&Time.unscaledTime<until){manager.uiDialogueScript.RequestAdvance();yield return Wait(.22f);}
        Assert.That(Object.FindAnyObjectByType<EquipmentManager>().MissionStarted,Is.True);Assert.That(manager.IsInConversation,Is.False);yield return null;Assert.That(objective.CurrentObjective,Does.Contain("0/3"));
        yield return MainIntegrationTests.Capture("Library/DialoguePolish-objective.png",1280,720);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator MouseChoicesSubmitOnceAndLongRowsHaveRoom()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var decision=Object.FindAnyObjectByType<DecisionManagerScript>();var owner=new GameObject("Choice test owner");Assert.That(decision.TryAcquire(owner),Is.True);var probe=new ChoiceProbe();
        string longLabel=string.Join(" ",Enumerable.Repeat("Puedo revisar el refugio y preguntarle al soldado por los fragmentos de la carta.",3));
        Assert.That(decision.Present(owner,new[]{longLabel,"Prefiero continuar hacia el puesto avanzado."},probe.Select),Is.True);yield return Wait(.3f);Canvas.ForceUpdateCanvases();
        var buttons=All<WalkieDecisionButton>().Where(b=>b.gameObject.activeInHierarchy).OrderByDescending(b=>b.GetComponent<LayoutElement>().preferredHeight).ToArray();Assert.That(buttons.Length,Is.EqualTo(2));Assert.That(buttons[0].GetComponent<LayoutElement>().preferredHeight,Is.GreaterThan(75));
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(buttons[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);ExecuteEvents.Execute(buttons[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.That(probe.index,Is.EqualTo(0));Assert.That(probe.count,Is.EqualTo(1));decision.Release(owner);Object.Destroy(owner);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator NpcFeetRestOnTerrainAfterAnimationAndTalking()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return Wait(.6f);var terrain=Object.FindAnyObjectByType<Terrain>();
        var report=new System.Text.StringBuilder();float worst=0;
        foreach(var actor in All<SpeakerScript>())
        {
            var primary=actor.GetComponentsInChildren<Animator>(true).Single(a=>a.enabled&&a.runtimeAnimatorController);Assert.That(primary.applyRootMotion,Is.False);Assert.That(primary.GetComponent<NpcFootPlacement>(),Is.Not.Null);
            var left=primary.GetBoneTransform(HumanBodyBones.LeftFoot).position-Vector3.up*primary.leftFeetBottomHeight;var right=primary.GetBoneTransform(HumanBodyBones.RightFoot).position-Vector3.up*primary.rightFeetBottomHeight;
            float gap=Mathf.Max(Mathf.Abs(left.y-terrain.SampleHeight(left)-terrain.transform.position.y),Mathf.Abs(right.y-terrain.SampleHeight(right)-terrain.transform.position.y));worst=Mathf.Max(worst,gap);report.AppendLine($"{actor.name} root={actor.transform.position} model={primary.transform.localPosition} leftSole={left} rightSole={right} gap={gap}");
        }
        File.WriteAllText("Library/DialoguePolish-feet.txt",report.ToString());Assert.That(worst,Is.LessThan(.15f),report.ToString());
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    sealed class ChoiceProbe{public int index=-1,count;public void Select(int value){index=value;count++;}}
}
#endif
