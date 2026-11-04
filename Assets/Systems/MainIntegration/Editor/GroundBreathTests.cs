#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using DialogueSystem.Scripts;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using StoryTell.MainIntegration;
using InmersionSystem.Ambient.Effects;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Object=UnityEngine.Object;
public sealed class GroundBreathTests
{
 static IEnumerator Wait(float seconds){float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static float SoleGap(SpeakerScript actor)
 {
  var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);var terrain=One<Terrain>();float gap=mesh.vertices.Min(v=>{var p=boots.transform.position+boots.transform.rotation*v;return p.y-terrain.SampleHeight(p)-terrain.transform.position.y;});Object.Destroy(mesh);return gap;
 }
 [UnityTest]public IEnumerator VisibleBootsStayAboveGroundAndBodiesDoNotFallOrTip()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int frame=0;frame<8;frame++)yield return null;yield return Wait(.4f);
  var actors=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None);var positions=actors.Select(a=>a.transform.position).ToArray();var report=new StringBuilder();
  for(int sample=0;sample<3;sample++)
  {
   for(int i=0;i<actors.Length;i++)
   {
    var actor=actors[i];float gap=SoleGap(actor);var placement=actor.GetComponentInChildren<NpcFootPlacement>();var animator=placement.GetComponent<Animator>();string fields=string.Join(";",typeof(NpcFootPlacement).GetFields(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Select(f=>f.Name+"="+f.GetValue(placement)));report.AppendLine($"{sample} {actor.name} visibleGap={gap} root={actor.transform.position} initialized={animator.isInitialized} human={animator.isHuman} {fields}");File.WriteAllText("Library/GroundBreath-grounded-visible.txt",report.ToString());Assert.That(gap,Is.InRange(-.035f,.12f),report.ToString());Assert.That(placement.Calibrated,Is.True,report.ToString());
    foreach(var rb in actor.GetComponentsInChildren<Rigidbody>())Assert.That(rb.isKinematic&&!rb.useGravity,Is.True,actor.name);
    Assert.That(Vector2.Distance(new Vector2(positions[i].x,positions[i].z),new Vector2(actor.transform.position.x,actor.transform.position.z)),Is.LessThan(.005f),actor.name);
    if(sample==0)actor.GetComponent<NpcConversationFacing>().Begin(Camera.main.transform);if(sample==1)actor.GetComponent<NpcConversationFacing>().End();
   }
   yield return Wait(1.5f);
  }
  File.WriteAllText("Library/GroundBreath-grounded-visible.txt",report.ToString());yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator VaporHasOneSpacedCycleDriftsIndependentlyAndDoesNotCoverDocuments()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");Assert.That(ShaderUtil.ShaderHasError(Shader.Find("StoryTell/ColdBreathVapor")),Is.False);yield return new EnterPlayMode();yield return null;
  var player=Object.FindObjectsByType<ColdBreathEffect>(FindObjectsSortMode.None).Single(e=>e.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>());for(int i=0;i<20;i++)player.OnEffectStart();
  float end=Time.unscaledTime+7;while(player.ExhalationCount==0&&Time.unscaledTime<end)yield return null;Assert.That(player.ExhalationCount,Is.EqualTo(1));yield return Wait(.25f);
  var ps=player.Particles;Assert.That(ps.particleCount,Is.GreaterThan(1));Assert.That(ps.main.simulationSpace,Is.EqualTo(ParticleSystemSimulationSpace.World));Assert.That(player.Interval,Is.GreaterThanOrEqualTo(8));Capture(Camera.main,"Library/GroundBreath-player-vapor.png");
  var particles=new ParticleSystem.Particle[32];int count=ps.GetParticles(particles);Vector3 first=particles[0].position;var seed=particles[0].randomSeed;var emitter=ps.transform.position;ps.transform.position+=Vector3.right*3;ps.Simulate(.02f,false,false,false);count=ps.GetParticles(particles);bool found=false;for(int i=0;i<count;i++)if(particles[i].randomSeed==seed){Assert.That(Vector3.Distance(first,particles[i].position),Is.LessThan(.15f),"A vapor puff must remain in the air when its emitter moves");found=true;break;}Assert.That(found,Is.True);ps.transform.position=emitter;ps.Play(false);
  yield return Wait(2);Assert.That(ps.particleCount,Is.Zero);Assert.That(player.ExhalationCount,Is.EqualTo(1));
  var session=One<UiSessionCoordinator>();var owner=new GameObject("Reading ownership probe");Assert.That(session.TryAcquire(UiMode.Inspection,owner),Is.True);yield return Wait(7);Assert.That(player.ExhalationCount,Is.EqualTo(1));Assert.That(ps.particleCount,Is.Zero);session.Release(owner);Object.Destroy(owner);
  player.OnEffectStop();player.OnEffectStart();Assert.That(player.IsRunning,Is.True);yield return new ExitPlayMode();
 }
 [UnityTest]public IEnumerator EmittersAreAtMouthScaleAndTableObjectsRestOnTheirSupports()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return Wait(.6f);
  foreach(var effect in Object.FindObjectsByType<ColdBreathEffect>(FindObjectsSortMode.None)){Assert.That(effect.Particles,Is.Not.Null);Assert.That(effect.Particles.main.playOnAwake,Is.False);Assert.That(effect.Particles.main.maxParticles,Is.LessThanOrEqualTo(32));Assert.That(effect.Particles.transform.lossyScale.x,Is.EqualTo(1).Within(.01));Assert.That(effect.Particles.transform.localPosition.magnitude*Mathf.Abs(effect.Particles.transform.parent.lossyScale.x),Is.LessThan(.5));}
  var table=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Single(r=>r.name=="wooden_table (2)");foreach(var doc in Object.FindObjectsByType<Systems.InspectionSystem.Scripts.InspectableDocument>(FindObjectsSortMode.None).Where(d=>d.name.StartsWith("Diario1"))){float min=doc.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);Assert.That(min-table.bounds.max.y,Is.InRange(-.005f,.025f),doc.name);}
  var actor=Object.FindObjectsByType<SpeakerScript>(FindObjectsSortMode.None).Single(a=>a.name=="NPC - Cocinero");var camera=new GameObject("Grounded NPC preview",typeof(Camera)).GetComponent<Camera>();camera.transform.position=actor.transform.position+new Vector3(2,1.3f,-3);camera.transform.LookAt(actor.transform.position+Vector3.up*.9f);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;Capture(camera,"Library/GroundBreath-npc.png");Object.Destroy(camera.gameObject);yield return new ExitPlayMode();
 }
 static void Capture(Camera camera,string path)
 {
  var rt=new RenderTexture(1280,720,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);}finally{RenderTexture.active=old;rt.Release();Object.Destroy(rt);}
 }
 [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
