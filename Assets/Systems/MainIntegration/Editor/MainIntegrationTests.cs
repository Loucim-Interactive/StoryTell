#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.MainIntegration.Editor;
using StoryTell.PhysicalMap;
using Systems.BattleSystem;
using Systems.DialogueSystem.Scripts;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.Missions;
using Systems.Missions.FindObjects;
using Systems.WeaponSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class MainIntegrationTests
{
    const string ScenePath="Assets/Scenes/Level1.unity";
    static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    [Test]public void MainUI_OriginalStylesAndAuthoredLocationsArePreserved()
    {
        EditorSceneManager.OpenScene(ScenePath);var baseline=JsonUtility.FromJson<MainIntegrationAuthoring.Baseline>(File.ReadAllText("Assets/Systems/MainIntegration/Data/MainUIBaseline.json"));
        foreach(var entry in baseline.ui)
        {
            Assert.That(GlobalObjectId.TryParse(entry.id,out var id),Is.True);var rect=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id)as RectTransform;Assert.That(rect,Is.Not.Null,entry.path);
            Assert.That(rect.anchorMin,Is.EqualTo(entry.anchorMin),entry.path);Assert.That(rect.anchorMax,Is.EqualTo(entry.anchorMax),entry.path);Assert.That(rect.pivot,Is.EqualTo(entry.pivot),entry.path);
            if(!rect.drivenByObject&&!rect.GetComponent<Canvas>()){Assert.That(Vector2.Distance(rect.sizeDelta,entry.size),Is.LessThan(0.02),entry.path);Assert.That(Vector2.Distance(rect.anchoredPosition,entry.position),Is.LessThan(0.02),entry.path);}
            var text=rect.GetComponent<TMP_Text>();if(text){Assert.That(AssetDatabase.GetAssetPath(text.font),Is.EqualTo(entry.font),entry.path);Assert.That(text.fontSize,Is.EqualTo(entry.fontSize).Within(0.001),entry.path);Assert.That(text.color,Is.EqualTo(entry.color),entry.path);}
        }
        foreach(var entry in baseline.positions)
        {
            // The polish request deliberately relocates the spawn, scouts and papers.
            if(Object.FindAnyObjectByType<RouteBoundary>())
            {
                GlobalObjectId.TryParse(entry.id,out var movedId);var moved=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(movedId)as Transform;
                if(moved&&(moved.GetComponentInParent<RouteBoundary>()||moved.GetComponentInParent<LetterHintProvider>()||moved.name.StartsWith("MateoLetterFragment")||moved.GetComponentInParent<Systems.InteractionSystem.Scripts.Interactables.Equipment.EquipmentInteractableScript>()))continue;
            }
            GlobalObjectId.TryParse(entry.id,out var id);var transform=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id)as Transform;Assert.That(transform,Is.Not.Null,entry.name);Assert.That(Vector3.Distance(transform.position,entry.position),Is.LessThan(0.001),entry.name);Assert.That(Quaternion.Angle(transform.rotation,entry.rotation),Is.LessThan(0.02),entry.name);Assert.That(Vector3.Distance(transform.localScale,entry.scale),Is.LessThan(0.001),entry.name);
        }
    }
    [Test]public void NativeRouteAndAllComponentsRemainConfigured()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);Assert.That(All<LetterManager>(scene).Length,Is.EqualTo(1));Assert.That(All<EquipmentManager>(scene).Length,Is.EqualTo(1));Assert.That(All<BattleManager>(scene).Length,Is.EqualTo(1));
        var letter=new SerializedObject(All<LetterManager>(scene).Single());Assert.That(letter.FindProperty("fragments").arraySize,Is.EqualTo(4));var equipment=new SerializedObject(All<EquipmentManager>(scene).Single());Assert.That(equipment.FindProperty("equipment").arraySize,Is.EqualTo(3));
        foreach(string name in new[]{"searchingConversation","returnConversation","completedConversation"})Assert.That(letter.FindProperty(name).objectReferenceValue,Is.Not.Null,name);
        Assert.That(new SerializedObject(All<BattleManager>(scene).Single()).FindProperty("spawnCount").intValue,Is.EqualTo(12));
        foreach(var transform in All<Transform>(scene))Assert.That(transform.GetComponents<Component>().All(c=>c),Is.True,transform.name);
        Assert.That(All<NpcConversationFacing>(scene).Length,Is.EqualTo(All<SpeakerScript>(scene).Length));
        Assert.That(LayerMask.LayerToName(9),Is.EqualTo("Equipment"));Assert.That(LayerMask.NameToLayer("MainPhysicalMap"),Is.GreaterThan(9));
    }
    [Test]public void DecorativeChest_DisabledOpeningNeedsNoLid()
    {
        var obj=new GameObject("DecorativeChestRegression");try{var chest=obj.AddComponent<Systems.InteractionSystem.Scripts.Interactables.Chests.SimpleChestInteractableScript>();chest.canOpen=false;Assert.DoesNotThrow(()=>typeof(Systems.InteractionSystem.Scripts.Interactables.Chests.SimpleChestInteractableScript).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(chest,null));}finally{Object.DestroyImmediate(obj);}
    }
    [UnityTest]public IEnumerator Documents_ShowPlainText_RotateAndToggle_CollectOnlyAfterInspection()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var reader=Object.FindAnyObjectByType<DocumentReader>();var inspection=Object.FindAnyObjectByType<InspectionScript>();var letter=Object.FindAnyObjectByType<LetterManager>();letter.StartMission();
        var fragments=Object.FindObjectsByType<LetterInteractableScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.name.StartsWith("MateoLetterFragment")).OrderBy(f=>f.name).ToArray();
        var fragment=fragments[0];var parent=fragment.transform.parent;var position=fragment.transform.localPosition;var scale=fragment.transform.localScale;var rotation=fragment.transform.localRotation;
        fragment.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,fragment.gameObject);yield return null;yield return null;
        Assert.That(inspection.IsInspecting,Is.True);Assert.That(reader.IsVisible,Is.True);Assert.That(reader.Content,Does.Contain("Querido Marcos"));Assert.That(letter.CollectedCount,Is.Zero);
        yield return Capture("Library/MainIntegration-document-1280x720.png",1280,720);yield return Capture("Library/MainIntegration-document-1280x1024.png",1280,1024);
        reader.ToggleText();Assert.That(reader.IsVisible,Is.False);Assert.That(inspection.IsInspecting,Is.True);reader.ToggleText();Assert.That(reader.IsVisible,Is.True);
        float closeReady=Time.unscaledTime+0.3f;while(Time.unscaledTime<closeReady)yield return null;
        var oldEditor=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;var oldBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
        UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        try
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
            typeof(UnityEngine.InputSystem.InputSystem).GetMethod("Update",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType)},null).Invoke(null,new object[]{UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic});
            Assert.That(inspection.IsInspecting,Is.False,"The dedicated E action must close reading even while world interaction is disabled.");
        }
        finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=oldBackground;}
        yield return null;
        Assert.That(letter.CollectedCount,Is.EqualTo(1));Assert.That(fragment.gameObject.activeSelf,Is.False);Assert.That(fragment.transform.parent,Is.EqualTo(parent));Assert.That(Vector3.Distance(fragment.transform.localPosition,position),Is.LessThan(0.001));Assert.That(fragment.transform.localScale,Is.EqualTo(scale));Assert.That(Quaternion.Angle(fragment.transform.localRotation,rotation),Is.LessThan(0.01));Assert.That(reader.IsVisible,Is.False);
        foreach(var next in fragments.Skip(1)){next.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,next.gameObject);yield return null;inspection.StopInspect();yield return null;}
        Assert.That(letter.CollectedCount,Is.EqualTo(4));Assert.That(letter.LetterAssembled,Is.False);letter.CompleteMission();Assert.That(letter.MissionCompleted,Is.True,"Main's direct return remains available.");
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator OptionalPuzzle_UsesActualPointerDragging_PreservesProgressAndControls()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var letter=Object.FindAnyObjectByType<LetterManager>();letter.StartMission();var inspection=Object.FindAnyObjectByType<InspectionScript>();
        foreach(var fragment in Object.FindObjectsByType<LetterInteractableScript>(FindObjectsSortMode.None)){if(!fragment.name.StartsWith("MateoLetterFragment"))continue;fragment.Interact();GameEventBus.Raise(GameplayEvents.StartInspection,fragment.gameObject);inspection.StopInspect();}
        var presentation=Object.FindAnyObjectByType<MainPresentation>();Assert.That(presentation.BeginLetterAssembly(),Is.True);yield return null;
        var puzzle=presentation.Puzzle;var pieces=Object.FindObjectsByType<LetterPuzzlePiece>(FindObjectsSortMode.None).OrderBy(p=>p.slotIndex).ToArray();
        pieces[0].transform.position=puzzle.Slot(0).position;pieces[0].rectTransform.localRotation=Quaternion.Euler(0,0,90);Assert.That(puzzle.TrySnap(pieces[0]),Is.False);pieces[0].rectTransform.localRotation=Quaternion.identity;Assert.That(puzzle.TrySnap(pieces[0]),Is.True);
        puzzle.Close();Assert.That(puzzle.PlacedCount,Is.EqualTo(1));Assert.That(presentation.BeginLetterAssembly(),Is.True);yield return null;
        pieces[1].rectTransform.localRotation=Quaternion.identity;Canvas.ForceUpdateCanvases();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,pieces[1].transform.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);bool reachable=false;foreach(var hit in hits)if(hit.gameObject==pieces[1].gameObject)reachable=true;Assert.That(reachable,Is.True);
        ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.beginDragHandler);pointer.position=RectTransformUtility.WorldToScreenPoint(null,puzzle.Slot(1).position);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.endDragHandler);Assert.That(pieces[1].Placed,Is.True);
        for(int i=2;i<4;i++){pieces[i].transform.position=puzzle.Slot(i).position;pieces[i].rectTransform.localRotation=Quaternion.identity;Assert.That(puzzle.TrySnap(pieces[i]),Is.True);}
        Assert.That(letter.LetterAssembled,Is.True);Assert.That(letter.MissionCompleted,Is.False);yield return Capture("Library/MainIntegration-puzzle.png",1280,720);puzzle.Close();Assert.That(inspection.GetComponent<PlayerControllerScript>().enabled,Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator Map_HidesNativeChecklistAndRifle_ThenRestoresBoth()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var presentation=Object.FindAnyObjectByType<MainPresentation>();var missions=Object.FindAnyObjectByType<MissionsUIManager>();Object.FindAnyObjectByType<EquipmentManager>().StartMission();
        Assert.That(presentation.Map.enabled,Is.False);Object.FindAnyObjectByType<MapPickup>().Interact();Assert.That(presentation.Map.enabled,Is.True);var weapon=Object.FindAnyObjectByType<PlayerWeaponController>();weapon.EquipWeapon("FAL");
        presentation.Map.SetOpen(true,true);yield return null;yield return null;Assert.That(missions.IsVisible,Is.False);Assert.That(weapon.CanShoot,Is.False);Assert.That(presentation.Map.IsOpen,Is.True);
        var playerMarker=presentation.Map.GetComponentsInChildren<MapMarkerController>(true).Single(m=>m.name=="PlayerMarker");Assert.That(playerMarker.IsVisible,Is.True);Assert.That(playerMarker.IsInsideMap,Is.True);Assert.That(playerMarker.TrackedTarget,Is.EqualTo(Object.FindAnyObjectByType<PlayerControllerScript>().transform));
        yield return Capture("Library/MainIntegration-map.png",1280,720);
        presentation.Map.SetOpen(false,true);yield return null;yield return null;Assert.That(missions.IsVisible,Is.True);Assert.That(weapon.CanShoot,Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator NativeWeapon_ReloadAimAndReadingLock_KeepBattleTrigger()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var weapon=Object.FindAnyObjectByType<PlayerWeaponController>();if(weapon.EquippedWeapon==null)GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;
        Assert.That(weapon.CanShoot,Is.True);Assert.That(weapon.TryShoot(),Is.True);Assert.That(weapon.RoundsInMagazine,Is.EqualTo(19));Assert.That(weapon.TryReload(),Is.True);Assert.That(weapon.TryShoot(),Is.False);
        Time.timeScale=10;float until=Time.realtimeSinceStartup+4;while(weapon.Reloading&&Time.realtimeSinceStartup<until)yield return null;Time.timeScale=1;Assert.That(weapon.RoundsInMagazine,Is.EqualTo(20));Assert.That(weapon.ReserveRounds,Is.EqualTo(59));
        weapon.GetComponent<PlayerWeaponInput>().enabled=false;float fov=Camera.main.fieldOfView;weapon.SetAim(true);until=Time.unscaledTime+0.5f;while(Time.unscaledTime<until)yield return null;Assert.That(Camera.main.fieldOfView,Is.LessThan(fov-5));
        var doc=Object.FindObjectsByType<InspectableDocument>(FindObjectsSortMode.None).First(d=>d.name=="Carta2");GameEventBus.Raise(GameplayEvents.StartInspection,doc.gameObject);yield return null;yield return null;Assert.That(weapon.CanShoot,Is.False);Object.FindAnyObjectByType<InspectionScript>().StopInspect();yield return null;yield return null;Assert.That(weapon.CanShoot,Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator NativeNPC_FacesPlayer_AndReturnsAfterConversationCancellation()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var speaker=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).First(s=>s.name.Contains("Mateo"));var rotation=speaker.transform.rotation;var cc=Object.FindAnyObjectByType<PlayerControllerScript>().GetComponent<CharacterController>();cc.enabled=false;cc.transform.position=speaker.transform.position+Vector3.right*3;cc.enabled=true;
        speaker.Speak();float until=Time.unscaledTime+1.5f;while(Time.unscaledTime<until)yield return null;
        Assert.That(Vector3.Dot(speaker.transform.forward,Vector3.right),Is.GreaterThan(0.97f));Object.FindAnyObjectByType<DialogueManagerScript>().CancelConversation(speaker);until=Time.unscaledTime+1.5f;while(Time.unscaledTime<until)yield return null;Assert.That(Quaternion.Angle(speaker.transform.rotation,rotation),Is.LessThan(0.2f));
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator NativeHitscan_RespectsCover_AndDissolvesOnlyShadowTargets()
    {
        EditorSceneManager.OpenScene(ScenePath);yield return new EnterPlayMode();yield return null;
        var weapon=Object.FindAnyObjectByType<PlayerWeaponController>();weapon.EquipWeapon("FAL");weapon.GetComponent<PlayerWeaponInput>().enabled=false;var player=Object.FindAnyObjectByType<PlayerControllerScript>();player.enabled=false;
        var camera=Camera.main;var shadow=GameObject.CreatePrimitive(PrimitiveType.Capsule);shadow.name="TestShadow";shadow.transform.position=camera.transform.position+camera.transform.forward*5;shadow.AddComponent<ShadowDamageTarget>();
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*2.5f,camera.transform.rotation);wall.transform.localScale=new Vector3(3,3,0.15f);Physics.SyncTransforms();
        Assert.That(weapon.TryShoot(),Is.True);yield return null;Assert.That(shadow!=null,Is.True,"The wall must stop the ray.");
        wall.GetComponent<Collider>().enabled=false;Physics.SyncTransforms();float until=Time.time+0.25f;while(Time.time<until)yield return null;Assert.That(weapon.TryShoot(),Is.True);yield return null;Assert.That(shadow==null,Is.True,"A clear hitscan must dissolve the shadow.");Object.Destroy(wall);
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
    // Batch previews show the UI pass separately; final world/UI composition needs Game View review.
    static IEnumerator Capture(string path,int width,int height)
    {
        var camera=Camera.main;var canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(c=>c.isRootCanvas);var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var target=camera.targetTexture;var active=RenderTexture.active;int mask=camera.cullingMask;
        var host=new GameObject("NativeUICaptureCamera");var ui=host.AddComponent<Camera>();ui.CopyFrom(camera);ui.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);ui.cullingMask=1<<5;ui.nearClipPlane=0.01f;ui.clearFlags=CameraClearFlags.Depth;var data=ui.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Overlay;data.renderPostProcessing=false;
        var layers=new Dictionary<GameObject,int>();foreach(var t in canvas.GetComponentsInChildren<Transform>(true)){layers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=5;}
        var stack=camera.GetUniversalAdditionalCameraData().cameraStack;stack.Add(ui);var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;ui.targetTexture=rt;ui.aspect=width/(float)height;ui.ResetProjectionMatrix();camera.cullingMask&=~(1<<5);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=1f;Canvas.ForceUpdateCanvases();foreach(var area in Object.FindObjectsByType<ReferenceCanvasSpace>(FindObjectsSortMode.None))area.Fit();yield return null;Canvas.ForceUpdateCanvases();foreach(var overlay in Object.FindObjectsByType<MapZoneOverlay>(FindObjectsSortMode.None))overlay.Refresh();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});data.renderType=CameraRenderType.Base;ui.clearFlags=CameraClearFlags.SolidColor;ui.backgroundColor=new Color(0.12f,0.14f,0.16f);RenderPipeline.SubmitRenderRequest(ui,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally{foreach(var layer in layers)layer.Key.layer=layer.Value;stack.Remove(ui);camera.cullingMask=mask;camera.targetTexture=target;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=active;Object.DestroyImmediate(host);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);}
    }
}
#endif
