#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DialogueSystem.Scripts;
using StoryTell.MainIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
public static class KitchenRelocationAuthoring
{
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 const string Data="Assets/Systems/MainIntegration/Data/KitchenRelocation";
 static Terrain terrain;
 static Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Seat(GameObject g,Vector3 point){Undo.RecordObject(g.transform,"Place kitchen and communications props");var b=BoundsOf(g);g.transform.position+=new Vector3(point.x-b.center.x,point.y+.015f-b.min.y,point.z-b.center.z);}
 static void Actor(SpeakerScript a,Vector3 point,Vector3 target)
 {
  Undo.RecordObject(a.transform,"Move kitchen soldier");a.transform.position=point;var direction=target-point;direction.y=0;a.transform.rotation=Quaternion.LookRotation(direction);
  var boots=a.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);float gap=mesh.vertices.Min(v=>{var p=boots.transform.position+boots.transform.rotation*v;return p.y-terrain.SampleHeight(p)-terrain.transform.position.y;});UnityEngine.Object.DestroyImmediate(mesh);a.transform.position+=Vector3.up*(.06f-gap);
 }
 static GameObject New(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Relocate kitchen and block out communications");g.transform.SetParent(parent,false);return g;}
 static Material Material(string name,Color color,bool unlit=false){var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));m.color=color;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(m,Data+"/"+name+".mat");return m;}
 static GameObject Shape(string name,Transform root,PrimitiveType primitive,Vector3 point,Vector3 scale,Material material,bool collision=true)
 {
  var g=GameObject.CreatePrimitive(primitive);Undo.RegisterCreatedObjectUndo(g,"Provisional communications equipment");g.name=name;g.transform.SetParent(root,true);g.transform.position=point;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Undo.DestroyObjectImmediate(g.GetComponent<Collider>());return g;
 }
 static void Line(string path,string value)
 {
  var line=AssetDatabase.LoadAssetAtPath<DialogueSO>(path);if(!line)throw new InvalidOperationException(path);var backup="Library/KitchenRelocation-backup/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);var d=new SerializedObject(line);d.FindProperty("_dialogueText").stringValue=value;d.ApplyModifiedProperties();EditorUtility.SetDirty(line);
 }
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<Transform>(s).Any(t=>t.name=="Comunicaciones del campamento"))throw new InvalidOperationException("Already applied");
  terrain=All<Terrain>(s).Single();Directory.CreateDirectory(Data);AssetDatabase.Refresh();File.Copy(AssetDatabase.GetAssetPath(terrain.terrainData),"Library/KitchenRelocation-backup/Terrain.asset",true);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Follow the user placed kitchen table");
  var table=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");var anchor=table.bounds.center;var originalPosition=table.transform.position;var originalRotation=table.transform.rotation;var originalScale=table.transform.localScale;Vector3 P(float x,float z)=>Ground(anchor.x+x,anchor.z+z);
  var meal=All<CampMeal>(s).Single();Seat(meal.gameObject,new Vector3(anchor.x,table.bounds.max.y,anchor.z));
  var shared=All<MeshRenderer>(s).Single(r=>r.name=="Mesa del comedor común");var communications=New("Comunicaciones del campamento",null);communications.transform.position=Ground(63,286.5f);var desk=UnityEngine.Object.Instantiate(shared.gameObject,communications.transform);Undo.RegisterCreatedObjectUndo(desk,"Reuse existing table for radio");desk.name="Mesa de comunicaciones";Seat(desk,Ground(63,286.5f));Seat(shared.gameObject,P(1.3f,6.5f));
  var fire=All<Transform>(s).Single(t=>t.name=="WestShelter_Fire");Undo.RecordObject(fire,"Keep kitchen fire beside new dining area");fire.position=P(5.3f,2.5f);
  var actors=All<SpeakerScript>(s);Actor(actors.Single(a=>a.name.Contains("Marcos")),P(.8f,3.5f),P(0,0));Actor(actors.Single(a=>a.name=="NPC - Cocinero"),P(1,-2.1f),P(0,0));Actor(actors.Single(a=>a.name=="NPC - Simon"),P(4.1f,.4f),fire.position);Actor(actors.Single(a=>a.name=="NPC - Nestor"),P(3.8f,5.5f),shared.bounds.center);
  var green=Material("RadioMetal",new Color(.18f,.23f,.21f));var dark=Material("CableAndControls",new Color(.045f,.055f,.06f));var screen=Material("RadioDisplay",new Color(.25f,.48f,.39f),true);float top=desk.GetComponent<Renderer>().bounds.max.y;
  Shape("Radio de campaña - provisional",communications.transform,PrimitiveType.Cube,new Vector3(63,top+.19f,286.5f),new Vector3(.64f,.36f,.38f),green);
  Shape("Visor de radio",communications.transform,PrimitiveType.Cube,new Vector3(62.88f,top+.23f,286.699f),new Vector3(.24f,.1f,.014f),screen,false);
  for(int i=0;i<2;i++){var knob=Shape("Selector de radio "+i,communications.transform,PrimitiveType.Cylinder,new Vector3(63.12f+i*.1f,top+.14f,286.71f),new Vector3(.055f,.018f,.055f),dark,false);knob.transform.rotation=Quaternion.Euler(90,0,0);}
  var mastPoint=Ground(64.5f,286.5f);Shape("Antena de comunicaciones - provisional",communications.transform,PrimitiveType.Cylinder,mastPoint+Vector3.up*1.65f,new Vector3(.045f,1.65f,.045f),green);
  var wireStart=new Vector3(63,top+.4f,286.5f);var wireEnd=mastPoint+Vector3.up*2.8f;var wire=Shape("Cable de antena",communications.transform,PrimitiveType.Cube,(wireStart+wireEnd)*.5f,new Vector3(.012f,.012f,Vector3.Distance(wireStart,wireEnd)),dark,false);wire.transform.rotation=Quaternion.LookRotation(wireEnd-wireStart);
  for(int i=0;i<2;i++){var point=Ground(61.2f+i*.85f,288.4f);Shape("Batería de comunicaciones - provisional "+i,communications.transform,PrimitiveType.Cube,point+Vector3.up*.26f,new Vector3(.65f,.5f,.46f),green);Shape("Asa de batería "+i,communications.transform,PrimitiveType.Cube,point+Vector3.up*.54f,new Vector3(.28f,.045f,.055f),dark,false);}
  var map=All<MapZoneOverlay>(s).Single();var md=new SerializedObject(map);var zones=md.FindProperty("zones");int index=zones.arraySize;zones.arraySize++;var zone=zones.GetArrayElementAtIndex(index);var marker=UnityEngine.Object.Instantiate(map.Zones[0].marker,map.Zones[0].marker.parent);Undo.RegisterCreatedObjectUndo(marker.gameObject,"Communications map location");marker.name="Zone_"+(index+1);marker.GetComponentInChildren<TMP_Text>().text=(index+1).ToString();var leader=UnityEngine.Object.Instantiate(map.Zones[0].leader,map.Zones[0].leader.parent);Undo.RegisterCreatedObjectUndo(leader.gameObject,"Communications map leader");zone.FindPropertyRelative("name").stringValue="Comunicaciones";zone.FindPropertyRelative("location").objectReferenceValue=communications.transform;zone.FindPropertyRelative("marker").objectReferenceValue=marker;zone.FindPropertyRelative("leader").objectReferenceValue=leader;zone.FindPropertyRelative("labelOffset").vector2Value=new Vector2(-45,45);md.ApplyModifiedProperties();
  var scenic=new SerializedObject(All<ScenicFocus>(s).Single());var views=scenic.FindProperty("views");for(int i=0;i<views.arraySize;i++){var view=views.GetArrayElementAtIndex(i);if(view.FindPropertyRelative("target").objectReferenceValue!=meal.transform)continue;var entrance=(Transform)view.FindPropertyRelative("entrance").objectReferenceValue;Undo.RecordObject(entrance,"Move kitchen view invitation");entrance.position=P(1.3f,12.5f);view.FindPropertyRelative("caption").stringValue="El comedor del campamento";}scenic.ApplyModifiedProperties();
  Line("Assets/Systems/DialogueSystem/ScriptableObjects/Dialogues/Diego/Equipment/Completed.asset","Ya tenés el equipo. Andá al comedor del sector sur del campamento y comé en la mesa de Máximo.");Line("Assets/Systems/MainIntegration/Data/CampExpansion/Nestor_2.asset","El comedor está acá, junto a la mesa de Máximo. Todavía queda algo caliente.");Line("Assets/Systems/MainIntegration/Data/ExperienceRevision/AgustinCamp_1.asset","Si necesitás calentarte, el comedor está en el sector sur del campamento, junto a Máximo.");
  foreach(var provider in All<LetterHintProvider>(s)){var d=new SerializedObject(provider);var fragments=d.FindProperty("fragments");var hints=d.FindProperty("hints");for(int i=0;i<Mathf.Min(fragments.arraySize,hints.arraySize);i++){var fragment=(Component)fragments.GetArrayElementAtIndex(i).objectReferenceValue;var hint=(ConversationSO)hints.GetArrayElementAtIndex(i).objectReferenceValue;if(!fragment||!hint||hint.Dialogues.Length==0)continue;var point=fragment.transform.position;string text=null;if(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(63,280))<3)text="Vi un papel en el cajón bajo, al sur del puesto de comunicaciones. Revisá ese sector del campamento.";else if(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(70,291))<3)text="Revisá el cajón pequeño junto a la carpa de suministros, cerca del puesto de comunicaciones.";if(text!=null)Line(AssetDatabase.GetAssetPath(hint.Dialogues[0]),text);}}
  ClearGround(anchor.x+1.5f,anchor.z+3,8,8);if(table.transform.position!=originalPosition||table.transform.rotation!=originalRotation||table.transform.localScale!=originalScale)throw new InvalidOperationException("User placed table was changed");
  foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
  File.WriteAllText("Library/KitchenRelocation-applied.txt","Preserved kitchen table: "+originalPosition+" rotation="+originalRotation.eulerAngles+"\nMeal: "+meal.transform.position+"\nCommunications: "+communications.transform.position+"\nMap zones: "+map.Zones.Length);
 }
 static void ClearGround(float cx,float cz,float rx,float rz)
 {
  var d=terrain.terrainData;Undo.RegisterCompleteObjectUndo(d,"Clear only new kitchen footprint");for(int layer=0;layer<d.detailPrototypes.Length;layer++){var cells=d.GetDetailLayer(0,0,d.detailWidth,d.detailHeight,layer);bool changed=false;for(int z=0;z<d.detailHeight;z++)for(int x=0;x<d.detailWidth;x++){if(cells[z,x]==0)continue;float wx=terrain.transform.position.x+(x+.5f)/d.detailWidth*d.size.x,wz=terrain.transform.position.z+(z+.5f)/d.detailHeight*d.size.z;float r=Mathf.Sqrt(Mathf.Pow((wx-cx)/rx,2)+Mathf.Pow((wz-cz)/rz,2));if(r>=1.1f)continue;int value=Mathf.RoundToInt(cells[z,x]*Mathf.InverseLerp(.9f,1.1f,r));if(value!=cells[z,x]){cells[z,x]=value;changed=true;}}if(changed)d.SetDetailLayer(0,0,layer,cells);}EditorUtility.SetDirty(d);
 }
 public static void Survey()
 {
  var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var b=new StringBuilder();var kitchen=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");b.AppendLine("KITCHEN position="+kitchen.transform.position+" rotation="+kitchen.transform.eulerAngles+" bounds="+kitchen.bounds);
  foreach(var r in All<MeshRenderer>(s).Where(r=>Vector2.Distance(new Vector2(r.bounds.center.x,r.bounds.center.z),new Vector2(kitchen.bounds.center.x,kitchen.bounds.center.z))<14))b.AppendLine("NEAR "+r.name+" "+r.bounds);
  foreach(var a in All<SpeakerScript>(s))b.AppendLine("NPC "+a.name+" "+a.transform.position);
  foreach(var t in All<Transform>(s).Where(t=>t.name.EndsWith("_Fire")||t.name.Contains("Mesa")||t.name=="Comedor común"))b.AppendLine("PROP "+t.name+" "+t.position);
  File.WriteAllText("Library/KitchenRelocation-survey.txt",b.ToString());
 }
}
#endif
