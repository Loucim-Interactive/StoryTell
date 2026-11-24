#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.Missions.FindObjects;
using Systems.DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.WalkieSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class CommunicationsCrewTests
{
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static IEnumerator Wait(float t){float end=Time.unscaledTime+t;while(Time.unscaledTime<end)yield return null;}
 [UnityTest]public IEnumerator NewOperatorsHaveTheirOwnDialogueNamesSaveBindingsAndEquipment()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<10;i++)yield return null;
  var radio=One<WalkieInteractionMachine>();float end=Time.unscaledTime+40;while(radio&&!radio.IsFinished){Assert.That(Time.unscaledTime,Is.LessThan(end));if(radio.IsChoosing)radio.Resolve(0);yield return Wait(.1f);}
  bool equipmentStarted=One<EquipmentManager>().MissionStarted,letterStarted=One<LetterManager>().MissionStarted;
  foreach(var identity in new[]{ESpeakers.Luis,ESpeakers.Ernesto})
  {
   var actor=Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name=="NPC - "+identity);Assert.That(actor.GetComponentsInChildren<LetterHintProvider>().Length,Is.Zero);foreach(var item in actor.GetComponentsInChildren<NPCInteractableScript>()){Assert.That(item.speaker,Is.EqualTo(actor));Assert.That(item.interactableName,Is.EqualTo(identity.ToString()));}
   var conversations=new SerializedObject(actor).FindProperty("_conversationSos");var conversation=(ConversationSO)conversations.GetArrayElementAtIndex(0).objectReferenceValue;Assert.That(conversation.Dialogues.All(d=>d.SpeakerName==identity),Is.True);
   actor.Speak();yield return null;Assert.That(One<DialogueManagerScript>().IsConversationOwnedBy(actor),Is.True);end=Time.unscaledTime+25;while(One<DialogueManagerScript>().IsInConversation){Assert.That(Time.unscaledTime,Is.LessThan(end));One<DialogueManagerScript>().uiDialogueScript.RequestAdvance();yield return Wait(.2f);}yield return null;Assert.That(actor.HasMet,Is.True);Assert.That(actor.HasSpoken,Is.True);
   var names=new SerializedObject(One<KnownSoldierNames>()).FindProperty("soldiers");Assert.That(Enumerable.Range(0,names.arraySize).Count(i=>names.GetArrayElementAtIndex(i).FindPropertyRelative("speaker").objectReferenceValue==actor),Is.EqualTo(1));
   var voices=new SerializedObject(One<CampAmbientDialogue>()).FindProperty("voices");var matching=Enumerable.Range(0,voices.arraySize).Select(i=>voices.GetArrayElementAtIndex(i)).Single(v=>v.FindPropertyRelative("actor").objectReferenceValue==actor);Assert.That(matching.FindPropertyRelative("lines").arraySize,Is.EqualTo(5));
  }
  Assert.That(One<EquipmentManager>().MissionStarted,Is.EqualTo(equipmentStarted));Assert.That(One<LetterManager>().MissionStarted,Is.EqualTo(letterStarted));var checkpoint=One<CheckpointController>().Capture();Assert.That(checkpoint.IsValid,Is.True);Assert.That(checkpoint.speakers.Length,Is.EqualTo(17));Assert.That(checkpoint.speakers.Select(p=>p.id).Distinct().Count(),Is.EqualTo(17));Assert.That(checkpoint.speakers.Count(p=>p.met),Is.GreaterThanOrEqualTo(2));
  foreach(var name in new[]{"Libreta de mensajes - provisional","Auriculares de radio - provisionales","Bobina de cable - provisional","Receptor auxiliar"})Assert.That(GameObject.Find(name),Is.Not.Null,name);foreach(var name in new[]{"Bobina de cable - provisional","Receptor auxiliar"})Assert.That(GameObject.Find(name).GetComponentInChildren<Collider>(),Is.Not.Null);
  var terrain=One<Terrain>();var point=new Vector3(68,0,294);point.y=terrain.SampleHeight(point)+terrain.transform.position.y+1.7f;yield return Capture(point,new Vector3(63.5f,29,287),"Library/CommunicationsCrew-player.png");yield return new ExitPlayMode();
 }
 static IEnumerator Capture(Vector3 from,Vector3 target,string path)
 {
  var go=new GameObject("Communications verification",typeof(Camera));var c=go.GetComponent<Camera>();c.fieldOfView=65;c.nearClipPlane=.05f;c.cullingMask=~(1<<5);c.transform.position=from;c.transform.LookAt(target);c.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return null;var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;
  try{RenderPipeline.SubmitRenderRequest(c,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
