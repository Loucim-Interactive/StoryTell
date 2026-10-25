#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.GameplayPath;
using StoryTell.PhysicalMap;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class GameplayPathUITests
{
    const string ScenePath = "Assets/Scenes/Level1.unity";
    [Test] public void SavedCanvas_UsesConsistentFontsAndValidSubtitleBounds()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        foreach(var text in All<TMP_Text>(scene))
        {
            Assert.That(text.font.name,Does.StartWith("InterTight-"),text.name);
            if(text.name=="NameText"||text.name=="NarrationText")Assert.That(text.margin,Is.EqualTo(Vector4.zero),"Legacy margins must not overlap the speaker and subtitle.");
            if(text is TextMeshProUGUI && text.name!="Transcription")
            { Assert.That(text.rectTransform.rect.width,Is.GreaterThan(0),text.name);Assert.That(text.rectTransform.rect.height,Is.GreaterThan(0),text.name); }
        }
        foreach(var scaler in All<CanvasScaler>(scene))
        {Assert.That(scaler.referenceResolution,Is.EqualTo(new Vector2(1920,1080)));Assert.That(scaler.screenMatchMode,Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));}
        Assert.That(All<DocumentInspectionPanel>(scene).Length,Is.EqualTo(1));
        foreach(var letter in All<GameplayPathInteractable>(scene).Where(i=>i.Action==PathAction.Read))
        {Assert.That(letter.InspectInteractable,Is.True);Assert.That(letter.GetComponent<InspectableDocument>().transcription,Is.Not.Empty);}
        var diary=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/Diario1.prefab");Assert.That(diary.GetComponent<InspectableDocument>(),Is.Not.Null);
    }

    [UnityTest] public IEnumerator Inspection_KeepsObjectAndTranscriptVisible_RestoresControlsAndTransforms()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var controller=Object.FindAnyObjectByType<GameplayPathController>();
        controller.Progress.Advance(PathAction.Ruben);controller.Interact(PathAction.Map);
        var map=Object.FindAnyObjectByType<PhysicalMapController>();map.SetOpen(true,true);
        var inspection=controller.Player.GetComponent<InspectionScript>();
        var panel=Object.FindAnyObjectByType<DocumentInspectionPanel>();
        var diary=Object.FindObjectsByType<InspectableDocument>(FindObjectsSortMode.None).First(d=>d.name=="Diario1");
        var parent=diary.transform.parent;var position=diary.transform.localPosition;var rotation=diary.transform.localRotation;var scale=diary.transform.localScale;
        var interactable=diary.GetComponent<InteractableScript>();
        interactable.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,diary.gameObject);yield return null;
        Assert.That(inspection.IsInspecting,Is.True);Assert.That(panel.IsVisible,Is.True);Assert.That(panel.Body.text,Is.EqualTo(diary.transcription));
        Assert.That(controller.IsBusy,Is.True);Assert.That(map.IsOpen,Is.False);Assert.That(map.enabled,Is.False);
        Assert.That(controller.Player.GetComponent<InteractionScript>().enabled,Is.False);
        Assert.That(inspection.inspectAction.action.enabled,Is.True,"Closing inspection must stay available while interaction is suspended.");
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1280,1024)})
            Capture("Library/GameplayPathUI-diary-"+size.x+"x"+size.y+".png",size.x,size.y,panel);
        float closeReady=Time.unscaledTime+0.3f;
        while(Time.unscaledTime<closeReady)yield return null;
        var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldBackground=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
            Assert.That(diary.transform.parent,Is.EqualTo(parent));Assert.That(Vector3.Distance(diary.transform.localPosition,position),Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(diary.transform.localRotation,rotation),Is.LessThan(0.01f));Assert.That(diary.transform.localScale,Is.EqualTo(scale));
            yield return null;
            Assert.That(inspection.IsInspecting,Is.False,"E must leave the inspected object without reopening it.");
        }
        finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;InputSystem.settings.backgroundBehavior=oldBackground; }
        Assert.That(panel.IsVisible,Is.False);Assert.That(inspection.IsInspecting,Is.False);Assert.That(controller.IsBusy,Is.False);
        Assert.That(interactable.GetComponent<Collider>().enabled,Is.True);Assert.That(controller.Player.GetComponent<InteractionScript>().enabled,Is.True);Assert.That(map.enabled,Is.True);
        var optional=Object.FindObjectsByType<GameplayPathInteractable>(FindObjectsSortMode.None).First(d=>d.Action==PathAction.Read);
        optional.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,optional.gameObject);yield return null;
        Assert.That(panel.IsVisible,Is.True);Assert.That(panel.Body.text,Is.EqualTo(optional.GetComponent<InspectableDocument>().transcription));
        Capture("Library/GameplayPathUI-letter.png",1280,720,panel);inspection.StopInspect();
        foreach(var fragment in Object.FindObjectsByType<GameplayPathInteractable>(FindObjectsSortMode.None).Where(i=>i.Action==PathAction.Fragment))
        {
            fragment.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,fragment.gameObject);yield return null;
            Assert.That(fragment.gameObject.activeSelf,Is.True,"A collected fragment remains visible until inspection ends.");
            Assert.That(panel.IsVisible,Is.True);inspection.StopInspect();yield return null;Assert.That(fragment.gameObject.activeSelf,Is.False);
        }
        Assert.That(controller.Progress.Fragments,Is.EqualTo(4));controller.InspectMotherLetter();yield return null;
        Assert.That(panel.IsVisible,Is.True);Assert.That(panel.Body.text,Is.EqualTo(controller.Definition.motherLetter));
        Capture("Library/GameplayPathUI-mother-letter.png",1280,720,panel);
        var assembled=inspection.CurrentTarget;inspection.StopInspect();yield return null;Assert.That(assembled.activeSelf,Is.False);
        var narration=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Single(t=>t.name=="NarrationText");
        var name=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Single(t=>t.name=="NameText");
        narration.text="¿Recién llegás? Soy Rubén. Antes de moverte por el campamento, revisá el equipo. El mapa te va a servir para ubicarte.";narration.maxVisibleCharacters=int.MaxValue;name.text="Rubén";yield return null;
        Capture("Library/GameplayPathUI-subtitles.png",1280,720,null);
        narration.text="";name.text="";map.SetOpen(true,true);yield return null;Capture("Library/GameplayPathUI-map.png",1280,720,null);
        yield return new ExitPlayMode();
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static void Capture(string path,int width,int height,DocumentInspectionPanel document)
    {
        var camera=Camera.main;var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=canvases.Select(c=>c.worldCamera).ToArray();var oldTarget=camera.targetTexture;var previous=RenderTexture.active;
        var rt=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            rt.Create();camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.3f;}
            Canvas.ForceUpdateCanvases();
            if(document!=null)
            {
                var panel=(GameObject)new SerializedObject(document).FindProperty("panel").objectReferenceValue;
                var corners=new Vector3[4];((RectTransform)panel.transform).GetWorldCorners(corners);
                foreach(var corner in corners){var p=camera.WorldToViewportPoint(corner);Assert.That(p.x,Is.InRange(0f,1f));Assert.That(p.y,Is.InRange(0f,1f));}
                Assert.That(document.Body.rectTransform.rect.height,Is.GreaterThanOrEqualTo(document.Body.preferredHeight-1));
            }
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
            RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget;RenderTexture.active=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=cameras[i];}
            rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
    }
}
#endif
