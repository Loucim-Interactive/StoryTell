#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using StoryTell.PhysicalMap;
using StoryTell.PhysicalMap.Editor;
using Systems.InspectionSystem.Scripts;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace StoryTell.GameplayPath.Editor
{
 public static class GameplayPathNextAuthoring
 {
  const string ScenePath="Assets/Scenes/Level1.unity";
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static TMP_FontAsset Medium=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");
  static TMP_FontAsset Bold=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Bold SDF.asset");
  public static void Inspect()
  {
   var scene=EditorSceneManager.OpenScene(ScenePath);var root=All<GameplayPathController>(scene).Single().transform;var reference=All<MapWorldReference>(scene).Single();
   File.WriteAllText("Library/GameplayPath-next-inspection.txt","map origin="+reference.WorldOrigin.position+" worldSize="+reference.WorldSize+"\n"+string.Join("\n",root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Ruben_")||t.name.StartsWith("Gomez_")||t.name.StartsWith("Sargento_")||t.name.StartsWith("Oficial_")||t.name.StartsWith("Cobertura_")||t.name.StartsWith("Point_")||t.name.StartsWith("Cartel_")||t.name=="Mesa_Suministros"||t.name=="Kitchen_Table"||t.name.StartsWith("BattleEntrance")).Select(t=>t.name+" position="+t.position+" scale="+t.lossyScale)));
  }
  [MenuItem("StoryTell/Gameplay Path/Apply map zones letter puzzle and combat improvements")]
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
   var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath){if(scene.IsValid()&&scene.isDirty)throw new InvalidOperationException("Save the current scene first.");scene=EditorSceneManager.OpenScene(ScenePath);}
   if(All<LetterAssemblyPuzzle>(scene).Any())throw new InvalidOperationException("This update is already installed.");
   Directory.CreateDirectory("Library/GameplayPathBackups");if(!EditorSceneManager.SaveScene(scene,"Library/GameplayPathBackups/Level1-before-next-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Cannot back up scene.");
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Map zones letter reconstruction and coherent combat");
   var controller=All<GameplayPathController>(scene).Single();var root=controller.transform;
   foreach(var actor in root.GetComponentsInChildren<GameplayPathInteractable>(true).Where(i=>i.name.StartsWith("Ruben_")||i.name.StartsWith("Gomez_")||i.name.StartsWith("Sargento_")||i.name.StartsWith("Oficial_")))if(!actor.GetComponent<NpcConversationFacing>())Undo.AddComponent<NpcConversationFacing>(actor.gameObject);
   foreach(var sign in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cartel_")).ToArray()){Undo.RecordObject(sign.gameObject,"Retire orientation signs now represented on the map");sign.gameObject.SetActive(false);}
   UpdateDialogue(controller.Definition);
   var canvas=root.GetComponentInChildren<Canvas>(true);if(!canvas.GetComponent<GraphicRaycaster>())Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
   var safe=canvas.transform.Find("SafeArea")??canvas.transform;
   BuildPuzzle(controller,safe);BuildMap(scene,controller,safe);BuildCover(scene,controller);
   foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   FinishVisuals();File.WriteAllText("Library/GameplayPath-next-result.txt","applied");Inspect();
  }
  public static void ApplyBatch(){EditorSceneManager.OpenScene(ScenePath);Apply();}
  public static void FinishVisuals()
  {
   var scene=EditorSceneManager.OpenScene(ScenePath);
   foreach(var piece in All<LetterPuzzlePiece>(scene))if(!piece.GetComponent<CanvasRenderer>())Undo.AddComponent<CanvasRenderer>(piece.gameObject);
   var trial=All<StoryTell.Weapons.Level1WeaponTrial>(scene).Single();var data=new SerializedObject(trial);var darkness=(Image)data.FindProperty("darkness").objectReferenceValue;
   Undo.RecordObject(darkness.rectTransform,"Keep the ending fade full screen");Stretch(darkness.transform);darkness.rectTransform.localScale=Vector3.one;darkness.rectTransform.localRotation=Quaternion.identity;
   Undo.RecordObject(darkness,"Use an unmasked full screen fade");darkness.sprite=null;darkness.type=Image.Type.Simple;darkness.preserveAspect=false;darkness.maskable=false;
   foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   UpdateDialogue(All<GameplayPathController>(scene).Single().Definition);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }
  static void BuildPuzzle(GameplayPathController controller,Transform parent)
  {
   var host=UI("LetterAssembly",parent);Stretch(host.transform);var puzzle=Undo.AddComponent<LetterAssemblyPuzzle>(host);
   var panel=UI("ReconstructionPanel",host.transform);Stretch(panel.transform);var backdrop=Undo.AddComponent<Image>(panel);backdrop.color=new Color(0.025f,0.035f,0.04f,0.98f);
   var title=Text("PuzzleTitle",panel.transform,"Reconstruí la carta",Bold,38);Anchor(title.transform,new Vector2(0.5f,1),new Vector2(0,-36),new Vector2(1000,70));title.alignment=TextAlignmentOptions.Center;
   var subtitle=Text("PuzzleHint",panel.transform,"Uní los bordes de los cuatro fragmentos para poder devolverla a Rubén. Sin tiempo límite.",Medium,23);Anchor(subtitle.transform,new Vector2(0.5f,1),new Vector2(0,-107),new Vector2(1500,64));subtitle.alignment=TextAlignmentOptions.Center;
   var board=UI("LetterBoard",panel.transform);Anchor(board.transform,new Vector2(0.5f,0.51f),Vector2.zero,new Vector2(720,500));var boardImage=Undo.AddComponent<Image>(board);boardImage.color=new Color(0.21f,0.23f,0.22f,0.45f);boardImage.raycastTarget=false;
   var slots=new RectTransform[4];var pieces=new LetterPuzzlePiece[4];string[] paragraphs=controller.Definition.motherLetter.Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries);
   for(int i=0;i<4;i++)
   {
    var slot=UI("Slot_"+i,board.transform);Anchor(slot.transform,Vector2.one*0.5f,new Vector2(i%2==0?-180:180,i/2==0?125:-125),new Vector2(360,250));slots[i]=(RectTransform)slot.transform;var ghost=Undo.AddComponent<Image>(slot);ghost.color=new Color(1,1,1,0.045f);ghost.raycastTarget=false;
    var obj=UI("Fragment_"+i,panel.transform);Anchor(obj.transform,new Vector2(i%2==0?0.12f:0.88f,i/2==0?0.67f:0.32f),Vector2.zero,new Vector2(360,250));obj.transform.localRotation=Quaternion.Euler(0,0,new[]{90,-90,180,0}[i]);
    var piece=Undo.AddComponent<LetterPuzzlePiece>(obj);piece.slotIndex=i;piece.owner=puzzle;piece.color=new Color(0.94f,0.89f,0.76f);pieces[i]=piece;
    var shadow=Undo.AddComponent<Shadow>(obj);shadow.effectColor=new Color(0,0,0,0.5f);shadow.effectDistance=new Vector2(4,-4);
    var ink=Text("LetterInk",obj.transform,paragraphs[Mathf.Min(i,paragraphs.Length-1)],Medium,22);Stretch(ink.transform,22);ink.color=new Color(0.17f,0.14f,0.10f);ink.enableAutoSizing=true;ink.fontSizeMin=18;ink.fontSizeMax=22;ink.textWrappingMode=TextWrappingModes.Normal;ink.overflowMode=TextOverflowModes.Ellipsis;
   }
   var status=Text("PuzzleStatus",panel.transform,"",Medium,25);Anchor(status.transform,new Vector2(0.5f,0),new Vector2(0,74),new Vector2(1420,116));status.alignment=TextAlignmentOptions.Center;
   var back=UI("ClosePuzzle",panel.transform);Anchor(back.transform,new Vector2(1,1),new Vector2(-100,-49),new Vector2(150,50));var backImage=Undo.AddComponent<Image>(back);backImage.color=new Color(0.23f,0.26f,0.25f,1);var button=Undo.AddComponent<Button>(back);button.targetGraphic=backImage;UnityEventTools.AddPersistentListener(button.onClick,puzzle.Close);var label=Text("ButtonLabel",back.transform,"Volver",Medium,24);Stretch(label.transform);label.alignment=TextAlignmentOptions.Center;
   Set(puzzle,"controller",controller);Set(puzzle,"panel",panel);Set(puzzle,"board",board.transform);Set(puzzle,"status",status);SetArray(puzzle,"slots",slots);SetArray(puzzle,"pieces",pieces);Set(controller,"letterPuzzle",puzzle);panel.SetActive(false);
   var eventSystem=All<EventSystem>(controller.gameObject.scene).FirstOrDefault();if(eventSystem==null){var obj=new GameObject("GameplayPath_EventSystem");Undo.RegisterCreatedObjectUndo(obj,"Create puzzle event system");eventSystem=Undo.AddComponent<EventSystem>(obj);}
   foreach(var legacy in eventSystem.GetComponents<StandaloneInputModule>()){Undo.RecordObject(legacy,"Use Input System for letter dragging");legacy.enabled=false;}
   var module=eventSystem.GetComponent<InputSystemUIInputModule>()??Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);module.AssignDefaultActions();
  }
  static void BuildMap(Scene scene,GameplayPathController controller,Transform parent)
  {
   var root=controller.transform;Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
   var map=All<PhysicalMapController>(scene).Single();var reference=map.GetComponent<MapWorldReference>();var settings=map.Settings;Undo.RecordObject(settings,"Make the held map a plausible paper size");settings.width=0.48f;settings.height=0.32f;settings.localHoldPosition=new Vector3(-0.20f,-0.07f,0.87f);settings.bendStrength=0.025f;settings.foldAmount=0.02f;EditorUtility.SetDirty(settings);
   var filter=map.GetComponent<MeshFilter>();var deformation=map.GetComponent<PaperDeformationController>();Undo.RecordObject(filter,"Use compact Level1 map geometry");deformation.RebuildMesh(settings);var generated=filter.sharedMesh;const string meshPath="Assets/Systems/GameplayPath/Data/Map_Level1_Compact.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null){saved=Object.Instantiate(generated);saved.name="Map_Level1_Compact";AssetDatabase.CreateAsset(saved,meshPath);}else{EditorUtility.CopySerialized(generated,saved);EditorUtility.SetDirty(saved);}filter.sharedMesh=saved;Object.DestroyImmediate(generated);
   Undo.RecordObject(map.GetComponent<BoxCollider>(),"Fit paper collision");map.GetComponent<BoxCollider>().size=new Vector3(settings.width,settings.colliderThickness,settings.height);
   var locations=new Transform[]{Find("Mesa_Suministros"),Find("Kitchen_Table"),Find("Sargento_Acosta_CarpaPrincipal"),Find("Oficial_Recepcion_Caja"),null,null};
   var gate=All<GameplayPathBattleGate>(scene).Single();var bombs=new GameObject("Zona_PasoDeBombas");Undo.RegisterCreatedObjectUndo(bombs,"Mark bomb passage on map");bombs.transform.SetParent(root,false);bombs.transform.position=Ground(gate.transform.position+Vector3.back*18);locations[4]=bombs.transform;
   var front=new GameObject("Zona_Frente");Undo.RegisterCreatedObjectUndo(front,"Mark frontline on map");front.transform.SetParent(root,false);front.transform.position=Ground(gate.transform.position+Vector3.forward*25);locations[5]=front.transform;
   var bounds=new Bounds(controller.Player.position,Vector3.zero);foreach(var point in locations)bounds.Encapsulate(point.position);foreach(var point in Find("Routes_BehindCover").GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Point_")))bounds.Encapsulate(point.position);bounds.Expand(50);var worldSize=new Vector2(Mathf.Max(bounds.size.z*1.5f,bounds.size.x),bounds.size.z);Level1PhysicalMapInstaller.ReframeSurvey(map,bounds.center,worldSize);
   var host=UI("MapZones",parent);Stretch(host.transform);var overlay=Undo.AddComponent<MapZoneOverlay>(host);var content=UI("ZoneMarkers",host.transform);Stretch(content.transform);string[] names={"Equipo","Cocina","Carpa principal","Puesto avanzado","Paso de bombas","Frente"};Vector2[] offsets={new(-48,-30),new(-32,20),new(44,-28),new(38,12),new(-34,-8),new(32,18)};var entries=new MapZoneOverlay.Zone[6];
   for(int i=0;i<entries.Length;i++)
   {
    var line=UI("Leader_"+(i+1),content.transform);Anchor(line.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(20,2));var lineImage=Undo.AddComponent<Image>(line);lineImage.color=new Color(0.2f,0.17f,0.1f,0.9f);lineImage.raycastTarget=false;
    var marker=UI("Zone_"+(i+1)+"_"+names[i],content.transform);Anchor(marker.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(26,26));var circle=Undo.AddComponent<Image>(marker);circle.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");circle.color=new Color(0.19f,0.16f,0.10f,1);circle.raycastTarget=false;var number=Text("Number",marker.transform,(i+1).ToString(),Bold,19);Stretch(number.transform);number.alignment=TextAlignmentOptions.Center;
    entries[i]=new MapZoneOverlay.Zone{name=names[i],location=locations[i],marker=(RectTransform)marker.transform,leader=(RectTransform)line.transform,labelOffset=offsets[i]};
   }
   Set(overlay,"map",map);Set(overlay,"reference",reference);Set(overlay,"mapCamera",map.GetComponent<PhysicalMapViewCamera>().MapCamera);Set(overlay,"overlay",content);var data=new SerializedObject(overlay);var array=data.FindProperty("zones");array.arraySize=entries.Length;for(int i=0;i<entries.Length;i++){var e=array.GetArrayElementAtIndex(i);e.FindPropertyRelative("name").stringValue=entries[i].name;e.FindPropertyRelative("location").objectReferenceValue=entries[i].location;e.FindPropertyRelative("marker").objectReferenceValue=entries[i].marker;e.FindPropertyRelative("leader").objectReferenceValue=entries[i].leader;e.FindPropertyRelative("labelOffset").vector2Value=entries[i].labelOffset;}data.ApplyModifiedProperties();content.SetActive(false);
   var journal=(GameObject)new SerializedObject(controller).FindProperty("journalPanel").objectReferenceValue;var list=(TMP_Text)new SerializedObject(controller).FindProperty("journalText").objectReferenceValue;Undo.RecordObject(list.rectTransform,"Reserve space for zone legend");list.rectTransform.offsetMin=new Vector2(24,270);
   var legend=Text("MapZoneLegend",journal.transform,"ZONAS\n1  Equipo\n2  Cocina\n3  Carpa principal\n4  Puesto avanzado\n5  Paso de bombas\n6  Frente\nPunto celeste: tu ubicación",Medium,22);var rect=legend.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(0.5f,0);rect.offsetMin=new Vector2(24,18);rect.offsetMax=new Vector2(-24,252);
  }
  static void BuildCover(Scene scene,GameplayPathController controller)
  {
   var root=controller.transform;var field=root.Find("Battlefield_ExistingExplosionZone");var host=new GameObject("Additional_Combat_Cover");Undo.RegisterCreatedObjectUndo(host,"Add combat route obstacles");host.transform.SetParent(field,false);
   Vector3[] positions={new(31,0,584),new(41,0,570),new(35,0,565),new(58,0,589),new(49,0,583),new(60,0,570),new(74,0,582),new(74,0,562)};
   for(int i=0;i<positions.Length;i++)
   {
    bool sandbags=i%2==0;string path=sandbags?"Assets/Art/Outsourced/Props/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab":"Assets/Prefabs/World/Crates/SmallBox.prefab";
    var wrapper=new GameObject("Cover_Additional_"+i);Undo.RegisterCreatedObjectUndo(wrapper,"Place physical combat cover");wrapper.transform.SetParent(host.transform,false);wrapper.transform.position=Ground(positions[i]);wrapper.transform.rotation=Quaternion.Euler(0,i%3==0?25:-20,0);
    AddModel(wrapper.transform,path,sandbags?3.5f:1.5f,0);
    if(!sandbags){var b=GameplayPathScaleAuthoring.VisualBounds(wrapper.transform);AddModel(wrapper.transform,path,1.3f,b.size.y);}
    var box=Undo.AddComponent<BoxCollider>(wrapper);var local=new Bounds();bool first=true;
    foreach(var mesh in wrapper.GetComponentsInChildren<MeshFilter>())
    {
     if(mesh.sharedMesh==null)continue;var b=mesh.sharedMesh.bounds;
     for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
     {var p=wrapper.transform.InverseTransformPoint(mesh.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);}
    }
    box.center=local.center;box.size=local.size;
   }
   Physics.SyncTransforms();var surface=All<NavMeshSurface>(scene).Single();var saved=surface.navMeshData;surface.BuildNavMesh();if(surface.navMeshData==null)throw new InvalidOperationException("Navigation failed.");EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();var nav=new SerializedObject(surface);nav.FindProperty("m_NavMeshData").objectReferenceValue=saved;nav.ApplyModifiedProperties();surface.AddData();EditorUtility.SetDirty(saved);
   var gate=All<GameplayPathBattleGate>(scene).Single();NavMesh.SamplePosition(gate.transform.position,out var goal,8,NavMesh.AllAreas);
   foreach(var point in field.Find("Routes_BehindCover").GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Point_")))
   {Undo.RecordObject(point,"Keep enemy route beside cover");point.position=Connected(point.position,goal.position);}
  }
  static Vector3 Connected(Vector3 start,Vector3 goal)
  {
   var path=new NavMeshPath();for(float radius=0;radius<=8;radius+=0.5f)for(int i=0;i<(radius==0?1:20);i++){float a=i*Mathf.PI*2/20;var p=start+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);if(NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(hit.position,goal,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return hit.position;}throw new InvalidOperationException("No connected route near "+start);
  }
  static void AddModel(Transform parent,string path,float maximum,float lift)
  {
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);Undo.RegisterCreatedObjectUndo(model,"Reuse cover model");foreach(var c in model.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var r in model.GetComponentsInChildren<Rigidbody>(true)){r.isKinematic=true;r.useGravity=false;}foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true))if(b)b.enabled=false;
   var bounds=GameplayPathScaleAuthoring.VisualBounds(model.transform);model.transform.localScale*=maximum/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));bounds=GameplayPathScaleAuthoring.VisualBounds(model.transform);model.transform.position+=parent.position+Vector3.up*lift-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
  }
  static void UpdateDialogue(GameplayPathDefinition definition)
  {
   foreach(var conversation in new[]{definition.introduction,definition.equipmentReady,definition.kitchen,definition.letterRequest,definition.letterReturn,definition.sergeant,definition.delivery})foreach(var line in conversation.Dialogues)
   {
    var value=line.DialogueText.Replace("al lado del cartel EQUIPO","en el sector EQUIPO indicado en el mapa").Replace("junto al cartel","en la zona indicada en el mapa").Replace("y los carteles al puesto","y la ubicación del puesto indicada en el mapa").Replace("Seguí el paso que sale del campamento y la ubicación del puesto indicada en el mapa. Está pasando la subida. Consultá el mapa si perdés la referencia.","Seguí el paso que sale del campamento hacia la subida. El PUESTO AVANZADO está marcado en el mapa; consultalo si perdés la referencia.").Replace("Encontré los cuatro pedazos. Acá tenés.","Encontré los cuatro pedazos y armé la carta. Acá tenés.");
    if(value==line.DialogueText)continue;var data=new SerializedObject(line);data.FindProperty("_dialogueText").stringValue=value;data.ApplyModifiedProperties();EditorUtility.SetDirty(line);
   }
  }
  static Vector3 Ground(Vector3 p){var terrain=Terrain.activeTerrains.First(t=>p.x>=t.transform.position.x&&p.z>=t.transform.position.z&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z<=t.transform.position.z+t.terrainData.size.z);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+0.015f;return p;}
  static GameObject UI(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Create readable UI");obj.transform.SetParent(parent,false);obj.layer=5;return obj;}
  static TMP_Text Text(string name,Transform parent,string text,TMP_FontAsset font,float size){var obj=UI(name,parent);var t=Undo.AddComponent<TextMeshProUGUI>(obj);t.font=font;t.fontSharedMaterial=font.material;t.text=text;t.fontSize=size;t.color=new Color(0.94f,0.92f,0.85f);t.raycastTarget=false;t.margin=Vector4.zero;t.fontStyle=FontStyles.Normal;t.textWrappingMode=TextWrappingModes.Normal;return t;}
  static void Stretch(Transform t,float margin=0){var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*margin;r.offsetMax=-Vector2.one*margin;}
  static void Anchor(Transform t,Vector2 anchor,Vector2 position,Vector2 size){var r=(RectTransform)t;r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*0.5f;r.anchoredPosition=position;r.sizeDelta=size;}
  static void Set(Object target,string field,Object value){var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedProperties();}
  static void SetArray(Object target,string field,Object[] values){var data=new SerializedObject(target);var array=data.FindProperty(field);array.arraySize=values.Length;for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];data.ApplyModifiedProperties();}
 }
}
#endif
