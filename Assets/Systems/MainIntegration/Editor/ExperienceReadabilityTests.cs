#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using InteractionSystem.Scripts.Interactables.Tests;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InteractionSystem.Scripts.Utils;
using Systems.Missions.FindObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class ExperienceReadabilityTests
{
    static bool displayDone;
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    static IEnumerator Display(UIDialogueScript ui,DialogueSO line){yield return ui.DisplayDialogue(line,true,true);displayDone=true;}
    [UnityTest]public IEnumerator ManualCompletionShowsChoicesWithoutTheAutomaticReadingDelay()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var ui=One<UIDialogueScript>();var line=ScriptableObject.CreateInstance<DialogueSO>();var data=new SerializedObject(line);data.FindProperty("_dialogueText").stringValue="¿Me ayudás a buscar los fragmentos de la carta?";data.FindProperty("_extraReadTime").floatValue=12;data.ApplyModifiedProperties();displayDone=false;ui.StartCoroutine(Display(ui,line));yield return Wait(.22f);Assert.That(ui.IsTyping,Is.True);ui.RequestAdvance();yield return null;yield return null;
        Assert.That(displayDone,Is.True,"Manual completion must reveal choices immediately instead of waiting twelve seconds");Assert.That(ui.IsWaitingForAdvance,Is.False);Object.Destroy(line);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator FocusRespectsWallsAndModalUiAndPickupsTakePriority()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        One<PlayerControllerScript>().enabled=false;var camera=Camera.main;var interaction=One<InteractionScript>();var config=new SerializedObject(interaction);int mask=config.FindProperty("interactionLayer").intValue;int layer=0;while((mask&(1<<layer))==0&&layer<31)layer++;
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Readable object test";go.layer=layer;go.transform.position=camera.transform.position+camera.transform.forward*1.4f;go.transform.localScale=Vector3.one*.2f;var target=go.AddComponent<CubeInteractableScript>();target.UIInteraction=new UIInteraction{label="Examinar",characterDescription=""};Physics.SyncTransforms();yield return null;yield return null;
        var focus=One<InteractionFocusHighlight>();Assert.That(focus.HighlightedTarget,Is.EqualTo(target));Assert.That(focus.OverlayCount,Is.EqualTo(1));
        yield return MainIntegrationTests.Capture("Library/ExperienceReadability-focus.png",1280,720);
        CaptureWorld(camera,"Library/ExperienceReadability-focus-world.png");
        var session=One<UiSessionCoordinator>();Assert.That(session.TryAcquire(UiMode.Map,go),Is.True);yield return null;yield return null;Assert.That(focus.HighlightedTarget,Is.Null);Assert.That(focus.OverlayCount,Is.Zero);session.Release(go);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=camera.transform.position+camera.transform.forward*.7f;wall.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();yield return null;yield return null;Assert.That(interaction.CurrentTarget,Is.Null);Assert.That(focus.OverlayCount,Is.Zero);Object.Destroy(wall);Object.Destroy(go);
        var feedback=One<MissionAnnouncement>();feedback.ShowMessage("Mensaje anterior");var letter=One<LetterManager>();letter.StartMission();yield return null;yield return null;
        var fragments=Object.FindObjectsByType<LetterInteractableScript>(FindObjectsSortMode.None).Where(f=>f.collectAfterInspection).Take(2).ToArray();Assert.That(fragments.Length,Is.EqualTo(2));fragments[0].OnInspectionEnded();yield return null;yield return null;fragments[1].OnInspectionEnded();yield return null;yield return null;
        Assert.That(letter.CollectedCount,Is.EqualTo(2));Assert.That(feedback.CurrentMessage,Is.EqualTo("Recogiste un fragmento · 2/4"));yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator FrontAmbienceIncreasesWithoutAdvancingMissions()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");Assert.That(ShaderUtil.ShaderHasError(Shader.Find("StoryTell/InteractionFocus")),Is.False);yield return new EnterPlayMode();yield return null;
        var ambience=One<FrontApproachAmbience>();ambience.enabled=false;var stage=One<LevelProgress>().Stage;ambience.ApplyForPosition(280);Assert.That(ambience.Intensity,Is.Zero);float quietInterval=ambience.ExplosionInterval;ambience.ApplyForPosition(430);Assert.That(ambience.Intensity,Is.InRange(.1f,.9f));ambience.ApplyForPosition(525);Assert.That(ambience.Intensity,Is.EqualTo(1));Assert.That(ambience.ExplosionInterval,Is.LessThan(quietInterval));Assert.That(One<LevelProgress>().Stage,Is.EqualTo(stage));
        var data=new SerializedObject(ambience);var smoke=data.FindProperty("smoke");Assert.That(smoke.arraySize,Is.EqualTo(3));for(int i=0;i<smoke.arraySize;i++){var ps=(ParticleSystem)smoke.GetArrayElementAtIndex(i).objectReferenceValue;Assert.That(ps.main.maxParticles,Is.EqualTo(36));Assert.That(ps.emission.rateOverTime.constant,Is.EqualTo(5));ps.Simulate(4,true,true);}
        Assert.That(Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Count(c=>c.name.StartsWith("Collapsed shelter")),Is.EqualTo(2));
        var terrain=One<Terrain>();var go=new GameObject("Approach preview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(72,terrain.SampleHeight(new Vector3(72,0,402))+terrain.transform.position.y+1.7f,402);camera.transform.LookAt(new Vector3(60,terrain.SampleHeight(new Vector3(60,0,416))+terrain.transform.position.y+1.5f,416));camera.fieldOfView=60;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes("Library/ExperienceReadability-front.png",pixels.EncodeToPNG());Object.Destroy(pixels);}
        finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
    static void CaptureWorld(Camera camera,string path)
    {
        var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}
        finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);}
    }
}
#endif
