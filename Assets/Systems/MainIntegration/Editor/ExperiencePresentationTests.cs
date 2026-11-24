#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using Systems.DialogueSystem.Scripts;
using Systems.DecisionSystem;
using Systems.EventSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InspectionSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.WalkieSystem.Scripts;
using Systems.BattleSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class ExperiencePresentationTests
{
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static IEnumerator Wait(float seconds){float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}
 InputSettings originalSettings,temporarySettings;Keyboard addedKeyboard;
 [Test]public void MarcosMatchesTheLetterAndKeepsHisSaveIdentity()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var actor=Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name.Contains("Marcos"));Assert.That((int)ESpeakers.Marcos,Is.EqualTo(12));var linked=(SpeakerScript)new SerializedObject(One<CampRoutine>()).FindProperty("mateo").objectReferenceValue;Assert.That(linked,Is.EqualTo(actor));Assert.That(actor.GetComponentInChildren<NPCInteractableScript>().interactableName,Is.EqualTo("Marcos"));Assert.That(One<LetterManager>().MissionTitle,Does.Contain("Marcos"));Assert.That(One<MapZoneOverlay>().Zones[1].name,Is.EqualTo("Marcos"));var cp=new SerializedObject(One<CheckpointController>()).FindProperty("speakers");var entry=Enumerable.Range(0,cp.arraySize).Select(i=>cp.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("speaker").objectReferenceValue==actor);Assert.That(entry.FindPropertyRelative("id").stringValue,Is.EqualTo(GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString()));
 }
 [UnityTest]public IEnumerator MouseInteractionShowsOnlyItsPictogramAndAction()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;One<InteractionScript>().enabled=false;var actor=Object.FindObjectsByType<NPCInteractableScript>().First();var ui=One<InteractionUIManager>();ui.DisplayInteraction(actor);var data=new SerializedObject(ui);var label=(TMP_Text)data.FindProperty("interactionAction").objectReferenceValue;var image=(Image)data.FindProperty("interactionImage").objectReferenceValue;Assert.That(image.enabled&&image.sprite,Is.True);Assert.That(image.rectTransform.rect.width,Is.GreaterThanOrEqualTo(60));Assert.That(label.text,Does.Not.Contain("Clic"));Assert.That(label.text,Does.Contain("Hablar"));yield return CaptureGame("Library/ExperiencePresentation-interaction.png");yield return new ExitPlayMode();
 }
 static void CompleteLetter()
 {
  var eq=One<EquipmentManager>();eq.StartMission();var items=new SerializedObject(eq).FindProperty("equipment");for(int i=0;i<items.arraySize;i++)((EquipmentInteractableScript)items.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();One<CampRoutine>().Restore(true,true);var letter=One<LetterManager>();letter.StartMission();foreach(var f in Object.FindObjectsByType<LetterInteractableScript>())if(f.name.StartsWith("MateoLetterFragment"))f.OnInspectionEnded();letter.MarkLetterAssembled();letter.MarkLetterRead();letter.CompleteMission();
 }
 [UnityTest]public IEnumerator CombatEndingClosesTheDeliveredLetterWindow()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;CompleteLetter();One<SupplyDispatch>().Restore(DispatchStage.Delivered);var player=One<InteractionScript>().GetComponent<CharacterController>();player.enabled=false;var terrain=One<Terrain>();player.transform.position=new Vector3(56,terrain.SampleHeight(new Vector3(56,0,531))+.1f,531);player.enabled=true;GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;Assert.That(One<ShadowSpawner>().HasStarted,Is.True);Assert.That(One<MainPresentation>().OpenLetter(),Is.True);One<CombatConclusion>().Finish(true);yield return Wait(2.5f);Assert.That(One<DeliveredLetterReader>().IsOpen,Is.False);Assert.That(One<CombatConclusion>().Finished,Is.True);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator DeliveredLetterUsesCenteredTextWithoutAWorldModelAndEscapeRestoresControls()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;CompleteLetter();var presentation=One<MainPresentation>();Assert.That(presentation.OpenLetter(),Is.True);yield return Wait(.3f);var reader=One<DeliveredLetterReader>();Assert.That(reader.IsOpen,Is.True);Assert.That(reader.Content,Is.EqualTo(presentation.LetterText));Assert.That(One<InspectionScript>().IsInspecting,Is.False);var model=(InspectableDocument)new SerializedObject(presentation).FindProperty("completedLetter").objectReferenceValue;Assert.That(model.gameObject.activeInHierarchy,Is.False);Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.LetterText));yield return CaptureGame("Library/ExperiencePresentation-letter-text.png");
  originalSettings=InputSystem.settings;temporarySettings=Object.Instantiate(originalSettings);temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=temporarySettings;var keyboard=Keyboard.current;if(keyboard==null)keyboard=addedKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;Assert.That(reader.IsOpen,Is.False);Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));Assert.That(One<PauseMenu>().IsOpen,Is.False);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator RadioTimerHasContextAndIsHiddenWhileTheRadioIsStored()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;var radio=One<WalkieInteractionMachine>();float end=Time.unscaledTime+40;while(!radio.IsFinished){Assert.That(Time.unscaledTime,Is.LessThan(end));if(radio.IsChoosing)radio.Resolve(0);yield return Wait(.1f);}
  var call=ScriptableObject.CreateInstance<WalkieDecisionAsset>();var data=new SerializedObject(call);data.FindProperty("promptText").stringValue="Prueba de recepción";data.FindProperty("callerName").stringValue="Comunicaciones";data.FindProperty("responseDelaySeconds").floatValue=0;data.FindProperty("allowIgnore").boolValue=true;data.FindProperty("timeWindowSeconds").floatValue=30;data.FindProperty("choices").arraySize=1;data.FindProperty("choices").GetArrayElementAtIndex(0).FindPropertyRelative("label").stringValue="Recibido";data.ApplyModifiedProperties();GameEventBus.Raise(GameplayEvents.WalkieTalkieTrigger,call);yield return Wait(.4f);Assert.That(radio.IsChoosing,Is.True);var ui=new SerializedObject(One<WalkieUIScript>());var panel=(GameObject)ui.FindProperty("timerPanel").objectReferenceValue;Assert.That(panel.GetComponent<CanvasGroup>().alpha,Is.LessThan(.01f));var walkie=One<WalkieTalkieScript>();typeof(WalkieTalkieScript).GetMethod("EnterWalkie",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(walkie,null);yield return Wait(.5f);Assert.That(panel.GetComponent<CanvasGroup>().alpha,Is.GreaterThan(.9f));Assert.That(panel.transform.Find("TimerPurpose"),Is.Not.Null);walkie.ExitWalkie();yield return Wait(.5f);Assert.That(panel.GetComponent<CanvasGroup>().alpha,Is.LessThan(.01f));radio.Resolve(0);Object.Destroy(call);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator IsolatedMapRemainsCompleteWhileOpeningAndTurning()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;var presentation=One<MainPresentation>();presentation.UnlockMap();var map=presentation.Map;var view=One<PhysicalMapViewCamera>();map.SetOpen(true,false);for(int i=0;i<8;i++)yield return null;Assert.That(view.Target,Is.Not.Null);Assert.That(view.MapCamera.targetTexture,Is.EqualTo(view.Target));Assert.That(Camera.main.GetUniversalAdditionalCameraData().cameraStack.Contains(view.MapCamera),Is.False);Assert.That(view.MapCamera.orthographic,Is.True);
  for(int frame=0;frame<12;frame++){Camera.main.transform.rotation*=Quaternion.Euler(0,4,0);yield return null;foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f}){var point=view.MapCamera.WorldToViewportPoint(map.transform.TransformPoint(new Vector3(map.Settings.width*x,0,map.Settings.height*z)));Assert.That(point.x,Is.InRange(-.015f,1.015f));Assert.That(point.y,Is.InRange(-.015f,1.015f));}}
  var rect=view.DisplayRect;Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(rect.xMax,Is.LessThan(Screen.width*.71f));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(rect.yMax,Is.LessThanOrEqualTo(Screen.height));yield return CaptureGame("Library/ExperiencePresentation-map-native.png");map.SetOpen(false,true);yield return null;Assert.That(view.MapCamera.enabled,Is.False);yield return new ExitPlayMode();
 }
 [Test]public void ExtendedGroundJoinsTheTerrainAndTheSurveySamplesInsideIt()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=One<Terrain>();var skirt=Object.FindObjectsByType<MeshFilter>().Single(f=>f.name=="HorizonTerrainSkirt");var size=terrain.terrainData.size;int boundary=0;foreach(var vertex in skirt.sharedMesh.vertices){var p=skirt.transform.TransformPoint(vertex);if(p.x<-.001f||p.z<-.001f||p.x>size.x+.001f||p.z>size.z+.001f)continue;if(Mathf.Min(p.x,p.z,size.x-p.x,size.z-p.z)>.005f)continue;boundary++;Assert.That(Mathf.Abs(p.y-terrain.SampleHeight(p)-terrain.transform.position.y),Is.LessThan(.035f));}Assert.That(boundary,Is.GreaterThan(700));Assert.That(skirt.GetComponent<Renderer>().bounds.min.x,Is.LessThan(-800));var reference=One<MapWorldReference>();foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f}){var p=reference.WorldOrigin.position+reference.WorldOrigin.right*x*reference.WorldSize.x+reference.WorldOrigin.forward*z*reference.WorldSize.y;Assert.That(p.x,Is.InRange(-.001f,size.x+.001f));Assert.That(p.z,Is.InRange(-.001f,size.z+.001f));}Assert.That(reference.WorldSize.x/reference.WorldSize.y,Is.EqualTo(1.5f).Within(.001));
 }
 [UnityTest]public IEnumerator KitchenCompanionsFormAConversationGroup()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<12;i++)yield return null;var actors=Object.FindObjectsByType<SpeakerScript>();var mateo=actors.Single(a=>a.name.Contains("Marcos"));var simon=actors.Single(a=>a.name=="NPC - Simon");var a=mateo.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Main Body").bounds.center;var b=simon.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Main Body").bounds.center;Assert.That(Vector3.Distance(a,b),Is.InRange(1,3));Assert.That(Vector3.Dot(mateo.transform.forward,Vector3.ProjectOnPlane(b-a,Vector3.up).normalized),Is.GreaterThan(.65f));Assert.That(Vector3.Dot(simon.transform.forward,Vector3.ProjectOnPlane(a-b,Vector3.up).normalized),Is.GreaterThan(.65f));var terrain=One<Terrain>();yield return CaptureWorld(new Vector3(88,terrain.SampleHeight(new Vector3(88,0,263))+1.75f,263),new Vector3(84.5f,29,256),"Library/ExperiencePresentation-kitchen-group.png");yield return CaptureWorld(new Vector3(45,terrain.SampleHeight(new Vector3(45,0,270))+1.75f,270),new Vector3(-100,10,270),"Library/ExperiencePresentation-coast.png");yield return CaptureWorld(new Vector3(72,terrain.SampleHeight(new Vector3(72,0,514))+1.75f,514),new Vector3(82,terrain.SampleHeight(new Vector3(82,0,523))+.15f,523),"Library/ExperiencePresentation-front-wear.png");yield return new ExitPlayMode();
 }
 static IEnumerator CaptureGame(string path)
 {
  if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int frame=0;frame<12;frame++)yield return null;if(!File.Exists(path))yield return MainIntegrationTests.Capture(path,1280,720);Assert.That(File.Exists(path),Is.True,path);
 }
 static IEnumerator CaptureWorld(Vector3 from,Vector3 target,string path)
 {
  var go=new GameObject("Experience review camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=Camera.main.farClipPlane;camera.cullingMask=~(1<<5);camera.transform.position=from;camera.transform.LookAt(target);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return null;var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(addedKeyboard!=null){InputSystem.RemoveDevice(addedKeyboard);addedKeyboard=null;}if(originalSettings!=null){InputSystem.settings=originalSettings;originalSettings=null;}if(temporarySettings!=null){Object.Destroy(temporarySettings);temporarySettings=null;}if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
