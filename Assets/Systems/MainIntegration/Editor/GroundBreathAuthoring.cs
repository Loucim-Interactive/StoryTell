#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using InmersionSystem.Ambient.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using InmersionSystem.Ambient.Configs;
using Systems.InspectionSystem.Scripts;
namespace StoryTell.MainIntegration.Editor
{
 public static class GroundBreathAuthoring
 {
  const string Data="Assets/Systems/MainIntegration/Data/GroundBreath";
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  [Serializable]public sealed class Placement{public string id,path;public Vector3 before,after;}
  [Serializable]public sealed class Placements{public Placement[] items;}
  public static void RecordPlacementChanges()
  {
   const string temporary="Assets/Systems/MainIntegration/Editor/GroundBreathReference.unity";
   if(File.Exists(temporary))throw new InvalidOperationException("Reference scene already exists");
   System.Collections.Generic.Dictionary<string,Vector3> before;
   try{File.Copy("Library/GroundBreath-backup/Level1.unity",temporary);AssetDatabase.ImportAsset(temporary);var reference=EditorSceneManager.OpenScene(temporary);before=All<InteractableScript>(reference).ToDictionary(i=>PathOf(i.transform),i=>i.transform.position);var current=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var items=All<InteractableScript>(current).Where(i=>before.ContainsKey(PathOf(i.transform))&&Vector3.Distance(before[PathOf(i.transform)],i.transform.position)>.001f).Select(i=>new Placement{id=GlobalObjectId.GetGlobalObjectIdSlow(i.transform).ToString(),path=PathOf(i.transform),before=before[PathOf(i.transform)],after=i.transform.position}).ToArray();File.WriteAllText(Data+"/PlacementChanges.json",JsonUtility.ToJson(new Placements{items=items},true));}finally{AssetDatabase.DeleteAsset(temporary);}AssetDatabase.Refresh();
  }
  static void Set(UnityEngine.Object o,string field,UnityEngine.Object value){var d=new SerializedObject(o);d.FindProperty(field).objectReferenceValue=value;d.ApplyModifiedProperties();}
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Stable NPCs, supported props and natural breath");
   var report=new StringBuilder();
   foreach(var actor in All<SpeakerScript>(s))
   {
    foreach(var body in actor.GetComponentsInChildren<Rigidbody>(true)){Undo.RecordObject(body,"Keep authored NPC stable");body.isKinematic=true;body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeAll;}
    var animator=actor.GetComponentsInChildren<Animator>(true).Single(a=>a.enabled&&a.runtimeAnimatorController);Undo.RecordObject(animator,"Evaluate feet even outside view");animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
    var placement=animator.GetComponent<NpcFootPlacement>();Set(placement,"ground",All<Terrain>(s).Single());
    var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);float minimumGap=mesh.vertices.Min(v=>{var p=boots.transform.position+boots.transform.rotation*v;return p.y-All<Terrain>(s).Single().SampleHeight(p)-All<Terrain>(s).Single().transform.position.y;});UnityEngine.Object.DestroyImmediate(mesh);if(Mathf.Abs(minimumGap)<.8f){Undo.RecordObject(actor.transform,"Start the visible soles just above ground");actor.transform.position+=Vector3.up*(.06f-minimumGap);}
    report.AppendLine("Stabilized "+actor.name);
   }
   PlaceProps(s,report);ConfigureBreath(s,report);
   foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();File.WriteAllText("Library/GroundBreath-applied.txt",report.ToString());AssetDatabase.Refresh();
  }
  static void PlaceProps(Scene scene,StringBuilder report)
  {
   var terrain=All<Terrain>(scene).Single();var renderers=All<MeshRenderer>(scene);
   bool IsSupport(Renderer r)=>r.name.StartsWith("wooden_table")||r.name=="Mesa de cocina"||r.name.StartsWith("LetterSupport_")||System.Text.RegularExpressions.Regex.IsMatch(r.name,@"^(box\d+|ammo_box)$");
   var supports=renderers.Where(IsSupport).ToArray();
   foreach(var r in supports.Where(r=>r.transform.position.z<610&&r.transform.position.z>205&&!r.GetComponentInParent<InteractableScript>()))
   {
    var b=BoundsOf(r.gameObject);float floor=terrain.SampleHeight(b.center)+terrain.transform.position.y;
    foreach(var other in supports)if(other!=r&&!other.transform.IsChildOf(r.transform)&&other.bounds.Contains(new Vector3(b.center.x,other.bounds.center.y,b.center.z))&&other.bounds.max.y<=b.min.y+.03f)floor=Mathf.Max(floor,other.bounds.max.y);
    float gap=b.min.y-floor;if(gap>.065f&&gap<1.5f){Undo.RecordObject(r.transform,"Rest a floating supply prop on its support");r.transform.position-=Vector3.up*(gap-.008f);report.AppendLine($"Lowered prop {PathOf(r.transform)} by {gap-.008f:F3}m");}
   }
   int paper=0;
   foreach(var item in All<InteractableScript>(scene))
   {
    if(item.GetComponentInParent<SpeakerScript>()||item.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>()||item.GetComponentsInChildren<Renderer>(true).Length==0||item.name=="Carta_Reconstruida_Consulta")continue;
    var b=BoundsOf(item.gameObject);if(b.center.z<205||b.center.z>610||Mathf.Max(b.size.x,b.size.z)>2.5f)continue;
    float surface=terrain.SampleHeight(b.center)+terrain.transform.position.y;bool supported=false;
    foreach(var support in supports){if(support.transform.IsChildOf(item.transform))continue;var top=support.bounds;bool inside=b.center.x>=top.min.x-.025f&&b.center.x<=top.max.x+.025f&&b.center.z>=top.min.z-.025f&&b.center.z<=top.max.z+.025f;if(inside&&top.max.y<=b.center.y+.12f&&b.min.y-top.max.y<1){surface=Mathf.Max(surface,top.max.y);supported=true;}}
    float clearance=item.GetComponent<InspectableDocument>() ? .004f : .009f;if(item.name.StartsWith("Diario1"))clearance=.004f+paper++*.003f;
    float delta=surface+clearance-b.min.y;
    if((supported||Mathf.Abs(delta)<.4f)&&Mathf.Abs(delta)>.015f&&Mathf.Abs(delta)<.65f){Undo.RecordObject(item.transform,"Place inspectable on the visible support");item.transform.position+=Vector3.up*delta;report.AppendLine($"Seated {PathOf(item.transform)} delta={delta:F3}m");}
   }
  }
  static void ConfigureBreath(Scene scene,StringBuilder report)
  {
   var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"/BreathVapor.mat");if(!material){material=new Material(Shader.Find("StoryTell/ColdBreathVapor"));AssetDatabase.CreateAsset(material,Data+"/BreathVapor.mat");}
   foreach(var effect in All<ColdBreathEffect>(scene))
   {
    var isPlayer=effect.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>();var actor=effect.GetComponentInParent<SpeakerScript>();var data=new SerializedObject(effect);var existing=(ColdBreathConfig)data.FindProperty("config").objectReferenceValue;
    string id=isPlayer?"Player":actor.name.Replace(" ","_").Replace("-","");string path=Data+"/Breath_"+id+".asset";var config=AssetDatabase.LoadAssetAtPath<ColdBreathConfig>(path);if(!config){config=UnityEngine.Object.Instantiate(existing);AssetDatabase.CreateAsset(config,path);}Undo.RecordObject(config,"Less frequent, short exhalations");config.breathInterval=isPlayer?9:10;config.breathDuration=.45f;config.simulationSpeed=1;config.exhaleVolume=isPlayer ? .065f : .04f;EditorUtility.SetDirty(config);Set(effect,"config",config);
    var parent=isPlayer?All<Camera>(scene).Single(c=>c.CompareTag("MainCamera")).transform:actor.GetComponentsInChildren<Animator>().First(a=>a.enabled&&a.isHuman).GetBoneTransform(HumanBodyBones.Head);
    var particles=(ParticleSystem)data.FindProperty("breathParticles").objectReferenceValue;
    if(!particles){var go=new GameObject("Cold breath vapor");Undo.RegisterCreatedObjectUndo(go,"Breath particles");particles=go.AddComponent<ParticleSystem>();}
    Undo.SetTransformParent(particles.transform,parent,"Emit vapor from the mouth");particles.transform.position=parent.position+parent.rotation*(isPlayer?new Vector3(.015f,-.15f,.3f):new Vector3(0,.025f,.12f));particles.transform.rotation=parent.rotation;var scale=parent.lossyScale;particles.transform.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
    particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Undo.RecordObject(particles,"Moving, fading puffs instead of a flat image");var main=particles.main;main.playOnAwake=false;main.loop=true;main.duration=.5f;main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.2f);main.startSpeed=new ParticleSystem.MinMaxCurve(.18f,.35f);main.startSize=new ParticleSystem.MinMaxCurve(.055f,.09f);main.startColor=Color.white;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;main.simulationSpeed=1;main.scalingMode=ParticleSystemScalingMode.Shape;main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
    main.startSize3D=false;main.startRotation3D=false;var sheets=particles.textureSheetAnimation;sheets.enabled=false;var trails=particles.trails;trails.enabled=false;var collisions=particles.collision;collisions.enabled=false;
    var emission=particles.emission;emission.enabled=true;emission.rateOverTime=18;emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());var shape=particles.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.012f;shape.position=Vector3.zero;shape.rotation=Vector3.zero;shape.scale=Vector3.one;var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,3.2f));var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.18f),new GradientAlphaKey(.45f,.5f),new GradientAlphaKey(0,1)});color.color=gradient;
    var noise=particles.noise;noise.enabled=true;noise.strength=.06f;noise.frequency=1.2f;noise.scrollSpeed=.3f;noise.damping=true;var rotation=particles.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-.3f,.3f);var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.025f;velocity.y=.025f;velocity.z=.015f;
    var renderer=particles.GetComponent<ParticleSystemRenderer>();Undo.RecordObject(renderer,"Soft procedural vapor");renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    Set(effect,"breathParticles",particles);if(isPlayer)Set(effect,"session",All<UiSessionCoordinator>(scene).Single());report.AppendLine("Configured breath "+id);
   }
  }
  public static void Inspect()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(s).Single();var report=new StringBuilder();
   foreach(var actor in All<SpeakerScript>(s)){var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);float min=mesh.vertices.Min(v=>(boots.transform.position+boots.transform.rotation*v).y);report.AppendLine($"NPC {actor.name} root={actor.transform.position} bakedSole={min} ground={terrain.SampleHeight(actor.transform.position)+terrain.transform.position.y} bounds={boots.bounds.min.y}");UnityEngine.Object.DestroyImmediate(mesh);foreach(var rb in actor.GetComponentsInChildren<Rigidbody>(true))report.AppendLine($"NPCBODY {PathOf(rb.transform)} kinematic={rb.isKinematic} gravity={rb.useGravity} colliders={rb.GetComponents<Collider>().Length}");}
   foreach(var e in All<ColdBreathEffect>(s))report.AppendLine($"BREATH {PathOf(e.transform)} data={EditorJsonUtility.ToJson(e)}");
   foreach(var p in All<ParticleSystem>(s).Where(p=>PathOf(p.transform).ToLowerInvariant().Contains("breath")))report.AppendLine($"PARTICLE {PathOf(p.transform)} position={p.transform.position} scale={p.transform.lossyScale} material={AssetDatabase.GetAssetPath(p.GetComponent<ParticleSystemRenderer>().sharedMaterial)} main={JsonUtility.ToJson(p.main)}");
   foreach(var item in All<InteractableScript>(s)){if(item.GetComponentInParent<SpeakerScript>()||item.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>()||item.GetComponentsInChildren<Renderer>(true).Length==0)continue;var bounds=BoundsOf(item.gameObject);report.AppendLine($"ITEM {PathOf(item.transform)} pos={item.transform.position} min={bounds.min} max={bounds.max} terrain={terrain.SampleHeight(bounds.center)+terrain.transform.position.y}");}
   foreach(var r in All<MeshRenderer>(s).Where(r=>!r.GetComponentInParent<InteractableScript>()&&!r.GetComponentInParent<SpeakerScript>()&&(r.name.ToLowerInvariant().Contains("box")||r.name.ToLowerInvariant().Contains("barrel")||r.name.Contains("wooden_table")||r.name.StartsWith("LetterSupport_")||r.name=="Mesa de cocina")))report.AppendLine($"PROP {PathOf(r.transform)} min={r.bounds.min} max={r.bounds.max} terrain={terrain.SampleHeight(r.bounds.center)+terrain.transform.position.y}");
   File.WriteAllText("Library/GroundBreath-inventory.txt",report.ToString());
  }
 }
}
#endif
