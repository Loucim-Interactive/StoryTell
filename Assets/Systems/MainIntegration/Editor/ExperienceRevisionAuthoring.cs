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
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InteractionSystem.Scripts.Utils;
using Systems.Missions.FindObjects;
using Systems.InspectionSystem.Scripts;
using Systems.WeaponSystem;
using Systems.BattleSystem;
using StoryTell.PhysicalMap;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEditor.Events;
using UnityEngine.Events;
namespace StoryTell.MainIntegration.Editor
{
 public static class ExperienceRevisionAuthoring
 {
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  const string Data="Assets/Systems/MainIntegration/Data/ExperienceRevision";
  static Terrain terrain;
  static Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
  static void Set(UnityEngine.Object o,string field,UnityEngine.Object v){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=v;s.ApplyModifiedProperties();}
  static void Flag(UnityEngine.Object o,string field,bool v){var s=new SerializedObject(o);s.FindProperty(field).boolValue=v;s.ApplyModifiedProperties();}
  static void Num(UnityEngine.Object o,string field,float v){var s=new SerializedObject(o);s.FindProperty(field).floatValue=v;s.ApplyModifiedProperties();}
  static GameObject New(string name,Transform parent=null,bool ui=false){var g=ui?new GameObject(name,typeof(RectTransform)):new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Revise player experience");g.transform.SetParent(parent,false);if(ui)g.layer=5;return g;}
  static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static void Seat(GameObject g,Vector3 p){Undo.RecordObject(g.transform,"Seat object");g.transform.position=p;g.transform.position+=Vector3.up*(p.y-BoundsOf(g).min.y+.015f);}
  static void Box(RectTransform r,Vector2 anchor,Vector2 position,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;}
  static TMP_Text Text(string name,Transform parent,string text,Vector2 anchor,Vector2 position,Vector2 size){var t=New(name,parent,true).AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");t.fontSize=23;t.text=text;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;Box(t.rectTransform,anchor,position,size);return t;}
  static ConversationSO Conversation(string id,ESpeakers speaker,params string[] lines)
  {
   var existing=AssetDatabase.LoadAssetAtPath<ConversationSO>(Data+"/"+id+".asset");if(existing)return existing;
   var c=ScriptableObject.CreateInstance<ConversationSO>();c.Dialogues=new DialogueSO[lines.Length];Flag(c,"_autoNext",false);
   for(int i=0;i<lines.Length;i++){var line=ScriptableObject.CreateInstance<DialogueSO>();var d=new SerializedObject(line);d.FindProperty("_speaker").enumValueIndex=(int)speaker;d.FindProperty("_dialogueText").stringValue=lines[i];d.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/"+id+"_"+i+".asset");c.Dialogues[i]=line;}
   AssetDatabase.CreateAsset(c,Data+"/"+id+".asset");return c;
  }
  static void Assign(SpeakerScript speaker,ConversationSO conversation){var d=new SerializedObject(speaker);var a=d.FindProperty("_conversationSos");a.arraySize=1;a.GetArrayElementAtIndex(0).objectReferenceValue=conversation;d.ApplyModifiedProperties();}
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(s.GetRootGameObjects().Any(g=>g.name=="ExperienceRevision"))throw new InvalidOperationException("Already applied");
   if(AssetDatabase.IsValidFolder(Data))AssetDatabase.DeleteAsset(Data);Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Revise camp route and combat");
   terrain=All<Terrain>(s).Single();var root=New("ExperienceRevision");var safe=All<CanvasSafeArea>(s).Single().transform;
   var equipment=All<EquipmentManager>(s).Single();var letter=All<LetterManager>(s).Single();var speakers=All<SpeakerScript>(s);var diego=speakers.Single(n=>n.name.Contains("Diego"));var mateo=speakers.Single(n=>n.name.Contains("Mateo"));var session=All<UiSessionCoordinator>(s).Single();var player=All<InteractionScript>(s).Single().transform;var weapon=All<PlayerWeaponController>(s).Single();var checkpoints=All<CheckpointController>(s).Single();
   var routine=root.AddComponent<CampRoutine>();Set(routine,"equipment",equipment);Set(routine,"letter",letter);Set(routine,"diego",diego);Set(routine,"mateo",mateo);Set(routine,"feedback",All<MissionAnnouncement>(s).Single());Set(routine,"beforeFood",Conversation("BeforeFood",ESpeakers.Mateo,"Primero andá a la cocina y comé algo. Después charlamos acá."));Set(mateo,"campRoutine",routine);Set(checkpoints,"campRoutine",routine);Set(All<ObjectiveHud>(s).Single(),"campRoutine",routine);Set(All<RouteBoundary>(s).Single(),"campRoutine",routine);Set(All<RouteBoundary>(s).Single(),"progress",All<LevelProgress>(s).Single());Set(All<BattleManager>(s).Single(),"campRoutine",routine);
   Assign(mateo,Conversation("MateoOffer",ESpeakers.Mateo,"Soy Mateo. ¿Pudiste comer algo caliente?","El viento me rompió una carta de mi vieja. Hay cuatro pedazos por los refugios y los cajones del campamento.","Preguntales a Simón y José. Cuando los tengas, juntalos y leela; después traémela."));
   var searching=Conversation("MateoSearching",ESpeakers.Mateo,"Los pedazos quedaron por los refugios y los suministros. Simón y José pueden orientarte.");Set(letter,"searchingConversation",searching);
   // The existing return conversation keeps its native mission-completion action.
   var eqData=new SerializedObject(equipment);var completed=(ConversationSO)eqData.FindProperty("completedConversation").objectReferenceValue;
   foreach(var line in completed.Dialogues){var d=new SerializedObject(line);d.FindProperty("_dialogueText").stringValue="Ya tenés el equipo. Andá a la cocina y comé algo caliente; Mateo está por ahí.";d.ApplyModifiedProperties();EditorUtility.SetDirty(line);}
   MoveEquipment(s,diego);
   var cook=BuildKitchen(s,root.transform,routine,mateo);
   ConfigureNames(s,safe,session);
   ConfigureDocuments(s,letter);
   ConfigureCombat(s,root.transform,safe,session,player,weapon);
   ConfigureMap(s,cook);
   ClearGrass(s);
   AddDamage(s,root.transform);
   BuildWater(s,root.transform);
   foreach(var end in All<MonoBehaviour>(s).Where(c=>c&&c.GetType().Name=="LevelEndTrigger")){Undo.RecordObject(end,"Battle owns the ending");end.enabled=false;}
   foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Inspect();
  }
  static void MoveEquipment(Scene s,SpeakerScript diego)
  {
   var table=All<MeshRenderer>(s).Single(r=>r.name=="wooden_table (3)");var old=table.bounds.center;var destination=Ground(93,278);Undo.RecordObject(table.transform,"Equipment beside Diego");table.transform.position+=new Vector3(destination.x-old.x,destination.y-(table.bounds.min.y),destination.z-old.z);
   var b=table.bounds;var equipment=All<EquipmentInteractableScript>(s);int i=0;
   foreach(var item in equipment){var p=new Vector3(b.center.x+(i-1)*.68f,b.max.y,b.center.z);Seat(item.gameObject,p);Flag(item,"equipAfterInspection",true);Flag(item,"inspectInteraction",true);item.UIInteraction.interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress;item.UIInteraction.label="Recoger";if(item.TryGetComponent<Rigidbody>(out var rb)&&rb){Undo.RecordObject(rb,"Stable equipment");rb.isKinematic=true;rb.useGravity=false;}EditorUtility.SetDirty(item);i++;}
   var map=All<MapPickup>(s).Single();Seat(map.gameObject,new Vector3(b.center.x,b.max.y,b.center.z+.55f));
   // Stabilize all inspectable props. Moving an inspected item back never resumes a stale falling velocity.
   foreach(var item in All<InteractableScript>(s)){Undo.RecordObject(item,"Spanish interaction labels");if(item.TryGetComponent<Rigidbody>(out var rb)&&rb){Undo.RecordObject(rb,"Stable props at rest");rb.isKinematic=true;rb.useGravity=false;}
    string n=item.name.ToLowerInvariant();if(n.Contains("jerrycan")||n.Contains("fuel canister"))item.interactableName="Bidón";if(n=="electronicbox")item.interactableName="Receptor de radio";if(item.interactableName=="Volume knob")item.interactableName="Volumen";if(item.interactableName=="Frequency")item.interactableName="Frecuencia";
    if(item.UIInteraction!=null){string label=item.UIInteraction.label;item.UIInteraction.label=label switch{"Check" or "Check out" or "Inspect" or "Observar"=>"Inspeccionar","Open"=>"Abrir","Tune radio" or "Tune"=>"Sintonizar","Dial up"=>"Ajustar",_=>label};string text=item.UIInteraction.characterDescription;item.UIInteraction.characterDescription=text switch{"This radio looks brand new.."=>"Una radio para escuchar las transmisiones.","That sound a bit better"=>"Ahora se escucha un poco mejor.","Is this good?"=>"Voy a buscar una señal más clara.","Just an old ammo box..."=>"Una caja de munición.",_=>text};}EditorUtility.SetDirty(item);}
   var jose=All<SpeakerScript>(s).Single(n=>n.name=="NPC - Jose");foreach(var item in jose.GetComponentsInChildren<InteractableScript>()){item.interactableName="José";EditorUtility.SetDirty(item);}
   foreach(var original in All<InteractableScript>(s).Where(n=>n.name=="MediumBox")){original.interactableName="Caja de munición";Flag(original,"subInteractions",false);Flag(original,"inspectInteraction",true);original.UIInteraction.label="Inspeccionar";EditorUtility.SetDirty(original);}
   // A dedicated pickup uses the existing box model without reparenting a physical supply crate.
   var source=All<InteractableScript>(s).First(n=>n.name=="MediumBox");var go=New("Munición junto a Diego",table.transform.parent);var sourceMesh=source.GetComponentInChildren<MeshFilter>();go.AddComponent<MeshFilter>().sharedMesh=sourceMesh.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=sourceMesh.GetComponent<Renderer>().sharedMaterials;go.transform.rotation=sourceMesh.transform.rotation;var size=BoundsOf(go).size;go.transform.localScale*=.48f/Mathf.Max(size.x,size.y,size.z);Seat(go,new Vector3(b.center.x+.7f,b.max.y,b.center.z+.55f));go.layer=equipment[0].gameObject.layer;go.AddComponent<BoxCollider>();var ammo=go.AddComponent<AmmunitionPickup>();ammo.interactableName="Munición para FAL";ammo.UIInteraction=new UIInteraction{label="Recoger",interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress};Flag(ammo,"subInteractions",false);Flag(ammo,"inspectInteraction",false);Flag(ammo,"focusInteraction",false);Flag(ammo,"stateDescription",false);Set(ammo,"weapon",All<PlayerWeaponController>(s).Single());Set(ammo,"feedback",All<MissionAnnouncement>(s).Single());Set(All<CheckpointController>(s).Single(),"ammunition",ammo);
  }
  static Transform BuildKitchen(Scene s,Transform root,CampRoutine routine,SpeakerScript mateo)
  {
   var source=All<MeshRenderer>(s).Single(r=>r.name=="wooden_table (1)");var table=UnityEngine.Object.Instantiate(source.gameObject,root);Undo.RegisterCreatedObjectUndo(table,"Kitchen table");table.name="Mesa de cocina";Seat(table,Ground(82,289));var bounds=BoundsOf(table);
   var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Undo.RegisterCreatedObjectUndo(plate,"Warm meal");plate.name="Ración caliente";plate.transform.SetParent(root,false);plate.transform.localScale=new Vector3(.34f,.025f,.34f);plate.transform.position=new Vector3(bounds.center.x,bounds.max.y+.025f,bounds.center.z);plate.layer=All<EquipmentInteractableScript>(s)[0].gameObject.layer;
   var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.26f,.22f,.13f);material.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(material,Data+"/Ration.mat");plate.GetComponent<Renderer>().sharedMaterial=material;
   var meal=plate.AddComponent<CampMeal>();meal.interactableName="Comida caliente";meal.UIInteraction=new UIInteraction{label="Comer",interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress};Flag(meal,"inspectInteraction",false);Flag(meal,"subInteractions",false);Flag(meal,"focusInteraction",false);Flag(meal,"stateDescription",false);Set(meal,"routine",routine);
   Seat(mateo.gameObject,Ground(80.5f,288));
   var sourceNpc=All<SpeakerScript>(s).Single(n=>n.name=="NPC - Simon");var cook=UnityEngine.Object.Instantiate(sourceNpc.gameObject,root);Undo.RegisterCreatedObjectUndo(cook,"Soldier at kitchen");cook.name="NPC - Cocinero";Seat(cook,Ground(84,289));foreach(var hint in cook.GetComponents<LetterHintProvider>())Undo.DestroyObjectImmediate(hint);var speaker=cook.GetComponent<SpeakerScript>();Assign(speaker,Conversation("Cook",ESpeakers.Maximo,"Soy Máximo. Acercate a la mesa, todavía queda una ración caliente.","Mateo está junto a la cocina. Venía cuidando una carta como si fuera un tesoro."));foreach(var item in cook.GetComponentsInChildren<InteractableScript>())item.interactableName="Máximo";
   var guard=UnityEngine.Object.Instantiate(sourceNpc.gameObject,root);Undo.RegisterCreatedObjectUndo(guard,"Soldier at outpost");guard.name="NPC - Centinela";Seat(guard,Ground(105,344));foreach(var hint in guard.GetComponents<LetterHintProvider>())Undo.DestroyObjectImmediate(hint);Assign(guard.GetComponent<SpeakerScript>(),Conversation("Guard",ESpeakers.Facundo,"Soy Facundo. El puesto está siguiendo el camino.","Mantenete por el paso entre los sacos. Afuera hay pozos y alambres que casi no se ven."));foreach(var item in guard.GetComponentsInChildren<InteractableScript>())item.interactableName="Facundo";
   return plate.transform;
  }
  static void ConfigureNames(Scene s,Transform safe,UiSessionCoordinator session)
  {
   var host=New("Nombres conocidos",safe,true);var area=(RectTransform)host.transform;area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=area.offsetMax=Vector2.zero;var names=host.AddComponent<KnownSoldierNames>();Set(names,"area",area);Set(names,"view",All<Camera>(s).Single(c=>c.CompareTag("MainCamera")));Set(names,"session",session);
   var data=new SerializedObject(names);var entries=data.FindProperty("soldiers");var speakers=All<SpeakerScript>(s);entries.arraySize=speakers.Length;
   for(int i=0;i<speakers.Length;i++){var speaker=speakers[i];var interaction=speaker.GetComponentInChildren<InteractableScript>();var name=interaction?interaction.interactableName:speaker.name;var label=Text("Nombre "+i,area,name,Vector2.one*.5f,Vector2.zero,new Vector2(240,40));label.color=new Color(1,.78f,.4f);label.gameObject.SetActive(false);var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("speaker").objectReferenceValue=speaker;e.FindPropertyRelative("label").objectReferenceValue=label;e.FindPropertyRelative("displayName").stringValue=name;var animator=speaker.GetComponentInChildren<Animator>();if(animator&&animator.isHuman)e.FindPropertyRelative("head").objectReferenceValue=animator.GetBoneTransform(HumanBodyBones.Head);var facing=speaker.GetComponent<NpcConversationFacing>();if(!facing)facing=Undo.AddComponent<NpcConversationFacing>(speaker.gameObject);Set(speaker,"conversationFacing",facing);}
   data.ApplyModifiedProperties();var cp=new SerializedObject(All<CheckpointController>(s).Single());var bindings=cp.FindProperty("speakers");bindings.arraySize=speakers.Length;for(int i=0;i<speakers.Length;i++){bindings.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue=speakers[i].name;bindings.GetArrayElementAtIndex(i).FindPropertyRelative("speaker").objectReferenceValue=speakers[i];}cp.ApplyModifiedProperties();
  }
  static void ConfigureDocuments(Scene s,LetterManager letter)
  {
   var presentation=All<MainPresentation>(s).Single();var completed=(InspectableDocument)new SerializedObject(presentation).FindProperty("completedLetter").objectReferenceValue;var reader=All<DocumentReader>(s).Single();Set(reader,"letter",letter);Set(reader,"completedLetter",completed);
   var ui=New("Controles de inspección",All<CanvasSafeArea>(s).Single().transform,true);var rect=(RectTransform)ui.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;var controls=ui.AddComponent<InspectionControls>();Set(controls,"inspection",All<InspectionScript>(s).Single());Set(controls,"label",Text("Ayuda",ui.transform,"",new Vector2(.5f,0),new Vector2(0,90),new Vector2(1200,52)));
   var fragments=All<LetterInteractableScript>(s).Where(f=>f.name.StartsWith("MateoLetterFragment")).OrderBy(f=>f.name).ToArray();
   for(int i=0;i<fragments.Length;i++){var support=All<Transform>(s).Single(t=>t.name=="LetterSupport_"+(i+1));var b=BoundsOf(support.gameObject);Seat(fragments[i].gameObject,new Vector3(b.center.x+(i%2==0?.23f:-.23f),b.max.y,b.center.z+.18f));fragments[i].transform.rotation=Quaternion.Euler(0,37+i*53,0);Seat(fragments[i].gameObject,new Vector3(b.center.x+(i%2==0?.23f:-.23f),b.max.y,b.center.z+.18f));
    var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);Undo.RegisterCreatedObjectUndo(prop,"Supply detail beside hidden letter");prop.name="Paquete junto al fragmento";prop.transform.SetParent(support,false);prop.transform.localPosition=new Vector3(-.12f,.6f,-.2f);prop.transform.localScale=new Vector3(.4f,.25f,.28f);prop.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/MainIntegration/Data/Polish/WetTimber.mat");Seat(prop,new Vector3(b.center.x-.1f,b.max.y,b.center.z-.12f));}
  }
  static void ConfigureMap(Scene s,Transform kitchen)
  {
   var overlay=All<MapZoneOverlay>(s).Single();var data=new SerializedObject(overlay);var zones=data.FindProperty("zones");zones.arraySize=5;var e=zones.GetArrayElementAtIndex(4);var template=zones.GetArrayElementAtIndex(0);var old=(RectTransform)template.FindPropertyRelative("marker").objectReferenceValue;var marker=UnityEngine.Object.Instantiate(old,old.parent);Undo.RegisterCreatedObjectUndo(marker.gameObject,"Kitchen map marker");marker.name="Zone_5";marker.GetComponentInChildren<TMP_Text>().text="5";var oldLine=(RectTransform)template.FindPropertyRelative("leader").objectReferenceValue;var leader=UnityEngine.Object.Instantiate(oldLine,oldLine.parent);Undo.RegisterCreatedObjectUndo(leader.gameObject,"Kitchen marker leader");e.FindPropertyRelative("name").stringValue="Cocina";e.FindPropertyRelative("location").objectReferenceValue=kitchen;e.FindPropertyRelative("marker").objectReferenceValue=marker;e.FindPropertyRelative("leader").objectReferenceValue=leader;e.FindPropertyRelative("labelOffset").vector2Value=new Vector2(40,32);data.ApplyModifiedProperties();
  }
  static void ConfigureCombat(Scene s,Transform root,Transform safe,UiSessionCoordinator session,Transform player,PlayerWeaponController weapon)
  {
   var canvas=New("Final de escena",safe,true);var rect=(RectTransform)canvas.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;var conclusion=canvas.AddComponent<CombatConclusion>();Set(conclusion,"session",session);Set(conclusion,"weapon",weapon);var dark=New("Negro",canvas.transform,true).AddComponent<Image>();dark.color=new Color(0,0,0,0);var dr=dark.rectTransform;dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;Set(conclusion,"blackout",dark);Set(conclusion,"title",Text("Fin",canvas.transform,"Finalización escena 1",Vector2.one*.5f,Vector2.zero,new Vector2(1100,100)));
   var d=new SerializedObject(conclusion);var hide=d.FindProperty("hideAtEnd");var nodes=safe.Cast<Transform>().Where(t=>t!=canvas.transform).Select(t=>t.gameObject).ToArray();hide.arraySize=nodes.Length;for(int i=0;i<nodes.Length;i++)hide.GetArrayElementAtIndex(i).objectReferenceValue=nodes[i];d.ApplyModifiedProperties();
   var spawner=All<ShadowSpawner>(s).Single();Set(spawner,"combatTarget",player);Set(spawner,"conclusion",conclusion);
   // Bake only the front and its existing obstacle colliders, leaving other navigation untouched.
   var bounds=new Bounds(Ground(65,557),new Vector3(110,120,100));var sources=new System.Collections.Generic.List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new System.Collections.Generic.List<NavMeshBuildMarkup>(),sources);sources.RemoveAll(a=>a.component is Collider c&&(c.isTrigger||c.transform.IsChildOf(player)||c.name.Contains("Route_")||c.name.Contains("MissionGate")));
   var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.4f;settings.agentHeight=1.8f;settings.agentClimb=.45f;settings.agentSlope=45;var nav=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!nav)throw new InvalidOperationException("Front navigation bake failed");AssetDatabase.CreateAsset(nav,Data+"/FrontNavigation.asset");Set(root.gameObject.AddComponent<CombatNavigation>(),"data",nav);
   var area=All<SpawnArea>(s).Single();Undo.RecordObject(area.transform,"Enemies enter from the front");area.transform.position=Ground(65,560);var a=new SerializedObject(area);a.FindProperty("sampleNavMesh").boolValue=true;a.FindProperty("bounds").boundsValue=new Bounds(Vector3.zero,new Vector3(24,20,20));a.ApplyModifiedProperties();
  }
  static void ClearGrass(Scene s)
  {
   var data=terrain.terrainData;
   if(!File.Exists("Library/ExperienceRevision-backup/Terrain.asset"))File.Copy(AssetDatabase.GetAssetPath(data),"Library/ExperienceRevision-backup/Terrain.asset");
   Undo.RegisterCompleteObjectUndo(data,"Clear only grass intersecting camp props");
   var areas=All<MeshRenderer>(s).Where(r=>r.name.ToLowerInvariant().Contains("box")||r.name.StartsWith("LetterSupport_")||r.name.StartsWith("wooden_table")||r.name=="Mesa de cocina").Select(r=>r.bounds).ToList();
   foreach(var fire in All<CampfireMotion>(s))
   {
    areas.Add(new Bounds(fire.transform.position,new Vector3(4,4,4)));
    var barriers=fire.transform.Cast<Transform>().Where(t=>t.name=="Borde de fogata").ToArray();
    foreach(var duplicate in barriers.Skip(1))Undo.DestroyObjectImmediate(duplicate.gameObject);
    if(barriers.Length>0)continue;
    var barrier=New("Borde de fogata",fire.transform);barrier.transform.position=fire.transform.position+Vector3.up*1.3f;var collider=barrier.AddComponent<CapsuleCollider>();collider.radius=.8f;collider.height=3;
   }
   int changes=0;for(int layer=0;layer<data.detailPrototypes.Length;layer++){var p=data.detailPrototypes[layer];string name=p.prototypeTexture?p.prototypeTexture.name:p.prototype?p.prototype.name:"";if(!name.ToLowerInvariant().Contains("grass"))continue;var values=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);for(int z=0;z<data.detailHeight;z++)for(int x=0;x<data.detailWidth;x++){var world=terrain.transform.position+new Vector3((x+.5f)/data.detailWidth*data.size.x,0,(z+.5f)/data.detailHeight*data.size.z);foreach(var b in areas){float hx=data.size.x/data.detailWidth*.5f,hz=data.size.z/data.detailHeight*.5f;if(world.x+hx>=b.min.x-.3f&&world.x-hx<=b.max.x+.3f&&world.z+hz>=b.min.z-.3f&&world.z-hz<=b.max.z+.3f){if(values[z,x]>0)changes++;values[z,x]=0;break;}}}data.SetDetailLayer(0,0,layer,values);}EditorUtility.SetDirty(data);File.WriteAllText("Library/ExperienceRevision-grass.txt","Cleared layer cells: "+changes);terrain.Flush();
  }
  static void AddDamage(Scene s,Transform root)
  {
   var source=All<MeshFilter>(s).Single(f=>f.name=="Collapsed shelter 0");int i=0;foreach(var p in new[]{new Vector2(81,439),new Vector2(26,490)}){var go=New("Refugio dañado adicional "+i,root);go.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;var mesh=UnityEngine.Object.Instantiate(source.sharedMesh);go.transform.position=Ground(p.x,p.y);go.transform.rotation=Quaternion.Euler(0,25+i*80,0);var vs=mesh.vertices;for(int v=0;v<vs.Length;v++){var world=go.transform.TransformPoint(vs[v]);var oldWorld=source.transform.TransformPoint(vs[v]);float height=oldWorld.y-(terrain.SampleHeight(oldWorld)+terrain.transform.position.y);vs[v].y=Ground(world.x,world.z).y-go.transform.position.y+Mathf.Max(.02f,height)*.85f;}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/DamagedShelter_"+i+".asset");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshCollider>().sharedMesh=mesh;i++;}
  }
  static void BuildWater(Scene s,Transform root)
  {
   var water=GameObject.CreatePrimitive(PrimitiveType.Plane);Undo.RegisterCreatedObjectUndo(water,"Cold sea horizon");water.name="Mar frío";water.transform.SetParent(root,false);water.transform.position=new Vector3(750,.5f,750);water.transform.localScale=new Vector3(650,1,650);Undo.DestroyObjectImmediate(water.GetComponent<Collider>());var material=new Material(Shader.Find("StoryTell/ColdSea"));AssetDatabase.CreateAsset(material,Data+"/ColdSea.mat");water.GetComponent<Renderer>().sharedMaterial=material;
  }
  public static void Refine()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(s).Single();var conclusion=All<CombatConclusion>(s).Single();Set(conclusion,"presentation",All<MainPresentation>(s).Single());Set(conclusion,"inspection",All<InspectionScript>(s).Single());Set(conclusion,"dialogue",All<Systems.DialogueSystem.Scripts.DialogueManagerScript>(s).Single());Set(conclusion,"radio",All<Systems.WalkieSystem.Scripts.WalkieTalkieScript>(s).Single());Set(conclusion,"pause",All<PauseMenu>(s).Single());Set(All<TriggerZone>(s).Single(),"campRoutine",All<CampRoutine>(s).Single());
   // Preserve the stable GlobalObjectIds used by checkpoints before this revision.
   var cp=new SerializedObject(All<CheckpointController>(s).Single());var bindings=cp.FindProperty("speakers");for(int i=0;i<bindings.arraySize;i++){var e=bindings.GetArrayElementAtIndex(i);var speaker=(SpeakerScript)e.FindPropertyRelative("speaker").objectReferenceValue;e.FindPropertyRelative("id").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(speaker).ToString();}cp.ApplyModifiedProperties();
   var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Systems/MainIntegration/Data/ColdStyle"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Where(m=>m&&m.HasProperty("_Wetness")).ToArray();
   foreach(var m in materials){Undo.RecordObject(m,"Distinct cold material response");string n=m.name.ToLowerInvariant();bool fabric=n.Contains("uniform")||n.Contains("tent")||n.Contains("canvas");m.SetFloat("_Wetness",fabric?.16f:.38f);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",fabric?.12f:n.Contains("metal")?.48f:.23f);if(m.HasProperty("_DetailStrength"))m.SetFloat("_DetailStrength",fabric?.035f:n.Contains("stone")?.12f:.075f);EditorUtility.SetDirty(m);}
   var cook=All<SpeakerScript>(s).Single(n=>n.name=="NPC - Cocinero");foreach(var item in cook.GetComponentsInChildren<InteractableScript>()){Undo.RecordObject(item,"Distinct soldier name");item.interactableName="Máximo";}var cookLine=AssetDatabase.LoadAssetAtPath<DialogueSO>(Data+"/Cook_0.asset");var cd=new SerializedObject(cookLine);cd.FindProperty("_speaker").enumValueIndex=(int)ESpeakers.Maximo;cd.FindProperty("_dialogueText").stringValue="Soy Máximo. Acercate a la mesa, todavía queda una ración caliente.";cd.ApplyModifiedProperties();EditorUtility.SetDirty(cookLine);
   var area=All<SpawnArea>(s).Single();Undo.RecordObject(area.transform,"Connected shadow approach");area.transform.position=Ground(55,545);var areaData=new SerializedObject(area);areaData.FindProperty("bounds").boundsValue=new Bounds(Vector3.zero,new Vector3(10,6,6));areaData.ApplyModifiedProperties();
   foreach(var oldSea in All<MeshRenderer>(s).Where(r=>r.name=="DistantSea_VisualOnly")){Undo.RecordObject(oldSea,"Use the new sea surface");oldSea.enabled=false;}
   foreach(var item in All<InteractableScript>(s).Where(i=>i.interactableName=="Interactable")){Undo.RecordObject(item,"Describe objects in Spanish");item.interactableName=item.name.Contains("Box")?"Caja de suministros":"Objeto del campamento";}
   var people=All<SpeakerScript>(s);
   void Give(string objectName,string id,ESpeakers speaker,params string[] lines){var npc=people.Single(n=>n.name==objectName);Assign(npc,Conversation(id,speaker,lines));}
   Give("NPC - Simon","SimonCamp",ESpeakers.Simon,"Soy Simón. Estoy ordenando lo que quedó junto a los refugios.","Si estás buscando algo, preguntame. Con este viento, los papeles terminan entre los cajones.");
   Give("NPC - Jose","JoseCamp",ESpeakers.Jose,"Soy José. Dejamos los suministros cerca de las carpas para que no se mojen.","Revisá los bordes de los cajones; a veces el viento deja los papeles pegados ahí.");
   Give("NPC - Agustin","AgustinCamp",ESpeakers.Agustin,"Diego está junto a la mesa del equipo. Hablá con él antes de salir.","Si necesitás calentarte, la cocina está del otro lado de la fogata del campamento.");
   Give("NPC - Pedro","PedroOutpost",ESpeakers.Pedro,"Soy Pedro. El paso al frente sigue entre los sacos de arena.","Escuchá la radio y mantenete cerca del camino. El terreno de los costados está lleno de pozos.");
   Give("NPC - Ruben","RubenOutpost",ESpeakers.Ruben,"Estamos tratando de mantener secos los abrigos. Parece que la humedad se mete por todos lados.");
   Give("NPC - Franco","FrancoOutpost",ESpeakers.Franco,"Desde acá se escucha más cerca. Seguí por el paso de los sacos; nosotros nos quedamos en el puesto.");
   var jose=people.Single(n=>n.name=="NPC - Jose");var hint=new SerializedObject(jose.GetComponent<LetterHintProvider>());var hints=hint.FindProperty("hints");for(int i=0;i<hints.arraySize;i++){var original=(ConversationSO)hints.GetArrayElementAtIndex(i).objectReferenceValue;hints.GetArrayElementAtIndex(i).objectReferenceValue=Conversation("JoseHint"+i,ESpeakers.Jose,original.Dialogues[0].DialogueText);}hint.ApplyModifiedProperties();
   var namesData=new SerializedObject(All<KnownSoldierNames>(s).Single());var names=namesData.FindProperty("soldiers");for(int i=0;i<names.arraySize;i++){var e=names.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("speaker").objectReferenceValue==cook)e.FindPropertyRelative("displayName").stringValue="Máximo";}namesData.ApplyModifiedProperties();
   foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();DiagnoseFront();
  }
  public static void DiagnoseFront()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(s).Single();var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(Data+"/FrontNavigation.asset");var instance=NavMesh.AddNavMeshData(nav);var report=new StringBuilder();var route=All<RouteBoundary>(s).Single();
   try{NavMesh.SamplePosition(Ground(56,531),out var entry,4,NavMesh.AllAreas);report.AppendLine("ENTRY "+Ground(56,531)+" NAV "+entry.position);for(float z=525;z<590;z+=5){for(float x=20;x<115;x+=5){var p=Ground(x,z);var path=new NavMeshPath();bool exists=NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas);bool connected=exists&&NavMesh.CalculatePath(entry.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;report.Append(connected?'C':exists?'x':'.');if(connected&&z>=535&&route.Contains(p))report.Append("["+x+","+z+"]");}report.AppendLine(" z="+z);}File.WriteAllText("Library/ExperienceRevision-navigation.txt",report.ToString());}finally{instance.Remove();}
  }
  public static void InspectCamp()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(s).Single();Physics.SyncTransforms();var report=new StringBuilder();
   foreach(var r in All<MeshRenderer>(s).Where(r=>r.name.StartsWith("Tent_")))report.AppendLine($"TENT {r.name} center={r.bounds.center} size={r.bounds.size}");
   for(float z=270;z<=310;z+=2){report.Append("z="+z+" ");for(float x=65;x<=107;x+=2){var p=Ground(x,z);var overlaps=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.38f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);bool blocked=overlaps.Any(c=>!(c is TerrainCollider)&&!c.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>());report.Append(blocked?'X':'.');}report.AppendLine();}File.WriteAllText("Library/ExperienceRevision-camp.txt",report.ToString());
  }
  public static void PlaceKitchen()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(s).Single();var meal=All<CampMeal>(s).Single();var table=All<Transform>(s).Single(t=>t.name=="Mesa de cocina");Seat(table.gameObject,Ground(92,295));Seat(meal.gameObject,new Vector3(92,BoundsOf(table.gameObject).max.y,295));Seat(All<SpeakerScript>(s).Single(n=>n.name.Contains("Mateo")).gameObject,Ground(90,295));Seat(All<SpeakerScript>(s).Single(n=>n.name=="NPC - Cocinero").gameObject,Ground(94,295));
   foreach(var provider in All<LetterHintProvider>(s)){var d=new SerializedObject(provider).FindProperty("hints");var c=(ConversationSO)d.GetArrayElementAtIndex(2).objectReferenceValue;var line=new SerializedObject(c.Dialogues[0]);line.FindProperty("_dialogueText").stringValue="Hay otro en el cajón entre la cocina y la fogata central. Revisá los bordes del paquete.";line.ApplyModifiedProperties();EditorUtility.SetDirty(c.Dialogues[0]);}
   foreach(var item in All<InteractableScript>(s).Where(i=>i.name=="Binoculares")){var box=item.GetComponent<BoxCollider>();var filter=item.GetComponentInChildren<MeshFilter>();if(box&&filter){var col=Undo.AddComponent<MeshCollider>(filter.gameObject);col.sharedMesh=filter.sharedMesh;col.convex=true;Undo.DestroyObjectImmediate(box);var d=new SerializedObject(item);var list=d.FindProperty("interactionColliders");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=col;d.ApplyModifiedProperties();}}
   ClearGrass(s);foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();InspectCamp();
  }
  public static void RecordApprovedLighting()
  {
   const string temporary="Assets/Systems/MainIntegration/Editor/RevisionLightingReference.unity";
   if(File.Exists(temporary))throw new InvalidOperationException("Temporary lighting reference already exists");
   try{File.Copy("Library/ExperienceRevision-backup/Level1.unity",temporary);AssetDatabase.ImportAsset(temporary);var original=EditorSceneManager.OpenScene(temporary);File.WriteAllText(Data+"/ApprovedLightingBeforeRevision.json",ExplorationPolishAuthoring.CaptureLighting(original));EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");}
   finally{AssetDatabase.DeleteAsset(temporary);}AssetDatabase.Refresh();
  }
  public static void FinalizeLayout(){PlaceKitchen();RecordApprovedLighting();}
  public static void Inspect()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var b=new StringBuilder();
   foreach(var c in All<InteractableScript>(s))b.AppendLine($"ITEM {PathOf(c.transform)} pos={c.transform.position} name={c.interactableName} inspect={c.InspectInteractable} description={(c.UIInteraction!=null?JsonUtility.ToJson(c.UIInteraction):"")} body={c.GetComponent<Rigidbody>()}");
   foreach(var c in All<SpeakerScript>(s))b.AppendLine($"NPC {PathOf(c.transform)} pos={c.transform.position} data={EditorJsonUtility.ToJson(c)}");
   foreach(var c in All<MonoBehaviour>(s).Where(c=>c&&(c.GetType().Name.Contains("Trigger")||c.GetType().Name.Contains("Scene")||c.GetType().Name.Contains("Induced"))))b.AppendLine($"SYSTEM {PathOf(c.transform)} {c.GetType().Name} data={EditorJsonUtility.ToJson(c)}");
   foreach(var c in All<Camera>(s))b.AppendLine($"CAMERA {PathOf(c.transform)} pos={c.transform.position} rot={c.transform.eulerAngles} data={EditorJsonUtility.ToJson(c)}");
   foreach(var c in All<MeshRenderer>(s).Where(c=>c.name.Contains("wooden_table")||c.name.Contains("Collapsed")||c.name.ToLowerInvariant().Contains("water")))b.AppendLine($"PROP {PathOf(c.transform)} center={c.bounds.center} size={c.bounds.size}");
   File.WriteAllText("Library/ExperienceRevision-inventory.txt",b.ToString());
  }
 }
}
#endif
