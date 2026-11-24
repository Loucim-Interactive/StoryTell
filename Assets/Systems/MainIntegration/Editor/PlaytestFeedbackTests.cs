#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.DialogueSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
using System.IO;
using System.Collections.Generic;
using StoryTell.PhysicalMap;
using Systems.InspectionSystem.Scripts;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using TMPro;
public sealed class PlaytestFeedbackTests
{
 private Mouse addedMouse;
 private InputSettings originalInputSettings, testInputSettings;
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 [UnityTest]public IEnumerator HearingDiegosMealInstructionThenLeavingDoesNotBlockEating()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
  var equipment=One<EquipmentManager>();equipment.StartMission();var items=new SerializedObject(equipment).FindProperty("equipment");for(int i=0;i<items.arraySize;i++)((EquipmentInteractableScript)items.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();
  Assert.That(equipment.MissionCompleted,Is.True);var diego=Object.FindObjectsByType<SpeakerScript>().Single(n=>n.name.Contains("Diego"));diego.Speak();yield return null;yield return null;Assert.That(One<DialogueManagerScript>().IsConversationOwnedBy(diego),Is.True);One<DialogueManagerScript>().CancelConversation(diego);yield return null;
  One<CampMeal>().Interact();Assert.That(One<CampRoutine>().Ate,Is.True,"Leaving after receiving the meal instruction must not force a return to Diego");yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator EquipmentBriefingFoodAndMateoStayInOrderWithoutRepeatedEating()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
  var routine=One<CampRoutine>();Assert.That(routine.Eat(),Is.False);var mateo=Object.FindObjectsByType<SpeakerScript>().Single(n=>n.name.Contains("Marcos"));Assert.That(routine.ConversationOverride(mateo).Dialogues[0].DialogueText,Does.Contain("Diego"));
  var equipment=One<EquipmentManager>();equipment.StartMission();var items=new SerializedObject(equipment).FindProperty("equipment");for(int i=0;i<items.arraySize;i++)((EquipmentInteractableScript)items.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();Assert.That(routine.Eat(),Is.False);
  var diego=Object.FindObjectsByType<SpeakerScript>().Single(n=>n.name.Contains("Diego"));diego.Speak();yield return null;One<DialogueManagerScript>().CancelConversation();yield return null;Assert.That(routine.Briefed,Is.True);One<CampMeal>().Interact();Assert.That(routine.Ate,Is.True);Assert.That(routine.Eat(),Is.False);Assert.That(routine.ConversationOverride(mateo),Is.Null);Assert.That(routine.ConversationOverride(diego),Is.Not.Null);diego.Speak();yield return null;One<DialogueManagerScript>().CancelConversation();Assert.That(routine.Ate,Is.True);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator HeldObjectRotatesFromMouseInputAndExplainsTheHeldButton()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
  var equipment=One<EquipmentManager>();equipment.StartMission();var prop=Object.FindObjectsByType<EquipmentInteractableScript>().First(i=>i.name=="Casco");var inspection=One<InspectionScript>();inspection.StartInspect(prop.gameObject);for(int i=0;i<5;i++)yield return null;var before=prop.transform.rotation;
  originalInputSettings=InputSystem.settings;testInputSettings=Object.Instantiate(originalInputSettings);testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=testInputSettings;
  var mouse=Mouse.current;if(mouse==null)mouse=addedMouse=InputSystem.AddDevice<Mouse>();for(int i=0;i<5;i++){InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,delta=new Vector2(35,15)});yield return null;}Assert.That(Quaternion.Angle(before,prop.transform.rotation),Is.GreaterThan(1));InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
  var help=(TMP_Text)new SerializedObject(One<InspectionControls>()).FindProperty("label").objectReferenceValue;Assert.That(help.text,Does.Contain("Mantené"));Assert.That(help.gameObject.activeInHierarchy,Is.True);inspection.StopInspect();yield return null;yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator MapFitsAndDestinationLeadersStayAttachedAtSeveralAspectRatios()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;One<MainPresentation>().UnlockMap();var map=One<PhysicalMapController>();map.SetOpen(true,true);for(int i=0;i<5;i++)yield return null;
  yield return CaptureMap(1280,720,"Library/PlaytestFeedback-map-wide.png");yield return CaptureMap(1280,1024,"Library/PlaytestFeedback-map-tall.png");map.SetOpen(false,true);yield return new ExitPlayMode();
 }
 static IEnumerator CaptureMap(int width,int height,string path)
 {
  var camera=Camera.main;var canvas=Object.FindObjectsByType<Canvas>().Single(c=>c.isRootCanvas);var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;var active=RenderTexture.active;
  var go=new GameObject("Feedback UI capture",typeof(Camera));var ui=go.GetComponent<Camera>();ui.CopyFrom(camera);ui.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);ui.cullingMask=1<<5;ui.nearClipPlane=.01f;ui.clearFlags=CameraClearFlags.Depth;ui.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Overlay;ui.GetUniversalAdditionalCameraData().renderPostProcessing=false;
  var layers=new Dictionary<GameObject,int>();foreach(var t in canvas.GetComponentsInChildren<Transform>(true)){layers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=5;}var stack=camera.GetUniversalAdditionalCameraData().cameraStack;stack.Add(ui);var rt=new RenderTexture(width,height,24);rt.Create();int oldMask=camera.cullingMask;
  try
  {
   camera.targetTexture=rt;ui.targetTexture=rt;camera.aspect=(float)width/height;ui.aspect=camera.aspect;ui.ResetProjectionMatrix();camera.cullingMask&=~(1<<5);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=1;for(int i=0;i<6;i++){Canvas.ForceUpdateCanvases();foreach(var area in Object.FindObjectsByType<ReferenceCanvasSpace>())area.Fit();yield return null;}
   var map=One<PhysicalMapController>();var view=One<PhysicalMapViewCamera>().MapCamera;foreach(var x in new[]{-.5f,.5f})foreach(var z in new[]{-.5f,.5f}){var point=view.WorldToViewportPoint(map.transform.TransformPoint(new Vector3(map.Settings.width*x,0,map.Settings.height*z)));Assert.That(point.x,Is.InRange(-.015f,1.015f));Assert.That(point.y,Is.InRange(-.015f,1.015f));}
   var overlay=One<MapZoneOverlay>();for(int frame=0;frame<4;frame++){camera.transform.rotation*=Quaternion.Euler(0,3,0);yield return null;overlay.Refresh();foreach(var zone in overlay.Zones.Where(z=>z.marker.gameObject.activeSelf)){One<MapWorldReference>().TryWorldToMapPosition(zone.location.position,out var mapped,out _);var viewport=view.WorldToViewportPoint(mapped);var source=One<PhysicalMapViewCamera>().ProjectToScreen(mapped);var marker=RectTransformUtility.WorldToScreenPoint(ui,zone.marker.position);var middle=RectTransformUtility.WorldToScreenPoint(ui,zone.leader.position);Assert.That(Vector2.Distance(middle,(source+marker)*.5f),Is.LessThan(2),zone.name);}}
   Assert.That(overlay.Zones.Count(z=>z.marker.gameObject.activeInHierarchy),Is.EqualTo(8));
   One<MapWorldReference>().TryWorldToMapPosition(map.ViewAnchor.position,out var playerPoint,out _);var playerViewport=view.WorldToViewportPoint(playerPoint);var playerScreen=One<PhysicalMapViewCamera>().ProjectToScreen(playerPoint);
   foreach(var zone in overlay.Zones){var viewport=ui.WorldToViewportPoint(zone.marker.position);Assert.That(viewport.x,Is.InRange(.01f,.70f));Assert.That(viewport.y,Is.InRange(.08f,.92f));Assert.That(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(ui,zone.marker.position),playerScreen),Is.GreaterThan(28));}
   var legend=(TMP_Text)new SerializedObject(overlay).FindProperty("legend").objectReferenceValue;Assert.That(legend.gameObject.activeInHierarchy,Is.True);Assert.That(legend.text,Does.Contain("Cocina / comedor"));Assert.That(legend.isTextTruncated,Is.False);Assert.That(legend.fontSize,Is.GreaterThanOrEqualTo(26));
   var corners=new Vector3[4];legend.rectTransform.GetWorldCorners(corners);foreach(var corner in corners){var screen=RectTransformUtility.WorldToScreenPoint(ui,corner);Assert.That(screen.x,Is.InRange(0,width));Assert.That(screen.y,Is.InRange(0,height));}
   RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();var world=pixels.GetPixels32();
   ui.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Base;ui.clearFlags=CameraClearFlags.SolidColor;ui.backgroundColor=Color.clear;RenderPipeline.SubmitRenderRequest(ui,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();var foreground=pixels.GetPixels32();
   for(int i=0;i<world.Length;i++){int inverse=255-foreground[i].a;world[i]=new Color32((byte)Mathf.Min(255,foreground[i].r+world[i].r*inverse/255),(byte)Mathf.Min(255,foreground[i].g+world[i].g*inverse/255),(byte)Mathf.Min(255,foreground[i].b+world[i].b*inverse/255),255);}pixels.SetPixels32(world);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);
  }
  finally{foreach(var layer in layers)layer.Key.layer=layer.Value;stack.Remove(ui);camera.cullingMask=oldMask;camera.targetTexture=oldTarget;camera.aspect=oldAspect;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(addedMouse!=null){InputSystem.RemoveDevice(addedMouse);addedMouse=null;}if(originalInputSettings!=null){InputSystem.settings=originalInputSettings;originalInputSettings=null;}if(testInputSettings!=null){Object.Destroy(testInputSettings);testInputSettings=null;}if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
