#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DialogueSystem.Scripts;
using StoryTell.MainIntegration;
using System.Reflection;
public static class CommonDiningAuthoring
{
 const string ScenePath="Assets/Scenes/Level1.unity";
 static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static Terrain terrain;
 static Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
 static void Seat(GameObject g,float x,float z){Undo.RecordObject(g.transform,"Place common dining prop");var b=BoundsOf(g);var p=Ground(x,z);g.transform.position+=new Vector3(x-b.center.x,p.y+.015f-b.min.y,z-b.center.z);}
 static void MoveActor(SpeakerScript actor,float x,float z,Vector3 target)
 {
  Undo.RecordObject(actor.transform,"Arrange common dining soldiers");actor.transform.position=Ground(x,z);var look=target-actor.transform.position;look.y=0;actor.transform.rotation=Quaternion.LookRotation(look);
  var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);float gap=mesh.vertices.Min(v=>{var p=boots.transform.position+boots.transform.rotation*v;return p.y-terrain.SampleHeight(p)-terrain.transform.position.y;});UnityEngine.Object.DestroyImmediate(mesh);actor.transform.position+=Vector3.up*(.06f-gap);
 }
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
  var s=EditorSceneManager.OpenScene(ScenePath);if(All<Transform>(s).Any(t=>t.name=="Comedor común"))throw new InvalidOperationException("Already applied");terrain=All<Terrain>(s).Single();
  string backup="Library/CommonDining-backup/";Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"Level1.unity",true);File.Copy(AssetDatabase.GetAssetPath(terrain.terrainData),backup+"Terrain.asset",true);
  foreach(var f in Directory.GetFiles("Assets/Fonts","*.asset",SearchOption.AllDirectories)){var dest=backup+f;Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(f,dest,true);}
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organize shared dining among new tents");
  var root=new GameObject("Comedor común");Undo.RegisterCreatedObjectUndo(root,"Common dining");
  var kitchen=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");var meal=All<CampMeal>(s).Single();var old=kitchen.bounds.center;
  Seat(kitchen.gameObject,65,285.5f);Undo.RecordObject(meal.transform,"Keep meal on its table");meal.transform.position+=kitchen.bounds.center-old;
  var shared=UnityEngine.Object.Instantiate(kitchen.gameObject,root.transform);Undo.RegisterCreatedObjectUndo(shared,"Common dining table");shared.name="Mesa del comedor común";Seat(shared,61,290);
  if(shared.GetComponentInChildren<CampMeal>(true))throw new InvalidOperationException("Unexpected meal copy");
  var fire=All<Transform>(s).Single(t=>t.name=="WestShelter_Fire");Undo.RecordObject(fire,"Move western campfire beside dining");fire.position=Ground(60,285.5f);
  var actors=All<SpeakerScript>(s);MoveActor(actors.Single(a=>a.name.Contains("Marcos")),66,289,Ground(63,287));MoveActor(actors.Single(a=>a.name=="NPC - Cocinero"),66.8f,284.2f,Ground(65,285.5f));MoveActor(actors.Single(a=>a.name=="NPC - Simon"),62,282.5f,Ground(60,285.5f));MoveActor(actors.Single(a=>a.name=="NPC - Nestor"),59,288,Ground(61,290));
  var map=All<MapZoneOverlay>(s).Single();var md=new SerializedObject(map);md.FindProperty("zones").GetArrayElementAtIndex(4).FindPropertyRelative("name").stringValue="Cocina / comedor";md.ApplyModifiedProperties();
  if(map.Zones[4].location!=meal.transform)throw new InvalidOperationException("Kitchen map reference changed unexpectedly");
  var scenic=new SerializedObject(All<ScenicFocus>(s).Single());var views=scenic.FindProperty("views");for(int i=0;i<views.arraySize;i++){var view=views.GetArrayElementAtIndex(i);if(view.FindPropertyRelative("target").objectReferenceValue!=meal.transform)continue;var entrance=(Transform)view.FindPropertyRelative("entrance").objectReferenceValue;Undo.RecordObject(entrance,"Move dining invitation");entrance.position=Ground(68,279);view.FindPropertyRelative("caption").stringValue="El comedor del campamento";}scenic.ApplyModifiedProperties();
  UpdateLine("Assets/Systems/MainIntegration/Data/CampExpansion/Nestor_2.asset","El comedor quedó entre estas carpas. Si no comiste, acercate a la mesa junto a Máximo.");
  UpdateLine("Assets/Systems/MainIntegration/Data/ExperienceRevision/AgustinCamp_1.asset","Si necesitás calentarte, el comedor está entre las carpas del sector oeste del campamento.");
  UpdateLine("Assets/Systems/MainIntegration/Data/ExperienceRevision/JoseHint2_0.asset","Revisá el cajón pequeño junto a la carpa de suministros. Está al costado del comedor.");
  UpdateLine("Assets/Systems/MainIntegration/Data/Polish/LetterHintLine2.asset","Revisá el cajón pequeño junto a la carpa de suministros. Está al costado del comedor.");
  var author=typeof(StoryTell.MainIntegration.Editor.CampExpansionAuthoring);author.GetField("terrain",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,terrain);author.GetMethod("ClearGrass",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new[]{kitchen.gameObject,shared,fire.gameObject}});
  foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
  var report=new StringBuilder("Common dining applied\n");report.AppendLine("Kitchen "+kitchen.bounds+" meal="+meal.transform.position);report.AppendLine("Shared table "+BoundsOf(shared));report.AppendLine("Fire "+fire.position);foreach(var a in actors.Where(a=>a.name.Contains("Marcos")||a.name.Contains("Cocinero")||a.name.Contains("Simon")||a.name.Contains("Nestor")))report.AppendLine(a.name+" "+a.transform.position);File.WriteAllText("Library/CommonDining-applied.txt",report.ToString());
 }
 static void UpdateLine(string path,string text){var line=AssetDatabase.LoadAssetAtPath<DialogueSO>(path);if(!line)throw new InvalidOperationException(path);var d=new SerializedObject(line);d.FindProperty("_dialogueText").stringValue=text;d.ApplyModifiedProperties();EditorUtility.SetDirty(line);}
 public static void ClearDiningGround()
 {
  var s=EditorSceneManager.OpenScene(ScenePath);terrain=All<Terrain>(s).Single();var data=terrain.terrainData;Undo.RegisterCompleteObjectUndo(data,"Clear only shared dining and its walking space");int removed=0;
  for(int layer=0;layer<data.detailPrototypes.Length;layer++)
  {
   var values=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);int count=0;
   for(int z=0;z<data.detailHeight;z++)for(int x=0;x<data.detailWidth;x++)
   {
    if(values[z,x]==0)continue;float wx=terrain.transform.position.x+(x+.5f)/data.detailWidth*data.size.x,wz=terrain.transform.position.z+(z+.5f)/data.detailHeight*data.size.z;
    float radius=Mathf.Sqrt(Mathf.Pow((wx-63)/7f,2)+Mathf.Pow((wz-286.5f)/7f,2));if(radius>=1.1f)continue;int density=Mathf.RoundToInt(values[z,x]*Mathf.InverseLerp(.9f,1.1f,radius));count+=values[z,x]-density;values[z,x]=density;
   }
   if(count>0){data.SetDetailLayer(0,0,layer,values);removed+=count;}
  }
  EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();File.WriteAllText("Library/CommonDining-ground.txt","Detail instances cleared in dining footprint, with soft border: "+removed);
 }
 public static void Survey()
 {
  var s=EditorSceneManager.OpenScene(ScenePath);var b=new StringBuilder();
  foreach(var r in All<Renderer>(s).Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.center.x>45&&r.bounds.center.x<74&&r.bounds.center.z>275&&r.bounds.center.z<303))b.AppendLine(r.name+" "+r.bounds);
  foreach(var t in All<Transform>(s).Where(t=>t.name.IndexOf("fire",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("fogata",StringComparison.OrdinalIgnoreCase)>=0))b.AppendLine("FIRE "+t.name+" "+t.position+" parent="+t.parent?.name);
  File.WriteAllText("Library/CommonDining-survey.txt",b.ToString());
 }
}
#endif
