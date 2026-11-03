#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InspectionSystem.Scripts;
using Systems.DialogueSystem.Scripts;
using Systems.DecisionSystem.UI;
using Systems.BattleSystem;
using Systems.WeaponSystem;
using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using Object=UnityEngine.Object;
public sealed class ExperienceRevisionTests
{
 const string Scene="Assets/Scenes/Level1.unity";
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static SpeakerScript Npc(string name)=>Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).Single(n=>n.name.Contains(name));
 static IEnumerator Wait(float seconds){float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}
 static IEnumerator Talk(SpeakerScript npc)
 {
  npc.Speak();yield return Wait(.25f);float end=Time.unscaledTime+25;
  while(One<DialogueManagerScript>().IsInConversation&&Time.unscaledTime<end){if(One<DecisionManagerScript>().HasRequest)One<DecisionManagerScript>().Submit(0);else One<DialogueManagerScript>().uiDialogueScript.RequestAdvance();yield return Wait(.22f);}
  Assert.That(One<DialogueManagerScript>().IsInConversation,Is.False);yield return null;
 }
 static void Equip(){var eq=One<EquipmentManager>();eq.StartMission();var items=new SerializedObject(eq).FindProperty("equipment");for(int i=0;i<items.arraySize;i++)((EquipmentInteractableScript)items.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();}
 static void Ready(){Equip();One<CampRoutine>().Restore(true,true);var l=One<LetterManager>();l.StartMission();foreach(var f in Object.FindObjectsByType<LetterInteractableScript>(FindObjectsSortMode.None))if(f.name.StartsWith("MateoLetterFragment"))f.OnInspectionEnded();l.MarkLetterAssembled();l.MarkLetterRead();l.CompleteMission();}
 static void AtFront(){var player=One<Systems.InteractionSystem.Scripts.InteractionScript>().transform;var cc=player.GetComponent<CharacterController>();cc.enabled=false;var terrain=One<Terrain>();var ground=new Vector3(56,terrain.SampleHeight(new Vector3(56,0,531))+terrain.transform.position.y,531);Assert.That(NavMesh.SamplePosition(ground,out var p,2,NavMesh.AllAreas),Is.True);player.position=p.position+Vector3.up*.1f;cc.enabled=true;Physics.SyncTransforms();}
 [UnityTest]public IEnumerator CampRouteRequiresBriefingFoodAssemblyReadingAndDelivery()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
  Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Diego"));yield return Talk(Npc("Mateo"));Assert.That(One<LetterManager>().MissionStarted,Is.False);
  yield return Talk(Npc("Diego"));Assert.That(One<EquipmentManager>().MissionStarted,Is.True);Equip();yield return null;Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Diego"));Assert.That(One<CampRoutine>().Eat(),Is.False);
  yield return Talk(Npc("Diego"));Assert.That(One<CampRoutine>().Briefed,Is.True);Assert.That(One<CampRoutine>().Eat(),Is.True);yield return Talk(Npc("Mateo"));Assert.That(One<LetterManager>().MissionStarted,Is.True);
  foreach(var f in Object.FindObjectsByType<LetterInteractableScript>(FindObjectsSortMode.None))if(f.name.StartsWith("MateoLetterFragment"))f.OnInspectionEnded();yield return null;
  var l=One<LetterManager>();l.CompleteMission();Assert.That(l.MissionCompleted,Is.False);Assert.That(One<RouteBoundary>().CanProceed,Is.False);
  var p=One<MainPresentation>();Assert.That(p.BeginLetterAssembly(),Is.True);yield return null;foreach(var piece in Object.FindObjectsByType<LetterPuzzlePiece>(FindObjectsSortMode.None)){piece.transform.position=p.Puzzle.Slot(piece.slotIndex).position;piece.rectTransform.localRotation=Quaternion.identity;Assert.That(p.Puzzle.TrySnap(piece),Is.True);}p.Puzzle.Close();yield return null;
  Assert.That(l.LetterAssembled,Is.True);l.CompleteMission();Assert.That(l.MissionCompleted,Is.False);
  var doc=(InspectableDocument)new SerializedObject(p).FindProperty("completedLetter").objectReferenceValue;doc.gameObject.SetActive(true);One<InspectionScript>().StartInspect(doc.gameObject);yield return Wait(.7f);Assert.That(One<DocumentReader>().IsVisible,Is.True);One<InspectionScript>().StopInspect();yield return null;yield return null;Assert.That(l.LetterRead,Is.True);
  yield return Talk(Npc("Mateo"));Assert.That(l.MissionCompleted,Is.True);Assert.That(One<RouteBoundary>().CanProceed,Is.True);
  var cp=One<CheckpointController>().Capture();Assert.That(cp.ate&&cp.briefed&&cp.letterRead,Is.True);Assert.That(cp.speakers.Any(x=>x.met),Is.True);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator MovingMapMarkersRemainAttachedAfterWalkingAndLooking()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;One<MapPickup>().Interact();One<MainPresentation>().Map.SetOpen(true,true);yield return null;yield return null;
  var overlay=One<MapZoneOverlay>();var camera=One<PhysicalMapViewCamera>().MapCamera;var reference=One<MapWorldReference>();
  for(int frame=0;frame<12;frame++){camera.transform.parent.rotation*=Quaternion.Euler(0,5,0);camera.transform.parent.position+=Vector3.right*.15f;yield return null;overlay.Refresh();foreach(var z in overlay.Zones){reference.TryWorldToMapPosition(z.location.position,out var position,out _);var actual=RectTransformUtility.WorldToScreenPoint(null,z.marker.position);var expected=(Vector2)camera.WorldToScreenPoint(position);var scale=z.marker.parent.lossyScale.x;Assert.That(Vector2.Distance(actual,expected+z.labelOffset*scale),Is.LessThan(2),z.name);}}
  yield return MainIntegrationTests.Capture("Library/ExperienceRevision-map.png",1280,720);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator CombatWaitsForKillsAndEndsAfterTheLastShadow()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Ready();AtFront();GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;
  Assert.That(One<PlayerWeaponController>().EquippedWeapon,Is.EqualTo("FAL"));Assert.That(Object.FindObjectsByType<AdvancingShadow>(FindObjectsSortMode.None).Length,Is.InRange(1,8));
  foreach(var a in Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))a.isStopped=true;
  // Pausing navigation, rather than time, verifies removal of the old 3–7 second expiry.
  foreach(var a in Object.FindObjectsByType<AdvancingShadow>(FindObjectsSortMode.None))a.enabled=false;
  yield return Wait(7.2f);Assert.That(One<CombatConclusion>().Ending,Is.False);Assert.That(One<ShadowSpawner>().Remaining,Is.EqualTo(12));
  var cc=One<CharacterController>();cc.enabled=false;cc.transform.position=new Vector3(56,cc.transform.position.y,520);cc.enabled=true;yield return null;Assert.That(cc.transform.position.z,Is.GreaterThanOrEqualTo(525),"The combat entrance must not allow retreat");
  float until=Time.unscaledTime+12;while(!One<CombatConclusion>().Ending&&Time.unscaledTime<until){foreach(var shadow in Object.FindObjectsByType<ShadowDamageTarget>(FindObjectsSortMode.None))shadow.ReceiveHit();yield return Wait(.7f);}
  Assert.That(One<CombatConclusion>().Reason,Is.EqualTo("cleared"));yield return Wait(2.4f);Assert.That(One<CombatConclusion>().Finished,Is.True);yield return MainIntegrationTests.Capture("Library/ExperienceRevision-ending.png",1280,720);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator TouchingPlayerEndsCombatAndOldExitCannotCutItShort()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;Ready();AtFront();GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;
  One<MainPresentation>().UnlockMap();One<MainPresentation>().Map.SetOpen(true,true);yield return null;
  var enemy=Object.FindObjectsByType<AdvancingShadow>(FindObjectsSortMode.None).First();var cc=One<CharacterController>();cc.enabled=false;cc.transform.position=enemy.transform.position;cc.enabled=true;Physics.SyncTransforms();yield return Wait(.3f);
  Assert.That(One<CombatConclusion>().Reason,Is.EqualTo("contact"));Assert.That(One<MainPresentation>().Map.IsOpen,Is.False);yield return Wait(2.5f);Assert.That(One<CombatConclusion>().Finished,Is.True);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator PropsRemainStableAndKnownNamesDoNotForgetAfterConversationChange()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Equip();var inspection=One<InspectionScript>();
  var helmet=Object.FindObjectsByType<EquipmentInteractableScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(n=>n.name=="Casco");helmet.gameObject.SetActive(true);var old=helmet.transform.position;inspection.StartInspect(helmet.gameObject);yield return null;Assert.That(helmet.GetComponentsInChildren<Renderer>().First().bounds.size.magnitude,Is.LessThan(1));inspection.StopInspect();Assert.That(Vector3.Distance(old,helmet.transform.position),Is.LessThan(.001));
  var ammo=One<AmmunitionPickup>();int before=One<PlayerWeaponController>().ReserveRounds;ammo.Interact();Assert.That(ammo.Collected,Is.True);Assert.That(One<PlayerWeaponController>().ReserveRounds,Is.EqualTo(before+40));
  var speaker=Npc("Simon");yield return Talk(speaker);Assert.That(speaker.HasMet,Is.True);speaker.SetConversation(ScriptableObject.CreateInstance<ConversationSO>());Assert.That(speaker.HasMet,Is.True);
  var checkpoint=One<CheckpointController>().Capture();speaker.RestoreKnown(false);Assert.That(One<CheckpointController>().Restore(checkpoint),Is.True);Assert.That(speaker.HasMet,Is.True,"Known names must survive checkpoint restoration");
  Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c&&c.GetType().Name=="LevelEndTrigger").All(c=>!c.enabled),Is.True);yield return new ExitPlayMode();
 }
 [Test]public void AuthoredAssetsCompileAndFiresAreBlocked()
 {
  EditorSceneManager.OpenScene(Scene);var shader=Shader.Find("StoryTell/ColdSea");Assert.That(shader,Is.Not.Null);Assert.That(ShaderUtil.ShaderHasError(shader),Is.False);
  foreach(var fire in Object.FindObjectsByType<CampfireMotion>(FindObjectsSortMode.None))Assert.That(fire.transform.Find("Borde de fogata").GetComponent<CapsuleCollider>().height,Is.GreaterThan(2));
  Assert.That(Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(11));
 }
 [UnityTest]public IEnumerator CaptureKitchenEquipmentAndSeaInRuntime()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;One<EquipmentManager>().StartMission();
  var go=new GameObject("Revision visual check",typeof(Camera));var camera=go.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=62;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
  var terrain=One<Terrain>();Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
  var meal=One<CampMeal>().transform.position;
  var positions=new[]{Ground(87,279)+Vector3.up*1.7f,Ground(meal.x-3,meal.z-3)+Vector3.up*1.7f,new Vector3(-15,8,285)};var targets=new[]{Ground(93,278)+Vector3.up*.8f,meal,new Vector3(-80,0,300)};
  var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
  try{for(int i=0;i<positions.Length;i++){camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);yield return Wait(.2f);RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes("Library/ExperienceRevision-world-"+i+".png",pixels.EncodeToPNG());Object.Destroy(pixels);}}finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(go);}yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator KitchenAndEquipmentHaveUnobstructedApproaches()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Physics.SyncTransforms();var terrain=One<Terrain>();
  foreach(var p in new[]{One<CampMeal>().transform.position,new Vector3(93,0,278)})
  {
   var foot=new Vector3(p.x,terrain.SampleHeight(new Vector3(p.x,0,p.z-1.8f))+terrain.transform.position.y,p.z-1.8f);var blocks=Physics.OverlapCapsule(foot+Vector3.up*.5f,foot+Vector3.up*1.5f,.35f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>!(c is TerrainCollider)&&!c.GetComponentInParent<SpeakerScript>()).ToArray();Assert.That(blocks,Is.Empty,string.Join(",",blocks.Select(c=>c.name)));
   var hits=Physics.RaycastAll(foot+Vector3.up*1.65f,(p-(foot+Vector3.up*1.65f)).normalized,Vector3.Distance(p,foot+Vector3.up*1.65f),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);Assert.That(hits.Any(h=>h.collider.name.StartsWith("Tent_")),Is.False,"Tent wall blocks the interaction");
  }
  yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator GameSelectActionsStayOnThePage()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/GameSelect.unity");yield return new EnterPlayMode();yield return null;yield return null;var doc=One<UIDocument>();var old=doc.panelSettings.targetTexture;var target=new RenderTexture(1280,720,24);target.Create();doc.panelSettings.targetTexture=target;yield return Wait(.3f);
  var root=doc.rootVisualElement;var page=root.Q(className:"notebook-page--right").worldBound;var buttons=root.Query<Button>().ToList().Where(b=>b.resolvedStyle.display!=DisplayStyle.None).ToArray();for(int i=0;i<buttons.Length;i++){var b=buttons[i].worldBound;Assert.That(b.xMin,Is.GreaterThanOrEqualTo(page.xMin-1),buttons[i].name);Assert.That(b.xMax,Is.LessThanOrEqualTo(page.xMax+1),buttons[i].name);Assert.That(b.yMax,Is.LessThanOrEqualTo(page.yMax+1),buttons[i].name);for(int j=0;j<i;j++)Assert.That(b.Overlaps(buttons[j].worldBound),Is.False,buttons[i].name+" overlaps "+buttons[j].name);}
  var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/ExperienceRevision-gameselect.png",image.EncodeToPNG());Object.Destroy(image);RenderTexture.active=previous;doc.panelSettings.targetTexture=old;target.Release();Object.Destroy(target);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator FalRaycastActuallyDefeatsAShadowCollider()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
  var weapon=One<PlayerWeaponController>();weapon.EquipWeapon("FAL");yield return null;var camera=Camera.main;camera.transform.position=new Vector3(3000,100,3000);
  var target=GameObject.CreatePrimitive(PrimitiveType.Capsule);target.name="Isolated shadow hit verification";target.transform.position=camera.transform.position+camera.transform.forward*4;target.AddComponent<ShadowDamageTarget>();Physics.SyncTransforms();
  Assert.That(weapon.TryShoot(),Is.True);yield return null;Assert.That((bool)target,Is.False,"The shot must hit the real collider and dissolve the shadow");yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator LegacyCheckpointWithOptionalLetterStillShowsHowToContinue()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
  Assert.That(One<CheckpointController>().Restore(new CheckpointData{equipmentStarted=true,equipmentMask=7,stage=RouteStage.Outpost}),Is.True);yield return null;yield return null;
  Assert.That(One<ObjectiveHud>().CurrentObjective,Does.Contain("Mateo"));Assert.That(One<CampRoutine>().Ate,Is.True);Assert.That(One<RouteBoundary>().CanProceed,Is.False);
  yield return Talk(Npc("Mateo"));Assert.That(One<LetterManager>().MissionStarted,Is.True);yield return new ExitPlayMode();
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
