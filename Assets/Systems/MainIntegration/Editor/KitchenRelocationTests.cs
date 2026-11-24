#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using NUnit.Framework;
using StoryTell.MainIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class KitchenRelocationTests
{
 [UnityTest]public IEnumerator UserKitchenIsPreservedAccessibleAndCommunicationsHasItsOwnMapLocation()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<15;i++)yield return null;
  var table=Object.FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Mesa de cocina");Assert.That(Vector3.Distance(table.transform.position,new Vector3(83.7f,27.800615f,253.5f)),Is.LessThan(.03f));Assert.That(Mathf.DeltaAngle(table.transform.eulerAngles.y,73.3f),Is.EqualTo(0).Within(.1f));
  var meal=Object.FindAnyObjectByType<CampMeal>();Assert.That(Vector2.Distance(new Vector2(meal.transform.position.x,meal.transform.position.z),new Vector2(table.bounds.center.x,table.bounds.center.z)),Is.LessThan(.01f));Assert.That(meal.GetComponent<Renderer>().bounds.min.y-table.bounds.max.y,Is.InRange(.005f,.04f));
  foreach(var name in new[]{"Marcos","Cocinero","Simon","Nestor"}){var actor=Object.FindObjectsByType<SpeakerScript>().Single(a=>a.name.Contains(name));Assert.That(Vector3.Distance(actor.transform.position,table.bounds.center),Is.LessThan(9),name);}
  var map=Object.FindAnyObjectByType<MapZoneOverlay>();Assert.That(map.Zones[4].location,Is.EqualTo(meal.transform));Assert.That(map.Zones.Last().name,Is.EqualTo("Comunicaciones"));Assert.That(map.Zones.Last().location.name,Is.EqualTo("Comunicaciones del campamento"));Assert.That(GameObject.Find("Radio de campaña - provisional"),Is.Not.Null);Assert.That(Object.FindObjectsByType<CampMeal>().Length,Is.EqualTo(1));
  var terrain=Object.FindAnyObjectByType<Terrain>();var target=meal.GetComponent<Renderer>().bounds.center;Physics.SyncTransforms();int approaches=0;
  for(int i=0;i<24;i++){float a=i*Mathf.PI/12;var foot=target+new Vector3(Mathf.Cos(a)*2,0,Mathf.Sin(a)*2);foot.y=terrain.SampleHeight(foot)+terrain.transform.position.y;if(Physics.OverlapCapsule(foot+Vector3.up*.4f,foot+Vector3.up*1.45f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)))continue;var eye=foot+Vector3.up*1.65f;var hit=Physics.RaycastAll(eye,(target-eye).normalized,Vector3.Distance(eye,target)+.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).FirstOrDefault();if(hit.collider&&hit.collider.GetComponentInParent<CampMeal>()==meal)approaches++;}
  Assert.That(approaches,Is.GreaterThanOrEqualTo(3));yield return Capture(new Vector3(88,terrain.SampleHeight(new Vector3(88,0,266))+terrain.transform.position.y+1.7f,266),target,"Library/KitchenRelocation-kitchen.png");yield return Capture(new Vector3(68,terrain.SampleHeight(new Vector3(68,0,291))+terrain.transform.position.y+1.7f,291),new Vector3(63,29,286.5f),"Library/KitchenRelocation-communications.png");yield return Capture(new Vector3(85,57,254),new Vector3(85,27.8f,254.01f),"Library/KitchenRelocation-layout.png");File.WriteAllText("Library/KitchenRelocation-verified.txt","Meal approaches: "+approaches+"\nPreserved user table transform; four soldiers nearby; eight map references; one meal.\n");yield return new ExitPlayMode();
 }
 static IEnumerator Capture(Vector3 from,Vector3 target,string path)
 {
  var go=new GameObject("Kitchen relocation verification",typeof(Camera));var camera=go.GetComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.cullingMask=~(1<<5);camera.transform.position=from;camera.transform.LookAt(target);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;yield return null;var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;
  try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
