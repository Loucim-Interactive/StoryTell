#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.BattleSystem;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.Missions.FindObjects;
using Systems.DialogueSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using Object=UnityEngine.Object;
public sealed class InteractionReviewTests
{
 const string Scene="Assets/Scenes/Level1.unity";
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static IEnumerator Wait(float t){float until=Time.unscaledTime+t;while(Time.unscaledTime<until)yield return null;}
 static void Ready()
 {
  var equipment=One<EquipmentManager>();equipment.StartMission();var items=new SerializedObject(equipment).FindProperty("equipment");for(int i=0;i<items.arraySize;i++)((EquipmentInteractableScript)items.GetArrayElementAtIndex(i).objectReferenceValue).OnInspectionEnded();
  One<CampRoutine>().Restore(true,true);var letter=One<LetterManager>();letter.StartMission();foreach(var part in Object.FindObjectsByType<LetterInteractableScript>())if(part.name.StartsWith("MateoLetterFragment"))part.OnInspectionEnded();letter.MarkLetterAssembled();letter.MarkLetterRead();letter.CompleteMission();if(One<SupplyDispatch>())One<SupplyDispatch>().Restore(DispatchStage.Delivered);
 }
 static void Move(float x,float z)
 {
  var body=One<Systems.InteractionSystem.Scripts.InteractionScript>().GetComponent<CharacterController>();var terrain=One<Terrain>();body.enabled=false;body.transform.position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+.1f,z);body.transform.rotation=Quaternion.identity;body.enabled=true;Physics.SyncTransforms();
 }
 [Test]public void EveryNpcInteractionBelongsToItsActorAndJeremiasIsStanding()
 {
  EditorSceneManager.OpenScene(Scene);
  foreach(var item in Object.FindObjectsByType<NPCInteractableScript>())Assert.That(item.speaker,Is.SameAs(item.GetComponentInParent<SpeakerScript>()),item.name);
  var actor=Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name=="NPC");var interaction=actor.GetComponentInChildren<NPCInteractableScript>();Assert.That(interaction,Is.Not.Null);Assert.That(interaction.interactableName,Is.EqualTo("Jeremías"));
  foreach(var driver in actor.GetComponentsInChildren<NPCAnimatorScript>()){Assert.That(driver.IsAnimationLocked,Is.False);Assert.That(new SerializedObject(driver).FindProperty("crouching").boolValue,Is.False);}
  var data=new SerializedObject(actor).FindProperty("_conversationSos");var conversation=(ConversationSO)data.GetArrayElementAtIndex(0).objectReferenceValue;
  Assert.That(conversation.Dialogues.All(d=>d.SpeakerName==ESpeakers.Jeremias&&d.SubDialogueSO==null),Is.True);
 }
 [UnityTest]public IEnumerator HeldPropReallyZoomsAndRestoresItsWorldPlacement()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
  var prop=Object.FindObjectsByType<NPCInteractableScript>().First();prop.Interact();yield return null;Assert.That(One<DialogueManagerScript>().IsConversationOwnedBy(prop.speaker),Is.True);One<DialogueManagerScript>().CancelConversation();yield return null;
  One<EquipmentManager>().StartMission();var helmet=Object.FindObjectsByType<EquipmentInteractableScript>(FindObjectsInactive.Include).First(i=>i.name=="Casco");helmet.gameObject.SetActive(true);var position=helmet.transform.position;var scale=helmet.transform.localScale;var inspection=One<InspectionScript>();inspection.StartInspect(helmet.gameObject);yield return Wait(.2f);float before=helmet.transform.localScale.magnitude;inspection.AdjustMagnification(2);yield return Wait(.2f);Assert.That(inspection.Magnification,Is.EqualTo(1.24f).Within(.001));Assert.That(helmet.transform.localScale.magnitude,Is.GreaterThan(before*1.05f));inspection.AdjustMagnification(100);Assert.That(inspection.Magnification,Is.EqualTo(1.6f));inspection.StopInspect();Assert.That(Vector3.Distance(position,helmet.transform.position),Is.LessThan(.001f));Assert.That(Vector3.Distance(scale,helmet.transform.localScale),Is.LessThan(.001f));yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator LetterGuideMatchesThePaperAndSnapsWithoutPixelPerfectDragging()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;Ready();var presentation=One<MainPresentation>();Assert.That(presentation.BeginLetterAssembly(),Is.True);yield return null;
  var puzzle=presentation.Puzzle;var data=new SerializedObject(puzzle);var board=(RectTransform)data.FindProperty("board").objectReferenceValue;Assert.That(board.sizeDelta.x,Is.EqualTo(450));Assert.That(board.sizeDelta.y,Is.EqualTo(586.8f).Within(.01));var guides=data.FindProperty("guides");Assert.That(guides.arraySize,Is.EqualTo(4));
  puzzle.Select(Object.FindObjectsByType<LetterPuzzlePiece>()[0]);yield return MainIntegrationTests.Capture("Library/InteractionReview-letter-guide.png",1280,720);
  foreach(var piece in Object.FindObjectsByType<LetterPuzzlePiece>())
  {
   var guide=(RawImage)guides.GetArrayElementAtIndex(piece.slotIndex).objectReferenceValue;Assert.That(guide.raycastTarget,Is.False);Assert.That(guide.texture,Is.Not.Null);Assert.That(guide.rectTransform.sizeDelta,Is.EqualTo(puzzle.Slot(piece.slotIndex).sizeDelta));
   piece.transform.position=puzzle.Slot(piece.slotIndex).position;piece.rectTransform.localRotation=Quaternion.Euler(0,0,90);Assert.That(puzzle.TrySnap(piece),Is.False);
   piece.rectTransform.localRotation=Quaternion.identity;piece.transform.position=board.TransformPoint(board.InverseTransformPoint(puzzle.Slot(piece.slotIndex).position)+Vector3.right*50);Assert.That(puzzle.TrySnap(piece),Is.True);Assert.That(guide.gameObject.activeSelf,Is.False);
  }
  Assert.That(puzzle.Complete,Is.True);yield return MainIntegrationTests.Capture("Library/InteractionReview-letter.png",1280,720);puzzle.Close();yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator WalkingToTheOldExitCannotFinishAndWavesArriveInView()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Ready();Move(56,531);GameEventBus.Raise(GameplayEvents.OnBattleTriggered);yield return null;
  var spawner=One<ShadowSpawner>();var ending=One<CombatConclusion>();Assert.That(spawner.HasStarted,Is.True);Assert.That(spawner.ActiveCount,Is.InRange(1,8));
  foreach(var enemy in Object.FindObjectsByType<AdvancingShadow>()){Assert.That(spawner.IsAhead(enemy.transform.position),Is.True,enemy.transform.position.ToString());enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().isStopped=true;}
  ending.Finish(false);Assert.That(ending.Ending,Is.False);Move(56,570);yield return Wait(.3f);Assert.That(ending.Ending,Is.False);Move(56,531);
  foreach(var old in Object.FindObjectsByType<SceneSystem.Scripts.LevelEndTrigger>(FindObjectsInactive.Include)){old.EndLevel();Assert.That(old.enabled,Is.False);}
  foreach(var enemy in Object.FindObjectsByType<AdvancingShadow>())enemy.GetComponent<ShadowDamageTarget>().ReceiveHit();Move(56,552);yield return Wait(1.3f);Assert.That(spawner.ActiveCount,Is.GreaterThan(0),"Replacement wave must follow the advancing player, not remain at the old spawn behind them");
  float until=Time.unscaledTime+18;
  while(!ending.Ending&&Time.unscaledTime<until)
  {
   foreach(var enemy in Object.FindObjectsByType<AdvancingShadow>()){Assert.That(spawner.IsAhead(enemy.transform.position),Is.True);enemy.GetComponent<ShadowDamageTarget>().ReceiveHit();}
   yield return Wait(.3f);
  }
  Assert.That(ending.Reason,Is.EqualTo("cleared"));Assert.That(spawner.DefeatedCount,Is.EqualTo(spawner.TotalCount));yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator ScenicFocusCanBeCancelledAndDoesNotLockMovementOrDocuments()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;
  var scenic=One<ScenicFocus>();scenic.AutomaticEnabled=false;var views=new SerializedObject(scenic).FindProperty("views");var entrance=(Transform)views.GetArrayElementAtIndex(0).FindPropertyRelative("entrance").objectReferenceValue;var target=(Transform)views.GetArrayElementAtIndex(0).FindPropertyRelative("target").objectReferenceValue;Move(entrance.position.x,entrance.position.z);var view=new ScenicFocus.View{entrance=entrance,target=target};
  Assert.That(scenic.Begin(view),Is.True);Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));scenic.Cancel();Assert.That(scenic.IsFocusing,Is.False);
  var session=One<UiSessionCoordinator>();Assert.That(session.TryAcquire(UiMode.Puzzle,thisObject()),Is.True);Assert.That(scenic.Begin(view),Is.False);Assert.That(One<CampAmbientDialogue>().IsShowing,Is.False);session.Release(thisObject());yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator JeremiasOwnsHisDialogueAndNeighboursDoNotTurn()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();for(int i=0;i<8;i++)yield return null;
  var actor=Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name=="NPC");actor.GetComponentInChildren<NPCInteractableScript>().Interact();yield return Wait(.3f);
  Assert.That(One<DialogueManagerScript>().IsConversationOwnedBy(actor),Is.True);
  foreach(var neighbour in Object.FindObjectsByType<SpeakerScript>())if(neighbour.name=="NPC - Ruben"||neighbour.name=="NPC - Franco")Assert.That(neighbour.GetComponent<NpcConversationFacing>().IsFacingListener,Is.False);
  yield return CaptureActor(actor.transform);yield return MainIntegrationTests.Capture("Library/InteractionReview-jeremias-ui.png",1280,720);One<DialogueManagerScript>().CancelConversation();yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator NearbyRemarksAreSpacedAndYieldToModalUi()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;var ambient=One<CampAmbientDialogue>();var data=new SerializedObject(ambient);var voices=data.FindProperty("voices");var actor=(SpeakerScript)voices.GetArrayElementAtIndex(0).FindPropertyRelative("actor").objectReferenceValue;Move(actor.transform.position.x+2,actor.transform.position.z);float until=Time.unscaledTime+16;
  while(!ambient.IsShowing&&Time.unscaledTime<until)yield return null;Assert.That(ambient.IsShowing,Is.True);string line=ambient.CurrentLine;Assert.That(line,Does.Contain("«"));yield return Wait(2);Assert.That(ambient.CurrentLine,Is.EqualTo(line));
  var session=One<UiSessionCoordinator>();session.TryAcquire(UiMode.Puzzle,ambient);yield return null;yield return null;Assert.That(ambient.IsShowing,Is.False);session.Release(ambient);yield return Wait(.6f);Assert.That(ambient.IsShowing,Is.False);yield return new ExitPlayMode();
 }
 static Object thisObject()=>One<ScenicFocus>();
 [UnityTest]public IEnumerator AutomaticFocusIsBriefAndModalUiAlwaysReleasesIt()
 {
  EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;yield return null;Ready();var scenic=One<ScenicFocus>();
  var views=(ScenicFocus.View[])typeof(ScenicFocus).GetField("views",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(scenic);var view=views[0];Move(view.entrance.position.x,view.entrance.position.z);
  var player=One<Systems.InteractionSystem.Scripts.InteractionScript>().transform;player.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(view.target.position-player.position,Vector3.up));
  var look=One<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>();look.SetCameraPitch(0);float until=Time.unscaledTime+2;
  while(!view.visited&&Time.unscaledTime<until)yield return null;Assert.That(view.visited,Is.True,$"Automatic focus: player={player.position} entrance={view.entrance.position} distance={Vector3.Distance(player.position,view.entrance.position)} cameraEnabled={look.enabled} locked={look.IsInputLocked} mode={One<UiSessionCoordinator>().Mode} stage={One<LevelProgress>().Stage} angle={Vector3.Angle(Camera.main.transform.forward,view.target.position-Camera.main.transform.position)} velocity={player.GetComponent<CharacterController>().velocity} mouse={UnityEngine.InputSystem.Mouse.current?.delta.ReadValue()} automatic={scenic.AutomaticEnabled}");Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));yield return Wait(2.1f);Assert.That(scenic.IsFocusing,Is.False);Assert.That(look.IsInputLocked,Is.False);
  Assert.That(scenic.Begin(view),Is.True);var session=One<UiSessionCoordinator>();session.TryAcquire(UiMode.Puzzle,scenic);yield return null;yield return null;Assert.That(scenic.IsFocusing,Is.False);session.Release(scenic);Assert.That(look.IsInputLocked,Is.False);yield return new ExitPlayMode();
 }
 static IEnumerator CaptureActor(Transform actor)
 {
  var host=new GameObject("Actor visual verification",typeof(Camera));var camera=host.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=58;camera.cullingMask=~(1<<5);camera.transform.position=actor.position+new Vector3(1.8f,1.6f,-3.2f);camera.transform.LookAt(actor.position+Vector3.up*.9f);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return Wait(.3f);
  var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
  try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes("Library/InteractionReview-jeremias-world.png",pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(host);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
