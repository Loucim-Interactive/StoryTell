#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.DialogueSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.BattleSystem;
using Systems.EventSystem.Scripts;
using Systems.WeaponSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class CampExpansionTests
{
 const string Scene="Assets/Scenes/Level1.unity";
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static SpeakerScript Actor(string name)=>Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name.Contains(name));
 static IEnumerator Wait(float t){float until=Time.unscaledTime+t;while(Time.unscaledTime<until)yield return null;}
 static IEnumerator Talk(SpeakerScript actor)
 {
  actor.Speak();yield return Wait(.2f);float end=Time.unscaledTime+30;
  while(One<DialogueManagerScript>().IsInConversation&&Time.unscaledTime<end){One<DialogueManagerScript>().uiDialogueScript.RequestAdvance();yield return Wait(.18f);}
  Assert.That(One<DialogueManagerScript>().IsInConversation,Is.False,actor.name);yield return null;
 }
 static void FinishLetter()
 {
  var equipment=One<EquipmentManager>();equipment.StartMission();var eq=new SerializedObject(equipment).FindProperty("equipment");for(int i=0;i<eq.arraySize;i++)((EquipmentInteractableScript)eq.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();One<CampRoutine>().Restore(true,true);
  var letter=One<LetterManager>();letter.StartMission();foreach(var fragment in Object.FindObjectsByType<LetterInteractableScript>())if(fragment.name.StartsWith("MateoLetterFragment"))fragment.OnInspectionEnded();letter.MarkLetterAssembled();letter.MarkLetterRead();letter.CompleteMission();
 }
 static void Move(float x,float z)
 {
  var body=One<Systems.InteractionSystem.Scripts.InteractionScript>().GetComponent<CharacterController>();var terrain=One<Terrain>();body.enabled=false;body.transform.position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+.1f,z);body.transform.rotation=Quaternion.identity;body.enabled=true;Physics.SyncTransforms();
 }
 [UnityTest]public IEnumerator DispatchIsLinearAndThePackageStaysInTheBackpack()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;var dispatch=One<SupplyDispatch>();
  yield return Talk(Actor("Sargento Ibarra"));yield return Talk(Actor("Ruben"));yield return Talk(Actor("Benitez"));Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.None));Assert.That(dispatch.CollectPackage(),Is.False);Assert.That(One<CampRoutine>().ReadyForFront,Is.False);
  FinishLetter();yield return null;Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.Sergeant));Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Ibarra"));Assert.That(One<RouteBoundary>().CanProceed,Is.False);
  yield return Talk(Actor("Ruben"));Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.Sergeant));yield return Talk(Actor("Sargento Ibarra"));Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.Outpost));Assert.That(One<RouteBoundary>().CanProceed,Is.True);Assert.That(One<CampRoutine>().ReadyForFront,Is.False);
  GameEventBus.Raise(GameplayEvents.OnBattleTriggered);Assert.That(One<LevelProgress>().Stage,Is.Not.EqualTo(RouteStage.Battle));Assert.That(One<ShadowSpawner>().HasStarted,Is.False);
  yield return Talk(Actor("Benitez"));Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.Outpost));yield return Talk(Actor("Ruben"));Assert.That(dispatch.Stage,Is.EqualTo(DispatchStage.Collect));var package=One<RadioPartsPickup>();Assert.That(package.gameObject.activeSelf,Is.True);int rounds=One<PlayerWeaponController>().ReserveRounds;package.Interact();yield return null;
  Assert.That(dispatch.HasPackage,Is.True);Assert.That(package.gameObject.activeSelf,Is.False);Assert.That(One<InspectionScript>().IsInspecting,Is.False);Assert.That(One<PlayerWeaponController>().ReserveRounds,Is.EqualTo(rounds));Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Benítez"));
  Move(45,500);yield return null;Move(45,514);yield return null;Assert.That(One<Systems.InteractionSystem.Scripts.InteractionScript>().transform.position.z,Is.LessThanOrEqualTo(510));
  yield return Talk(Actor("Benitez"));Assert.That(dispatch.Delivered,Is.True);Assert.That(One<CampRoutine>().ReadyForFront,Is.True);Assert.That(dispatch.HasPackage,Is.False);Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("frente"));yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator DispatchCheckpointRestoresThePackageWithoutSkippingDelivery()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;FinishLetter();var dispatch=One<SupplyDispatch>();dispatch.Restore(DispatchStage.Carrying);var checkpoints=One<CheckpointController>();var saved=checkpoints.Capture();Assert.That(saved.IsValid,Is.True);Assert.That(saved.dispatchSchema,Is.EqualTo(1));Assert.That(saved.dispatchStage,Is.EqualTo((int)DispatchStage.Carrying));
  string path=Path.GetFullPath("Library/CampExpansion-save.json");var store=new CheckpointStore(path);Assert.That(store.TryWrite(saved),Is.True);Assert.That(store.TryRead(out var read),Is.True);dispatch.Restore(DispatchStage.Delivered);Assert.That(checkpoints.Restore(read),Is.True);Assert.That(dispatch.HasPackage,Is.True);Assert.That(dispatch.Delivered,Is.False);Assert.That(One<RadioPartsPickup>(true),Is.Not.Null);
  var package=Object.FindAnyObjectByType<RadioPartsPickup>(FindObjectsInactive.Include);Assert.That(package.gameObject.activeSelf,Is.False);yield return Talk(Actor("Benitez"));Assert.That(dispatch.Delivered,Is.True);
  File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");yield return new ExitPlayMode();
 }
 static T One<T>(bool inactive)where T:Object=>Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
 [UnityTest]public IEnumerator OlderSavesKeepTheirLetterAndResumeAtTheSergeant()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;var saved=new CheckpointData{stage=RouteStage.Outpost,equipmentStarted=true,equipmentMask=7,letterStarted=true,letterMask=15,letterCompleted=true,puzzleMask=15,letterRead=true,ate=true,briefed=true};Assert.That(One<CheckpointController>().Restore(saved),Is.True);yield return null;
  Assert.That(One<SupplyDispatch>().Stage,Is.EqualTo(DispatchStage.Sergeant));Assert.That(One<LetterManager>().MissionCompleted,Is.True);Assert.That(One<LevelProgress>().Stage,Is.EqualTo(RouteStage.Camp));Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Ibarra"));Assert.That(One<RouteBoundary>().CanProceed,Is.False);yield return new ExitPlayMode();
 }
 [Test]public void ExpandedCampHasClearTentFootprintsAndRegisteredSoldiers()
 {
  EditorSceneManager.OpenScene(Scene);var tents=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Carpa del campamento")).ToArray();Assert.That(tents.Length,Is.EqualTo(3));var original=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Tent_")&&r.bounds.center.z<320).ToArray();
  foreach(var tent in tents){Assert.That(tent.GetComponentsInChildren<Collider>().Length,Is.GreaterThan(0));foreach(var other in original)Assert.That(tent.bounds.Intersects(other.bounds),Is.False,tent.name+" overlaps "+other.name);}
  foreach(var fragment in Object.FindObjectsByType<LetterInteractableScript>(FindObjectsInactive.Include))if(fragment.name.StartsWith("MateoLetterFragment"))foreach(var tent in tents)Assert.That(tent.bounds.Contains(fragment.transform.position),Is.False,fragment.name+" covered by "+tent.name);
  var actors=Object.FindObjectsByType<SpeakerScript>();Assert.That(actors.Length,Is.EqualTo(15));foreach(var actor in actors)foreach(var target in actor.GetComponentsInChildren<NPCInteractableScript>())Assert.That(target.speaker,Is.SameAs(actor));
  var bindings=new SerializedObject(One<CheckpointController>()).FindProperty("speakers");var ids=new string[bindings.arraySize];for(int i=0;i<ids.Length;i++)ids[i]=bindings.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;Assert.That(ids.Distinct().Count(),Is.EqualTo(15));
  var voices=new SerializedObject(One<CampAmbientDialogue>()).FindProperty("voices");Assert.That(voices.arraySize,Is.EqualTo(15));for(int i=0;i<voices.arraySize;i++)Assert.That(voices.GetArrayElementAtIndex(i).FindPropertyRelative("lines").arraySize,Is.GreaterThanOrEqualTo(5));Assert.That(One<MapZoneOverlay>().Zones.Length,Is.EqualTo(7));Assert.That(new SerializedObject(One<ScenicFocus>()).FindProperty("views").arraySize,Is.EqualTo(5));
  var table=Object.FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Mesa de repuestos del puesto");foreach(var fire in Object.FindObjectsByType<CampfireMotion>()){var point=fire.transform.position;var closest=table.bounds.ClosestPoint(point);Assert.That(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(closest.x,closest.z)),Is.GreaterThan(1.5f),"The wooden dispatch table must stay clear of the fire");}
 }
 [UnityTest]public IEnumerator EnemyEntriesKeepTheirDistanceWhileThePlayerAdvances()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;FinishLetter();One<SupplyDispatch>().Restore(DispatchStage.Delivered);Move(56,531);GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;Assert.That(One<ShadowSpawner>().ActiveCount,Is.InRange(5,8));
  var player=One<Systems.InteractionSystem.Scripts.InteractionScript>().transform;foreach(var enemy in Object.FindObjectsByType<AdvancingShadow>()){Assert.That(Vector3.ProjectOnPlane(enemy.transform.position-player.position,Vector3.up).magnitude,Is.GreaterThan(27.5f));enemy.GetComponent<ShadowDamageTarget>().ReceiveHit();}Move(56,570);yield return Wait(1.3f);Assert.That(One<ShadowSpawner>().ActiveCount,Is.GreaterThan(0));
  foreach(var enemy in Object.FindObjectsByType<AdvancingShadow>()){Assert.That(One<ShadowSpawner>().IsAhead(enemy.transform.position),Is.True);Assert.That(Vector3.ProjectOnPlane(enemy.transform.position-player.position,Vector3.up).magnitude,Is.GreaterThan(27.5f));Assert.That(enemy.GetComponent<NavMeshAgent>().hasPath,Is.True);}Assert.That(One<CombatConclusion>().Ending,Is.False);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator RadioPartsCanBeApproachedAndSeenFromAStandingPlayer()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;FinishLetter();One<SupplyDispatch>().Restore(DispatchStage.Collect);yield return null;yield return null;Physics.SyncTransforms();var package=One<RadioPartsPickup>();var terrain=One<Terrain>();var target=package.GetComponent<Renderer>().bounds.center;bool reachable=false;Vector3 viewpoint=Vector3.zero;
  for(int i=0;i<12;i++)
  {
   float angle=i*Mathf.PI/6;var foot=target+new Vector3(Mathf.Cos(angle)*2.2f,0,Mathf.Sin(angle)*2.2f);foot.y=terrain.SampleHeight(foot)+terrain.transform.position.y;
   var obstacles=Physics.OverlapCapsule(foot+Vector3.up*.45f,foot+Vector3.up*1.45f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);if(obstacles.Any(c=>!(c is TerrainCollider)))continue;
   var eye=foot+Vector3.up*1.65f;var ray=Physics.RaycastAll(eye,(target-eye).normalized,Vector3.Distance(eye,target)+.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).FirstOrDefault();if(ray.collider&&ray.collider.GetComponentInParent<RadioPartsPickup>()==package){reachable=true;viewpoint=eye;break;}
  }
  Assert.That(reachable,Is.True,"The new package must be within interaction range from an unobstructed standing position");yield return CaptureWorld(viewpoint,target,"Library/CampExpansion-parts-table.png");yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator CampAndRadioPostAreVisibleAtPlayerScale()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();for(int i=0;i<10;i++)yield return null;var terrain=One<Terrain>();var kitchen=One<CampMeal>().transform.position;
  yield return CaptureWorld(new Vector3(86,terrain.SampleHeight(new Vector3(86,0,275))+terrain.transform.position.y+2.3f,275),kitchen,"Library/CampExpansion-kitchen.png");
  yield return CaptureWorld(new Vector3(74,terrain.SampleHeight(new Vector3(74,0,281))+terrain.transform.position.y+55,281),new Vector3(74,terrain.SampleHeight(new Vector3(74,0,282))+terrain.transform.position.y,282),"Library/CampExpansion-layout.png");
  var receiver=Actor("Benitez").transform.position;yield return CaptureWorld(receiver+new Vector3(1.8f,1.7f,-4),receiver+Vector3.up*.9f,"Library/CampExpansion-radio-post.png");One<MainPresentation>().UnlockMap();One<MainPresentation>().Map.SetOpen(true,true);yield return Wait(.3f);yield return MainIntegrationTests.Capture("Library/CampExpansion-map.png",1280,720);One<MainPresentation>().Map.SetOpen(false,true);yield return new ExitPlayMode();
 }
 static IEnumerator CaptureWorld(Vector3 position,Vector3 target,string file)
 {
  var go=new GameObject("Camp expansion verification",typeof(Camera));var camera=go.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=65;camera.cullingMask=~(1<<5);camera.transform.position=position;camera.transform.LookAt(target);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return Wait(.25f);var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;
  try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(file,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
