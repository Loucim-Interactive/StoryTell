#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.GameplayPath;
using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using Systems.DialogueSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public sealed class GameplayPathNextTests
{
 [Test]public void Puzzle_RequiresCorrectPositionRotationAndFourDifferentFragments()
 {
  var state=new LetterAssemblyState();Assert.That(state.TryPlace(0,90,0),Is.False);Assert.That(state.TryPlace(0,0,90),Is.False);Assert.That(state.TryPlace(-1,0,0),Is.False);
  for(int i=0;i<4;i++){Assert.That(state.TryPlace(i,8,360),Is.True);Assert.That(state.TryPlace(i,0,0),Is.False);Assert.That(state.Complete,Is.EqualTo(i==3));}
 }
 [Test]public void SavedScene_HasCompactMappedZones_NoVisibleSigns_AndPhysicalCover()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var controller=All<GameplayPathController>(scene).Single();var map=All<PhysicalMapController>(scene).Single();
  Assert.That(map.Settings.width,Is.EqualTo(0.48f).Within(0.001));Assert.That(map.Settings.height,Is.EqualTo(0.32f).Within(0.001));
  var zones=All<MapZoneOverlay>(scene).Single();Assert.That(zones.Zones.Length,Is.EqualTo(6));
  foreach(var zone in zones.Zones){Assert.That(zone.location,Is.Not.Null);Assert.That(map.GetComponent<MapWorldReference>().TryWorldToMapPosition(zone.location.position,out _,out bool inside),Is.True);Assert.That(inside,Is.True,zone.name);}
  foreach(var sign in controller.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cartel_")))Assert.That(sign.gameObject.activeSelf,Is.False);
  Assert.That(All<NpcConversationFacing>(scene).Length,Is.GreaterThanOrEqualTo(6));
  var covers=All<Transform>(scene).Where(t=>t.name.StartsWith("Cover_Additional_")).ToArray();Assert.That(covers.Length,Is.EqualTo(8));foreach(var cover in covers)Assert.That(cover.GetComponent<BoxCollider>().enabled,Is.True);
 }
 [UnityTest]public IEnumerator Puzzle_CancelResumeSnapAndMandatoryDelivery()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
  var c=Object.FindAnyObjectByType<GameplayPathController>();var p=c.Progress;EquipToLetter(p);
  Assert.That(p.Advance(PathAction.Ruben),Is.False);Assert.That(c.BeginLetterAssembly(),Is.True);yield return null;
  var puzzle=Object.FindAnyObjectByType<LetterAssemblyPuzzle>();var pieces=Object.FindObjectsByType<LetterPuzzlePiece>(FindObjectsSortMode.None).OrderBy(t=>t.slotIndex).ToArray();
  yield return Capture("Library/GameplayPath-next-puzzle.png",1280,720);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));Assert.That(c.IsBusy,Is.True);
  pieces[0].transform.position=puzzle.Slot(0).position;pieces[0].rectTransform.localRotation=Quaternion.Euler(0,0,90);Assert.That(puzzle.TrySnap(pieces[0]),Is.False);
  pieces[0].rectTransform.localRotation=Quaternion.identity;Assert.That(puzzle.TrySnap(pieces[0]),Is.True);Assert.That(Vector3.Distance(pieces[0].transform.position,puzzle.Slot(0).position),Is.LessThan(0.001));
  puzzle.Close();yield return null;Assert.That(puzzle.PlacedCount,Is.EqualTo(1));Assert.That(c.IsBusy,Is.False);Assert.That(p.LetterAssembled,Is.False);Assert.That(c.BeginLetterAssembly(),Is.True);yield return null;
    pieces[1].rectTransform.localRotation=Quaternion.identity;Canvas.ForceUpdateCanvases();
  var pointer=new PointerEventData(EventSystem.current);pointer.position=RectTransformUtility.WorldToScreenPoint(null,pieces[1].transform.position);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);bool reachable=false;foreach(var hit in hits)if(hit.gameObject==pieces[1].gameObject)reachable=true;Assert.That(reachable,Is.True,"The visible paper must receive pointer input through the actual UI raycaster.");
  ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.beginDragHandler);pointer.position=RectTransformUtility.WorldToScreenPoint(null,puzzle.Slot(1).position);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(pieces[1].gameObject,pointer,ExecuteEvents.endDragHandler);Assert.That(pieces[1].Placed,Is.True,"Dragging through pointer events must place a fragment.");
  for(int i=2;i<4;i++){pieces[i].transform.position=puzzle.Slot(i).position;pieces[i].rectTransform.localRotation=Quaternion.identity;Assert.That(puzzle.TrySnap(pieces[i]),Is.True);}
  Assert.That(p.LetterAssembled,Is.True);Assert.That(p.Stage,Is.EqualTo(PathStage.ReturnLetter));yield return Capture("Library/GameplayPath-next-puzzle-complete.png",1280,720);
  puzzle.Close();Assert.That(p.Advance(PathAction.Ruben),Is.True);Assert.That(p.Stage,Is.EqualTo(PathStage.Sergeant));
  var map=Object.FindAnyObjectByType<PhysicalMapController>();map.enabled=true;map.SetOpen(true,true);yield return null;
  yield return Capture("Library/GameplayPath-next-map.png",1280,720);yield return Capture("Library/GameplayPath-next-map-square.png",1280,1024);
  yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator NPC_TurnsDuringConversationAndRestoresAfterCancellation()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
  var c=Object.FindAnyObjectByType<GameplayPathController>();var npc=Object.FindObjectsByType<NpcConversationFacing>(FindObjectsSortMode.None).First(n=>n.name=="Ruben_01_Arrival");var rotation=npc.transform.rotation;
  var cc=c.Player.GetComponent<CharacterController>();cc.enabled=false;c.Player.position=npc.transform.position+Vector3.right*3;cc.enabled=true;
  c.Interact(PathAction.Ruben,0,null,npc.transform);float until=Time.unscaledTime+1.8f;while(Time.unscaledTime<until)yield return null;
  Vector3 expected=Vector3.ProjectOnPlane(c.Player.position-npc.transform.position,Vector3.up).normalized;Assert.That(Vector3.Dot(npc.transform.forward,expected),Is.GreaterThan(0.97f));
  Object.FindAnyObjectByType<DialogueManagerScript>().CancelConversation(c);until=Time.unscaledTime+1.8f;while(Time.unscaledTime<until)yield return null;
  Assert.That(Quaternion.Angle(npc.transform.rotation,rotation),Is.LessThan(0.2f));yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator EnemyContact_IgnoresPlayerCollider_RespectsWall_AndShowsEnding()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
  var c=Object.FindAnyObjectByType<GameplayPathController>();var p=c.Progress;EquipToLetter(p);p.AssembleLetter();p.Advance(PathAction.Ruben);p.Advance(PathAction.Sergeant);p.Advance(PathAction.Crate);p.Advance(PathAction.Officer);
  var gate=Object.FindAnyObjectByType<GameplayPathBattleGate>();var cc=c.Player.GetComponent<CharacterController>();cc.enabled=false;c.Player.position=gate.transform.position-Vector3.up*2;cc.enabled=true;Assert.That(c.TryEnterBattle(),Is.True);
  float until=Time.realtimeSinceStartup+12;while(Object.FindObjectsByType<GameplayPathEnemy>(FindObjectsSortMode.None).Length==0&&Time.realtimeSinceStartup<until)yield return null;
  var enemy=Object.FindObjectsByType<GameplayPathEnemy>(FindObjectsSortMode.None).First();var trial=Object.FindAnyObjectByType<Level1WeaponTrial>();
  enemy.GetComponent<NavMeshAgent>().enabled=false;enemy.transform.position=c.Player.position+Vector3.right*0.75f;
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="ContactTestWall";wall.transform.position=c.Player.position+Vector3.right*0.375f+Vector3.up;wall.transform.localScale=new Vector3(0.08f,3,2);Physics.SyncTransforms();
  Assert.That(enemy.CheckPlayerContact(),Is.False,"A wall still blocks contact.");Assert.That(trial.IsFinishing,Is.False);
  wall.GetComponent<Collider>().enabled=false;Physics.SyncTransforms();Assert.That(enemy.CheckPlayerContact(),Is.True,"The player's collider must not count as cover.");Assert.That(trial.IsFinishing,Is.True);
  until=Time.realtimeSinceStartup+5;while(!trial.Completed&&Time.realtimeSinceStartup<until)yield return null;
  Assert.That(trial.Completed,Is.True);Assert.That(trial.CentralDarkness,Is.EqualTo(1));var title=(TMP_Text)new SerializedObject(trial).FindProperty("completion").objectReferenceValue;Assert.That(title.gameObject.activeInHierarchy,Is.True);Assert.That(title.text,Does.Contain("escena 1"));yield return Capture("Library/GameplayPath-next-contact-ending.png",1280,720);
  Object.Destroy(wall);yield return new ExitPlayMode();
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
 static void EquipToLetter(GameplayPathProgress p){p.Advance(PathAction.Ruben);p.Collect(PathEquipment.All);p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);for(int i=0;i<4;i++)p.CollectFragment(i);}
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static IEnumerator Capture(string file,int width,int height)
 {
  var camera=Camera.main;var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var previousCameras=canvases.Select(c=>c.worldCamera).ToArray();var previousTarget=camera.targetTexture;var previous=RenderTexture.active;
  var rt=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);var layers=new Dictionary<GameObject,int>();
  var uiHost=new GameObject("CaptureUICamera");var uiCamera=uiHost.AddComponent<Camera>();
  try
  {
   rt.Create();camera.targetTexture=rt;uiCamera.CopyFrom(camera);uiCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);uiCamera.cullingMask=1<<5;uiCamera.nearClipPlane=0.01f;uiCamera.clearFlags=CameraClearFlags.Nothing;uiCamera.targetTexture=rt;
   var uiData=uiCamera.GetUniversalAdditionalCameraData();uiData.renderType=CameraRenderType.Base;var uiSettings=new SerializedObject(uiData);uiSettings.FindProperty("m_ClearDepth").boolValue=true;uiSettings.ApplyModifiedPropertiesWithoutUndo();uiData.renderPostProcessing=false;uiCamera.depth=camera.depth+100;
   foreach(var canvas in canvases)
   {
    foreach(var t in canvas.GetComponentsInChildren<Transform>(true))if(!layers.ContainsKey(t.gameObject)){layers.Add(t.gameObject,t.gameObject.layer);t.gameObject.layer=5;}
    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=0.1f;
   }
   Canvas.ForceUpdateCanvases();
   yield return null;
   Canvas.ForceUpdateCanvases();var map=Object.FindAnyObjectByType<PhysicalMapController>();if(map!=null)map.GetComponent<PhysicalMapViewCamera>().MapCamera.aspect=camera.aspect;
   foreach(var overlay in Object.FindObjectsByType<MapZoneOverlay>(FindObjectsSortMode.None))overlay.Refresh();
   RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderPipeline.SubmitRenderRequest(uiCamera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(file,pixels.EncodeToPNG());
  }
  finally
  {
   camera.targetTexture=previousTarget;RenderTexture.active=previous;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=previousCameras[i];}foreach(var entry in layers)entry.Key.layer=entry.Value;Object.DestroyImmediate(uiHost);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
  }
 }
}
#endif
