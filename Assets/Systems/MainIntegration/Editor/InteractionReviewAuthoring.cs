#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DialogueSystem.Scripts;
using Systems.BattleSystem;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.DialogueSystem.Scripts;
using UnityEngine.UI;
using UnityEngine.AI;
namespace StoryTell.MainIntegration.Editor
{
 public static class InteractionReviewAuthoring
 {
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  const string Data="Assets/Systems/MainIntegration/Data/InteractionReview";
  static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
  static GameObject New(string name,Transform parent=null,bool ui=false){var go=ui?new GameObject(name,typeof(RectTransform)):new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Clear interactions and combat");go.transform.SetParent(parent,false);if(ui)go.layer=5;return go;}
  static TMP_Text Text(string name,Transform parent,float y,int size=24){var t=New(name,parent,true).AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.94f,.95f,.96f);t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(1050,80);t.gameObject.SetActive(false);return t;}
  static void Save(Scene scene){foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
   if(All<CampAmbientDialogue>(s).Length>0)throw new InvalidOperationException("Already applied");
   Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Clear inspection, letter, NPC and combat interactions");
   var session=All<UiSessionCoordinator>(s).Single();var safe=All<CanvasSafeArea>(s).Single().transform;var look=All<CameraControllerScript>(s).Single();var player=look.transform;var progress=All<LevelProgress>(s).Single();var terrain=All<Terrain>(s).Single();
   var root=New("InteractionReview");
   foreach(var interaction in All<NPCInteractableScript>(s))
   {
    var owner=interaction.GetComponentInParent<SpeakerScript>();if(owner){Undo.RecordObject(interaction,"Use this NPC's conversation");interaction.speaker=owner;}
    var collider=interaction.GetComponent<BoxCollider>();if(collider){Undo.RecordObject(collider,"Distinct NPC interaction target");collider.size=new Vector3(.55f,1.8f,.55f);}
   }
   var actors=All<SpeakerScript>(s);
   foreach(var actor in actors)foreach(var driver in actor.GetComponentsInChildren<NPCAnimatorScript>(true)){Set(driver,"speaker",actor);Set(driver,"movementRoot",actor.transform);}
   var jeremias=actors.Single(a=>a.name=="NPC");
   foreach(var driver in jeremias.GetComponentsInChildren<NPCAnimatorScript>(true)){Undo.RecordObject(driver,"Stand beside the anti-tank guns");driver.UnlockAnimation();driver.SetCrouching(false);driver.SetArmed(false);}
   var conversation=ScriptableObject.CreateInstance<ConversationSO>();var cs=new SerializedObject(conversation);cs.FindProperty("_autoNext").boolValue=false;cs.ApplyModifiedProperties();
   string[] words={"Soy Jeremías. Estaba revisando los suministros de este puesto.","Rubén y Franco están acá al lado. Si necesitás ubicarte, hablá con cada uno."};conversation.Dialogues=new DialogueSO[words.Length];
   for(int i=0;i<words.Length;i++){var line=ScriptableObject.CreateInstance<DialogueSO>();var ls=new SerializedObject(line);ls.FindProperty("_speaker").enumValueIndex=(int)ESpeakers.Jeremias;ls.FindProperty("_dialogueText").stringValue=words[i];ls.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/Jeremias_"+i+".asset");conversation.Dialogues[i]=line;}
   AssetDatabase.CreateAsset(conversation,Data+"/Jeremias.asset");var actorData=new SerializedObject(jeremias);var convs=actorData.FindProperty("_conversationSos");convs.arraySize=1;convs.GetArrayElementAtIndex(0).objectReferenceValue=conversation;actorData.ApplyModifiedProperties();
   var source=actors.Single(a=>a.name=="NPC - Ruben").GetComponentInChildren<NPCInteractableScript>(true);var target=UnityEngine.Object.Instantiate(source.gameObject,jeremias.transform);Undo.RegisterCreatedObjectUndo(target,"Jeremías interaction");target.name="Interaction";target.transform.localPosition=source.transform.localPosition;var npc=target.GetComponent<NPCInteractableScript>();npc.speaker=jeremias;npc.interactableName="Jeremías";var targetBox=target.GetComponent<BoxCollider>();if(targetBox)targetBox.size=new Vector3(.55f,1.8f,.55f);
   var names=All<KnownSoldierNames>(s).Single();var ns=new SerializedObject(names);var soldiers=ns.FindProperty("soldiers");for(int i=0;i<soldiers.arraySize;i++){var entry=soldiers.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("speaker").objectReferenceValue==jeremias)entry.FindPropertyRelative("displayName").stringValue="Jeremías";}ns.ApplyModifiedProperties();
   var battle=All<ShadowSpawner>(s).Single();Set(All<CombatConclusion>(s).Single(),"battle",battle);
   foreach(var old in All<SceneSystem.Scripts.LevelEndTrigger>(s)){Undo.RecordObject(old,"Combat exclusively owns the ending");old.enabled=false;var so=new SerializedObject(old);so.FindProperty("triggerOnPlayerEnter").boolValue=false;so.ApplyModifiedProperties();}
   var puzzle=All<LetterAssemblyPuzzle>(s).Single();var ps=new SerializedObject(puzzle);var board=(RectTransform)ps.FindProperty("board").objectReferenceValue;Undo.RecordObject(board,"Exact paper-sized guide");board.sizeDelta=new Vector2(450,586.8f);var bg=board.GetComponent<Image>();if(bg){Undo.RecordObject(bg,"Subtle paper backing");bg.color=new Color(.11f,.13f,.14f,.45f);bg.raycastTarget=false;}
   var pieces=ps.FindProperty("pieces");var guides=ps.FindProperty("guides");guides.arraySize=pieces.arraySize;
   for(int i=0;i<pieces.arraySize;i++)
   {
    var piece=(LetterPuzzlePiece)pieces.GetArrayElementAtIndex(i).objectReferenceValue;var slot=(RectTransform)ps.FindProperty("slots").GetArrayElementAtIndex(piece.slotIndex).objectReferenceValue;
    foreach(var graphic in slot.GetComponents<Graphic>()){Undo.RecordObject(graphic,"No misleading rectangular slots");graphic.enabled=false;}
    var ghost=New("Paper guide "+i,board,true).AddComponent<RawImage>();ghost.raycastTarget=false;ghost.color=new Color(.86f,.9f,.91f,.07f);var gr=ghost.rectTransform;gr.anchorMin=slot.anchorMin;gr.anchorMax=slot.anchorMax;gr.pivot=slot.pivot;gr.sizeDelta=slot.sizeDelta;gr.anchoredPosition=slot.anchoredPosition;
    var pd=new SerializedObject(piece);ghost.texture=(Texture)pd.FindProperty("artworkTexture").objectReferenceValue;var mesh=(Mesh)pd.FindProperty("artworkMesh").objectReferenceValue;var uv=mesh.uv;ghost.uvRect=Rect.MinMaxRect(uv.Min(v=>v.x),uv.Min(v=>v.y),uv.Max(v=>v.x),uv.Max(v=>v.y));gr.SetAsFirstSibling();guides.GetArrayElementAtIndex(piece.slotIndex).objectReferenceValue=ghost;
   }
   ps.ApplyModifiedProperties();
   var subtitle=Text("Conversaciones cercanas",safe,178);var ambient=root.AddComponent<CampAmbientDialogue>();Set(ambient,"player",player);Set(ambient,"session",session);Set(ambient,"progress",progress);Set(ambient,"dialogue",All<DialogueManagerScript>(s).Single());Set(ambient,"subtitle",subtitle);
   var voiceActors=actors.Where(a=>a.name=="NPC - Simon"||a.name=="NPC - Jose"||a.name=="NPC - Cocinero"||a.name=="NPC - Agustin"||a==jeremias).ToArray();
   string[][] phrases={new[]{"No siento los dedos. Voy a acercarme un rato al fuego.","Mi hermano debe estar volviendo de la escuela a esta hora."},new[]{"Anoche el viento no nos dejó dormir.","Guardá bien las cartas. La humedad se mete por todos lados."},new[]{"Una comida caliente cambia un poco el día.","En casa siempre dejábamos un lugar más en la mesa."},new[]{"Cuando vuelva quiero sentarme a tomar mate con mi viejo.","¿Te queda algo seco? Se me mojaron las medias."},new[]{"Qué frío. Hoy extraño hasta el ruido de mi casa.","Ojalá les haya llegado la carta que mandé."}};
   var ads=new SerializedObject(ambient);var voices=ads.FindProperty("voices");voices.arraySize=voiceActors.Length;
   for(int i=0;i<voiceActors.Length;i++){var v=voices.GetArrayElementAtIndex(i);var a=voiceActors[i];v.FindPropertyRelative("actor").objectReferenceValue=a;var interaction=a.GetComponentInChildren<NPCInteractableScript>();v.FindPropertyRelative("displayName").stringValue=interaction?interaction.interactableName:a.name;var lines=v.FindPropertyRelative("lines");lines.arraySize=phrases[i].Length;for(int k=0;k<phrases[i].Length;k++)lines.GetArrayElementAtIndex(k).stringValue=phrases[i][k];}ads.ApplyModifiedProperties();
   var scenic=root.AddComponent<ScenicFocus>();Set(scenic,"player",player);Set(scenic,"viewCamera",CameraFrom(s));Set(scenic,"look",look);Set(scenic,"session",session);Set(scenic,"progress",progress);Set(scenic,"hint",Text("Mirar el entorno",safe,116,21));
   var focusData=new SerializedObject(scenic);var views=focusData.FindProperty("views");views.arraySize=2;
   Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
   var centers=new[]{Ground(103,336),Ground(92,400)};var points=new[]{Ground(93,352)+Vector3.up*1.2f,Ground(83,414)+Vector3.up*1.5f};
   for(int i=0;i<2;i++){var entrance=New("Mirada - entrada "+i,root.transform).transform;entrance.position=centers[i];var destination=New("Mirada - destino "+i,root.transform).transform;destination.position=points[i];var v=views.GetArrayElementAtIndex(i);v.FindPropertyRelative("entrance").objectReferenceValue=entrance;v.FindPropertyRelative("target").objectReferenceValue=destination;v.FindPropertyRelative("radius").floatValue=7;v.FindPropertyRelative("caption").stringValue=i==0?"Puesto avanzado":"Refugios del camino";}focusData.ApplyModifiedProperties();
   Save(s);RefineNpc();RefineFocus();UnifyFocus();RebuildNavigation();Inspect();
  }
  static Camera CameraFrom(Scene s)=>All<Camera>(s).Single(c=>c.CompareTag("MainCamera"));
  public static void RefineFocus()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var scenic=All<ScenicFocus>(scene).Single();var data=new SerializedObject(scenic);var view=data.FindProperty("views").GetArrayElementAtIndex(1);var target=(Transform)view.FindPropertyRelative("target").objectReferenceValue;
   var shelter=All<Transform>(scene).Where(t=>t.name.StartsWith("Collapsed shelter")||t.name.StartsWith("Refugio dañado")).OrderBy(t=>Vector3.Distance(t.position,target.position)).First();
   var renderers=shelter.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);Undo.RecordObject(target,"Focus the existing damaged shelter");target.position=bounds.center;Save(scene);File.WriteAllText("Library/InteractionReview-focus.txt",shelter.name+" at "+bounds.center);
  }
  public static void RefineNpc()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
   foreach(var interaction in All<NPCInteractableScript>(scene))
   {
    var owner=interaction.GetComponentInParent<SpeakerScript>();if(!owner)continue;Undo.RecordObject(interaction,"NPC speech owns no inspection or inherited thought");interaction.speaker=owner;if(owner.name=="NPC")interaction.interactableName="Jeremías";
    var so=new SerializedObject(interaction);foreach(string f in new[]{"stateDescription","focusInteraction","inspectInteraction","subInteractions"})so.FindProperty(f).boolValue=false;so.ApplyModifiedProperties();if(interaction.UIInteraction!=null)interaction.UIInteraction.label="Hablar";EditorUtility.SetDirty(interaction);
   }
   Save(scene);
  }
  public static void InspectNavigation()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var nav=All<CombatNavigation>(scene).Single();var data=(NavMeshData)new SerializedObject(nav).FindProperty("data").objectReferenceValue;var instance=NavMesh.AddNavMeshData(data);var log=new StringBuilder();
   try
   {
    Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
    NavMesh.SamplePosition(Ground(56,531),out var entry,3,NavMesh.AllAreas);log.AppendLine("ENTRY "+entry.position);
    for(int z=530;z<=600;z+=2){var connected=new System.Collections.Generic.List<int>();for(int x=20;x<=110;x+=2){var point=Ground(x,z);if(!NavMesh.SamplePosition(point,out var sample,2,NavMesh.AllAreas))continue;var path=new NavMeshPath();if(NavMesh.CalculatePath(entry.position,sample.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)connected.Add(x);}log.AppendLine("Z "+z+" connectedX="+string.Join(",",connected));}
    foreach(var collider in All<Collider>(scene))if(!collider.isTrigger&&collider.enabled&&collider.bounds.Intersects(new Bounds(Ground(60,560),new Vector3(80,20,60))))log.AppendLine("OBSTACLE "+PathOf(collider.transform)+" "+collider.GetType().Name+" "+collider.bounds);
   }finally{instance.Remove();}
   File.WriteAllText("Library/InteractionReview-navigation.txt",log.ToString());
  }
  public static void RebuildNavigation()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var player=All<CameraControllerScript>(scene).Single().transform;
   // These effect detectors only became triggers in Awake, after the previous bake.
   foreach(var effect in All<MonoBehaviour>(scene).Where(c=>c&&(c is Systems.VFXSystem.New.Triggers.VFXColliderTrigger||c is Systems.VFXSystem.New.InmersiveVFXObject)))foreach(var detector in effect.GetComponents<Collider>()){Undo.RecordObject(detector,"Effect volume is not physical cover");detector.isTrigger=true;}
   Physics.SyncTransforms();var bounds=new Bounds(new Vector3(70,terrain.SampleHeight(new Vector3(70,0,560))+terrain.transform.position.y,560),new Vector3(130,130,110));var sources=new System.Collections.Generic.List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new System.Collections.Generic.List<NavMeshBuildMarkup>(),sources);
   sources.RemoveAll(source=>source.component&&(source.component.transform.IsChildOf(player)||PathOf(source.component.transform).Contains("/VFX/")||source.component.name.Contains("Route_")||source.component.name.Contains("MissionGate")||(source.component is Collider c&&(c.isTrigger||!c.enabled))));
   var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.45f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;settings.minRegionArea=2;
   var built=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!built)throw new InvalidOperationException("Front navigation did not bake");string asset=Data+"/FrontNavigation.asset";var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(asset);if(existing){Undo.RecordObject(existing,"Connected front navigation");EditorUtility.CopySerialized(built,existing);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(built,asset);existing=built;}Set(All<CombatNavigation>(scene).Single(),"data",existing);Save(scene);InspectNavigation();
  }
  public static void UnifyFocus()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
   foreach(var legacy in All<Systems.InmersionSystem.Elements.LookAtPointScript>(scene))
   {
    Undo.RecordObject(legacy,"Use the brief interruptible route focus");legacy.enabled=false;var so=new SerializedObject(legacy);so.FindProperty("lookAtOnTrigger").boolValue=false;so.FindProperty("canRepeatLookAt").boolValue=false;so.ApplyModifiedProperties();
   }
   Save(scene);
  }
  public static void Inspect()
  {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var log=new StringBuilder();
   foreach(var speaker in All<SpeakerScript>(s))
   {
    log.AppendLine("NPC "+PathOf(speaker.transform)+" "+speaker.transform.position);
    foreach(var a in speaker.GetComponentsInChildren<Animator>(true)){log.AppendLine("  Animator "+a.name+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController));if(a.runtimeAnimatorController)foreach(var c in a.runtimeAnimatorController.animationClips)log.AppendLine("  clip "+c.name+" "+AssetDatabase.GetAssetPath(c));}
    foreach(var c in speaker.GetComponentsInChildren<Collider>(true))log.AppendLine("  collider "+PathOf(c.transform)+" center="+c.bounds.center+" size="+c.bounds.size);
    foreach(var b in speaker.GetComponentsInChildren<MonoBehaviour>(true))if(b){var so=new SerializedObject(b);var p=so.GetIterator();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.Generic&&p.type=="UnityEvent")log.AppendLine("  event "+b.GetType().Name+" "+p.propertyPath);}
   }
   foreach(var t in All<TMP_Text>(s))if(t.text.IndexOf("zoom",StringComparison.OrdinalIgnoreCase)>=0||t.text.IndexOf("rueda",StringComparison.OrdinalIgnoreCase)>=0)log.AppendLine("HINT "+PathOf(t.transform)+" "+t.text);
   foreach(var c in All<Component>(s))if(c&&(c.GetType().Name.Contains("End")||c.GetType().Name.Contains("SpawnArea")||c.GetType().Name=="BattleManager"))log.AppendLine("ENDING/SPAWN "+c.GetType().Name+" "+PathOf(c.transform)+" "+EditorJsonUtility.ToJson(c));
   var puzzle=All<LetterAssemblyPuzzle>(s).Single();var ps=new SerializedObject(puzzle);foreach(string field in new[]{"board","slots","pieces"}){var p=ps.FindProperty(field);if(p.isArray){for(int i=0;i<p.arraySize;i++){var r=(Component)p.GetArrayElementAtIndex(i).objectReferenceValue;log.AppendLine(field+" "+PathOf(r.transform)+" "+EditorJsonUtility.ToJson(r));}}else{var r=(Component)p.objectReferenceValue;log.AppendLine(field+" "+PathOf(r.transform)+" "+EditorJsonUtility.ToJson(r));}}
   File.WriteAllText("Library/InteractionReview-inventory.txt",log.ToString());
  }
 }
}
#endif
