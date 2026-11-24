#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using DialogueSystem.Scripts;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class CommonDiningTests
{
 [UnityTest] public IEnumerator CommonDiningHasClearMealAccessAndCorrectMapReferences()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<15;i++)yield return null;
  var meal=Object.FindAnyObjectByType<CampMeal>();var terrain=Object.FindAnyObjectByType<Terrain>();var map=Object.FindAnyObjectByType<MapZoneOverlay>();Assert.That(map.Zones[4].location,Is.EqualTo(meal.transform));Assert.That(map.Zones[4].name,Does.Contain("comedor"));
  var mateo=Object.FindObjectsByType<SpeakerScript>().Single(s=>s.name.Contains("Marcos"));Assert.That(map.Zones[1].location,Is.EqualTo(mateo.transform));Assert.That(Vector3.Distance(mateo.transform.position,meal.transform.position),Is.LessThan(6));
  Assert.That(GameObject.Find("Mesa del comedor común"),Is.Not.Null);var target=meal.GetComponent<Renderer>().bounds.center;int approaches=0;Physics.SyncTransforms();
  for(int i=0;i<24;i++){float a=i*Mathf.PI/12;var foot=target+new Vector3(Mathf.Cos(a)*2,0,Mathf.Sin(a)*2);foot.y=terrain.SampleHeight(foot)+terrain.transform.position.y;var blocked=Physics.OverlapCapsule(foot+Vector3.up*.4f,foot+Vector3.up*1.45f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider));if(blocked)continue;var eye=foot+Vector3.up*1.65f;var hit=Physics.RaycastAll(eye,(target-eye).normalized,Vector3.Distance(eye,target)+.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).FirstOrDefault();if(hit.collider&&hit.collider.GetComponentInParent<CampMeal>()==meal)approaches++;}
  Assert.That(approaches,Is.GreaterThanOrEqualTo(3),"Meal must be reachable from several standing positions");
  var routine=Object.FindAnyObjectByType<CampRoutine>();routine.Restore(true,false);meal.Interact();Assert.That(routine.Ate,Is.True);Assert.That(routine.Eat(),Is.False);Assert.That(routine.Ate,Is.True);
  var from=new Vector3(69,0,279);from.y=terrain.SampleHeight(from)+terrain.transform.position.y+1.7f;yield return Capture(from,new Vector3(63,29,287),"Library/CommonDining-player.png");yield return Capture(new Vector3(62,55,286),new Vector3(62,27.8f,286.01f),"Library/CommonDining-layout.png");
  File.WriteAllText("Library/CommonDining-verified.txt","Meal standing approaches: "+approaches+"\nMap references follow meal and Mateo. Eating marks Ate and does not reset it.\n");yield return new ExitPlayMode();
 }
 static IEnumerator Capture(Vector3 from,Vector3 target,string path)
 {
  var go=new GameObject("Dining verification",typeof(Camera));var c=go.GetComponent<Camera>();c.fieldOfView=65;c.nearClipPlane=.05f;c.cullingMask=~(1<<5);c.transform.position=from;c.transform.LookAt(target);c.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return null;var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;
  try{RenderPipeline.SubmitRenderRequest(c,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
