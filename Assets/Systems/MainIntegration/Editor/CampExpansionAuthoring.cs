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
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.InteractionSystem.Scripts.Utils;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.Missions.FindObjects;
using Systems.BattleSystem;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
namespace StoryTell.MainIntegration.Editor
{
 public static class CampExpansionAuthoring
 {
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  const string Data="Assets/Systems/MainIntegration/Data/CampExpansion";
  static Terrain terrain;
  static Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
  static void Set(UnityEngine.Object o,string f,UnityEngine.Object v){var so=new SerializedObject(o);so.FindProperty(f).objectReferenceValue=v;so.ApplyModifiedProperties();}
  static GameObject New(string name,Transform parent){var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Expand the linear camp route");go.transform.SetParent(parent,false);return go;}
  static Bounds BoundsOf(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds;}
  static void Seat(GameObject go,Vector3 point){Undo.RecordObject(go.transform,"Seat the prop");var b=BoundsOf(go);go.transform.position+=new Vector3(point.x-b.center.x,point.y-b.min.y+.015f,point.z-b.center.z);}
  static ConversationSO Conversation(string id,ESpeakers speaker,params string[] words)
  {
   var existing=AssetDatabase.LoadAssetAtPath<ConversationSO>(Data+"/"+id+".asset");if(existing)return existing;
   var conversation=ScriptableObject.CreateInstance<ConversationSO>();var cs=new SerializedObject(conversation);cs.FindProperty("_autoNext").boolValue=false;cs.ApplyModifiedProperties();conversation.Dialogues=new DialogueSO[words.Length];
   for(int i=0;i<words.Length;i++){var line=ScriptableObject.CreateInstance<DialogueSO>();var data=new SerializedObject(line);data.FindProperty("_speaker").enumValueIndex=(int)speaker;data.FindProperty("_dialogueText").stringValue=words[i];data.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/"+id+"_"+i+".asset");conversation.Dialogues[i]=line;}
   AssetDatabase.CreateAsset(conversation,Data+"/"+id+".asset");return conversation;
  }
  static void Assign(SpeakerScript actor,ConversationSO conversation){var data=new SerializedObject(actor);var choices=data.FindProperty("_conversationSos");choices.arraySize=1;choices.GetArrayElementAtIndex(0).objectReferenceValue=conversation;data.ApplyModifiedProperties();}
  static void MoveActor(SpeakerScript actor,Vector3 point)
  {
   Undo.RecordObject(actor.transform,"Place the soldier");actor.transform.position=point;
   var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");var mesh=new Mesh();boots.BakeMesh(mesh,true);float gap=mesh.vertices.Min(v=>{var p=boots.transform.position+boots.transform.rotation*v;return p.y-terrain.SampleHeight(p)-terrain.transform.position.y;});UnityEngine.Object.DestroyImmediate(mesh);actor.transform.position+=Vector3.up*(.06f-gap);
  }
  static SpeakerScript Soldier(SpeakerScript source,Transform parent,string name,string display,Vector3 point)
  {
   var go=UnityEngine.Object.Instantiate(source.gameObject,parent);Undo.RegisterCreatedObjectUndo(go,"New camp soldier");go.name="NPC - "+name;foreach(var hint in go.GetComponents<LetterHintProvider>())Undo.DestroyObjectImmediate(hint);var actor=go.GetComponent<SpeakerScript>();
   foreach(var i in go.GetComponentsInChildren<NPCInteractableScript>(true)){i.speaker=actor;i.interactableName=display;var data=new SerializedObject(i);foreach(string f in new[]{"stateDescription","focusInteraction","inspectInteraction","subInteractions"})data.FindProperty(f).boolValue=false;data.ApplyModifiedProperties();}
   foreach(var driver in go.GetComponentsInChildren<NPCAnimatorScript>(true)){Set(driver,"speaker",actor);Set(driver,"movementRoot",actor.transform);driver.UnlockAnimation();driver.SetCrouching(false);}
   foreach(var feet in go.GetComponentsInChildren<NpcFootPlacement>(true)){Set(feet,"actor",actor.transform);Set(feet,"ground",terrain);}
   MoveActor(actor,point);return actor;
  }
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<SupplyDispatch>(scene).Length>0)throw new InvalidOperationException("Camp expansion already applied");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Expand camp and dispatch supplies linearly");terrain=All<Terrain>(scene).Single();var root=New("CampExpansion",null);var actors=All<SpeakerScript>(scene);var source=actors.Single(a=>a.name=="NPC - Simon");
   var tentSource=All<MeshRenderer>(scene).Single(r=>r.name=="Tent_002");var tents=new GameObject[3];var places=new[]{Ground(61,260),Ground(52,279),Ground(61,307)};
   for(int i=0;i<3;i++){var tent=UnityEngine.Object.Instantiate(tentSource.gameObject,root.transform);Undo.RegisterCreatedObjectUndo(tent,"Camp tent");tent.name="Carpa del campamento "+i;Seat(tent,places[i]);tents[i]=tent;}
   var kitchen=All<MeshRenderer>(scene).Single(r=>r.name=="Mesa de cocina");var meal=All<CampMeal>(scene).Single();var old=kitchen.bounds.center;Seat(kitchen.gameObject,Ground(85,283));var delta=kitchen.bounds.center-old;Undo.RecordObject(meal.transform,"Move the meal with the kitchen");meal.transform.position+=delta;
   var mateo=actors.Single(a=>a.name.Contains("Mateo"));MoveActor(mateo,Ground(85,288));MoveActor(actors.Single(a=>a.name=="NPC - Cocinero"),Ground(87,285));
   var sergeant=Soldier(source,root.transform,"Sargento Ibarra","Sargento Ibarra",Ground(65.5f,269));var nestor=Soldier(source,root.transform,"Nestor","Néstor",Ground(58,278));var andres=Soldier(source,root.transform,"Andres","Andrés",Ground(66.5f,302));var receiver=Soldier(source,root.transform,"Benitez","Benítez",Ground(45,500));
   Assign(nestor,Conversation("Nestor",ESpeakers.Nestor,"Soy Néstor. Estoy revisando que no se aflojen las sogas de las carpas.","Con este viento, una lona suelta no te deja dormir en toda la noche.","La cocina quedó entre las carpas del centro. Si no comiste, acercate."));
   Assign(andres,Conversation("Andres",ESpeakers.Andres,"Soy Andrés. Estoy ordenando lo que quedó seco.","Mi hermana me mandó una foto. La guardo entre dos cartones para que no se doble.","José anda por los cajones. Siempre encuentra las cosas que el viento mueve."));
   var outpost=actors.Single(a=>a.name=="NPC - Ruben");var dispatch=root.AddComponent<SupplyDispatch>();Set(dispatch,"letter",All<LetterManager>(scene).Single());Set(dispatch,"sergeant",sergeant);Set(dispatch,"outpost",outpost);Set(dispatch,"receiver",receiver);Set(dispatch,"feedback",All<MissionAnnouncement>(scene).Single());
   Set(dispatch,"sergeantBlocked",Conversation("SergeantBlocked",ESpeakers.SargentoIbarra,"Primero terminá lo que tenés pendiente con Diego y Mateo. Después vení a verme."));
   Set(dispatch,"sergeantOrder",Conversation("SergeantOrder",ESpeakers.SargentoIbarra,"Soy el sargento Ibarra. Mateo me dijo que podía contar con vos.","Andá al puesto avanzado y buscá a Rubén. Tiene preparado un envío para el puesto de radio.","Seguí el camino. Rubén está junto a los antitanques."));
   Set(dispatch,"sergeantAfter",Conversation("SergeantAfter",ESpeakers.SargentoIbarra,"Rubén te espera en el puesto avanzado. Él te va a indicar qué llevar."));
   Set(dispatch,"outpostBlocked",Conversation("OutpostBlocked",ESpeakers.Ruben,"Antes de darte un encargo necesito que hables con el sargento Ibarra. Está en el campamento."));
   Set(dispatch,"outpostOrder",Conversation("OutpostOrder",ESpeakers.Ruben,"Ibarra te mandó, ¿no? Hay un paquete de repuestos de radio sobre la mesa, junto a los cajones.","Guardalo en tu mochila y lleváselo a Benítez, en el puesto de radio del frente.","Seguí el camino entre los refugios. Benítez te espera antes de la zona de explosiones."));
   Set(dispatch,"outpostAfter",Conversation("OutpostAfter",ESpeakers.Ruben,"El paquete está sobre la mesa del puesto. Benítez lo necesita en el puesto de radio."));
   Set(dispatch,"receiverBlocked",Conversation("ReceiverBlocked",ESpeakers.Benitez,"Soy Benítez. Estoy esperando unos repuestos del puesto de Rubén. Hablá con él antes de seguir."));
   Set(dispatch,"receiverDelivery",Conversation("ReceiverDelivery",ESpeakers.Benitez,"¿Trajiste los repuestos? Gracias, dejámelos acá.","Con esto podemos seguir trabajando en la radio.","Seguí hacia el frente por el paso entre los sacos de arena."));
   Set(dispatch,"receiverAfter",Conversation("ReceiverAfter",ESpeakers.Benitez,"Los repuestos ya están acá. Seguí por el paso entre los sacos."));
   var letter=All<LetterManager>(scene).Single();Set(letter,"completedConversation",Conversation("MateoDelivered",ESpeakers.Mateo,"Gracias por devolverme la carta. Me hacía falta leer algo de casa.","El sargento Ibarra te está buscando. Está junto a la carpa de mando, en el campamento."));
   var tableSource=All<MeshRenderer>(scene).Single(r=>r.name=="wooden_table (3)");var table=UnityEngine.Object.Instantiate(tableSource.gameObject,root.transform);Undo.RegisterCreatedObjectUndo(table,"Dispatch table");table.name="Mesa de repuestos del puesto";Seat(table,Ground(101,350));
   var boxSource=All<InteractableScript>(scene).First(f=>f.name=="SmallBox"||f.name=="MediumBox").GetComponentInChildren<MeshFilter>(true);var packet=New("Repuestos de radio - guardar en mochila",root.transform);packet.AddComponent<MeshFilter>().sharedMesh=boxSource.sharedMesh;packet.AddComponent<MeshRenderer>().sharedMaterials=boxSource.GetComponent<Renderer>().sharedMaterials;var bounds=BoundsOf(packet);packet.transform.localScale*=.45f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);Seat(packet,new Vector3(table.GetComponent<Renderer>().bounds.center.x,table.GetComponent<Renderer>().bounds.max.y,table.GetComponent<Renderer>().bounds.center.z));packet.layer=source.GetComponentInChildren<NPCInteractableScript>().gameObject.layer;packet.AddComponent<BoxCollider>();var pickup=packet.AddComponent<RadioPartsPickup>();pickup.interactableName="Repuestos de radio";pickup.UIInteraction=new UIInteraction{label="Guardar en mochila",interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress};Set(pickup,"dispatch",dispatch);var flags=new SerializedObject(pickup);foreach(string f in new[]{"stateDescription","focusInteraction","inspectInteraction","subInteractions"})flags.FindProperty(f).boolValue=false;flags.ApplyModifiedProperties();Set(dispatch,"package",packet);
   Assign(actors.Single(a=>a.name=="NPC - Franco"),Conversation("Franco",ESpeakers.Franco,"Soy Franco. Estamos revisando esta posición.","Con la humedad hay que controlar el equipo más de una vez."));Assign(actors.Single(a=>a.name=="NPC - Pedro"),Conversation("Pedro",ESpeakers.Pedro,"Soy Pedro. Estamos reforzando los refugios de este sector.","Por acá el viento levanta todo. Guardá bien lo que lleves."));
   Set(All<CampRoutine>(scene).Single(),"dispatch",dispatch);Set(All<ObjectiveHud>(scene).Single(),"dispatch",dispatch);Set(All<CheckpointController>(scene).Single(),"dispatch",dispatch);Set(All<RouteBoundary>(scene).Single(),"dispatch",dispatch);
   RegisterSoldiers(scene,new[]{sergeant,nestor,andres,receiver});Map(scene,sergeant,receiver,outpost);ExpandAmbient(scene,new[]{sergeant,nestor,andres,receiver});Views(scene,root.transform,meal.transform,sergeant.transform,receiver.transform);ClearGrass(tents.Append(kitchen.gameObject).ToArray());
   var spawn=All<SpawnArea>(scene).Single();Undo.RecordObject(spawn.transform,"Distant enemy entries");spawn.transform.position=Ground(57,571);var sp=new SerializedObject(spawn);sp.FindProperty("bounds").boundsValue=new Bounds(Vector3.zero,new Vector3(16,12,10));sp.ApplyModifiedProperties();Set(All<ShadowSpawner>(scene).Single(),"spawnGround",terrain);
   Save(scene);FinalizeLayout();Inspect();
  }
  static void Save(Scene scene){foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  static void RegisterSoldiers(Scene scene,SpeakerScript[] added)
  {
   var names=All<KnownSoldierNames>(scene).Single();var data=new SerializedObject(names);var entries=data.FindProperty("soldiers");var template=(TMP_Text)entries.GetArrayElementAtIndex(0).FindPropertyRelative("label").objectReferenceValue;int offset=entries.arraySize;entries.arraySize+=added.Length;
   for(int i=0;i<added.Length;i++){var actor=added[i];var label=UnityEngine.Object.Instantiate(template,template.transform.parent);Undo.RegisterCreatedObjectUndo(label.gameObject,"Known soldier name");label.name="Nombre "+actor.name;var entry=entries.GetArrayElementAtIndex(offset+i);entry.FindPropertyRelative("speaker").objectReferenceValue=actor;entry.FindPropertyRelative("head").objectReferenceValue=actor.GetComponentsInChildren<Animator>().First(a=>a.enabled&&a.isHuman).GetBoneTransform(HumanBodyBones.Head);entry.FindPropertyRelative("label").objectReferenceValue=label;entry.FindPropertyRelative("displayName").stringValue=actor.GetComponentInChildren<NPCInteractableScript>().interactableName;label.gameObject.SetActive(false);}data.ApplyModifiedProperties();
   var cp=new SerializedObject(All<CheckpointController>(scene).Single());var bindings=cp.FindProperty("speakers");offset=bindings.arraySize;bindings.arraySize+=added.Length;for(int i=0;i<added.Length;i++){var binding=bindings.GetArrayElementAtIndex(offset+i);binding.FindPropertyRelative("id").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(added[i]).ToString();binding.FindPropertyRelative("speaker").objectReferenceValue=added[i];}cp.ApplyModifiedProperties();
  }
  static void Map(Scene scene,SpeakerScript sergeant,SpeakerScript receiver,SpeakerScript outpost)
  {
   var map=All<MapZoneOverlay>(scene).Single();var data=new SerializedObject(map);var zones=data.FindProperty("zones");zones.arraySize=7;var positions=new Transform[]{All<SpeakerScript>(scene).Single(a=>a.name.Contains("Diego")).transform,All<SpeakerScript>(scene).Single(a=>a.name.Contains("Mateo")).transform,outpost.transform,New("Referencia del frente",sergeant.transform.parent).transform,All<CampMeal>(scene).Single().transform,sergeant.transform,receiver.transform};positions[3].position=Ground(56,531);string[] labels={"Equipo / Diego","Mateo","Puesto / Rubén","Frente","Cocina","Sargento Ibarra","Radio / Benítez"};
   for(int i=0;i<7;i++){var entry=zones.GetArrayElementAtIndex(i);entry.FindPropertyRelative("name").stringValue=labels[i];entry.FindPropertyRelative("location").objectReferenceValue=positions[i];if(i<5)continue;var first=zones.GetArrayElementAtIndex(0);foreach(string field in new[]{"marker","leader"}){var original=(RectTransform)first.FindPropertyRelative(field).objectReferenceValue;var clone=UnityEngine.Object.Instantiate(original,original.parent);Undo.RegisterCreatedObjectUndo(clone.gameObject,"Dispatch map reference");entry.FindPropertyRelative(field).objectReferenceValue=clone;if(field=="marker")clone.GetComponentInChildren<TMP_Text>().text=(i+1).ToString();}entry.FindPropertyRelative("labelOffset").vector2Value=i==5?new Vector2(-65,-38):new Vector2(65,25);}data.ApplyModifiedProperties();
   var legend=(TMP_Text)data.FindProperty("legend").objectReferenceValue;Undo.RecordObject(legend,"Fit the new destinations");legend.fontSize=21;
  }
  static void ExpandAmbient(Scene scene,SpeakerScript[] added)
  {
   var ambient=All<CampAmbientDialogue>(scene).Single();var data=new SerializedObject(ambient);var voices=data.FindProperty("voices");var actors=All<SpeakerScript>(scene);voices.arraySize=actors.Length;
   for(int i=0;i<actors.Length;i++){var a=actors[i];var voice=voices.GetArrayElementAtIndex(i);voice.FindPropertyRelative("actor").objectReferenceValue=a;voice.FindPropertyRelative("displayName").stringValue=a.GetComponentInChildren<NPCInteractableScript>()?.interactableName??a.name;string[] phrases=a.name.Contains("Cocinero")?new[]{"Acérquense de a uno. Todavía queda algo caliente.","Hoy me acordé del olor de la cocina de mi casa.","No dejen la olla destapada, se enfría enseguida.","Comé despacio. Después hay que seguir.","Ojalá esta noche podamos descansar un rato."}:a.name.Contains("Ibarra")?new[]{"Revisen que los suministros estén cubiertos.","Avísenme si a alguno le falta abrigo.","Necesito que se cuiden entre ustedes.","Mantengan libre el paso entre las carpas.","Un rato de descanso también hace falta."}:a.name.Contains("Benitez")?new[]{"Volvé a probar la conexión. Te escuché entrecortado.","El equipo tiene que quedar protegido de la humedad.","Ojalá hoy podamos mandar noticias.","Voy a revisar otra vez los contactos.","Mi familia debe estar esperando una carta."}:a.name.Contains("Nestor")?new[]{"Otra vez se aflojó esta soga.","Si el viento sigue así, vamos a tener que asegurar la lona.","No siento las manos. Después paso por el fuego.","Mi viejo arreglaba todo con alambre. Ahora me acuerdo de él.","¿Pudiste dormir algo anoche?"}:a.name.Contains("Andres")?new[]{"Esto todavía está seco. Guardalo más adentro.","Tengo la foto de mi hermana acá, bien envuelta.","En casa a esta hora ya estarían poniendo la mesa.","Una carta te cambia el día.","Ayudame con estos cajones cuando termines."}:a.name.Contains("Mateo")?new[]{"Me acuerdo de la letra de mi mamá de memoria.","Qué ganas de sentarme a tomar mate en casa.","El viento no nos deja guardar nada.","¿Escuchaste algo de los demás?", "Voy a quedarme un rato cerca de la cocina."}:a.name.Contains("Jose")?new[]{"La humedad se mete hasta en los bolsillos.","Anoche el viento no nos dejó dormir.","Vi papeles por los cajones. Hay que guardarlos bien.","Mi hermano siempre me pide que le escriba más.","Con algo seco para los pies ya estaría mejor."}:a.name.Contains("Simon")?new[]{"No siento los dedos. Me voy a acercar al fuego.","Mi hermano debe estar volviendo de la escuela.","Ojalá esta noche no llueva otra vez.","¿Te quedó un par de medias secas?", "Guardé un lugar cerca de la carpa."}:AmbientFallback(a.name);var lines=voice.FindPropertyRelative("lines");lines.arraySize=phrases.Length;for(int k=0;k<phrases.Length;k++)lines.GetArrayElementAtIndex(k).stringValue=phrases[k];}data.ApplyModifiedProperties();
  }
  static void Views(Scene scene,Transform root,Transform kitchen,Transform sergeant,Transform receiver)
  {
   var scenic=All<ScenicFocus>(scene).Single();var data=new SerializedObject(scenic);var views=data.FindProperty("views");int offset=views.arraySize;views.arraySize+=3;var places=new[]{Ground(86,276),Ground(69,274),Ground(43,487)};var targets=new[]{kitchen,sergeant,receiver};var captions=new[]{"La cocina del campamento","La carpa de mando","El puesto de radio"};
   for(int i=0;i<3;i++){var entry=New("Mirada del campamento "+i,root).transform;entry.position=places[i];var target=New("Tema de mirada "+i,root).transform;target.position=targets[i].position+Vector3.up*(i==0?.1f:1.2f);var view=views.GetArrayElementAtIndex(offset+i);view.FindPropertyRelative("entrance").objectReferenceValue=entry;view.FindPropertyRelative("target").objectReferenceValue=target;view.FindPropertyRelative("caption").stringValue=captions[i];view.FindPropertyRelative("radius").floatValue=4;}data.ApplyModifiedProperties();
  }
  static string[] AmbientFallback(string name)
  {
   if(name.Contains("Diego"))return new[]{"Acomodá las correas. Después de un rato empiezan a lastimar.","Si a alguno le falta equipo, que me avise.","Voy a revisar lo que queda en la mesa.","Mi viejo siempre decía que había que cuidar las cosas.","Ojalá pudiera mandarles una señal de que estoy bien."};
   if(name.Contains("Ruben"))return new[]{"Franco, fijate que esos cajones queden tapados.","Tenemos que cuidar lo que todavía está seco.","No hace falta que cargues todo solo, pedí una mano.","Mi mamá debe estar escuchando la radio otra vez.","Cuando volvamos, quiero comer sin tener apuro."};
   if(name.Contains("Franco"))return new[]{"Revisé el equipo dos veces. Lo voy a mirar de nuevo.","A esta hora en mi barrio ya estarían jugando a la pelota.","¿Escuchaste algo de los que están más adelante?", "No sabía que se podía extrañar tanto una cama.","Rubén, después te ayudo a acomodar eso."};
   if(name.Contains("Pedro"))return new[]{"Esta lona volvió a aflojarse.","Si juntamos los sacos acá, queda un poco más reparado.","Mi hermano se reiría si me viera remendando esto.","Apenas pueda voy a escribir otra carta.","Sentate un rato si estás cansado."};
   if(name.Contains("Centinela"))return new[]{"Desde acá se escucha el viento distinto.","Estoy mirando el camino. Si viene alguien, les aviso.","Me vendría bien algo caliente para las manos.","Anoche soñé que estaba en casa y no me quería despertar.","Cuando me releven voy a intentar dormir un rato."};
   if(name=="NPC")return new[]{"Rubén, ¿quedó algo seco entre los cajones?", "Me acordé de mi hermana. Siempre me guardaba un lugar en la mesa.","Ojalá hoy lleguen noticias de casa.","Voy a quedarme acá por si hace falta una mano.","Al menos acá el viento pega un poco menos."};
   return new[]{"Todavía tengo tierra en los bolsillos.","¿Te queda un poco de agua?", "Mi familia debe estar esperando que les escriba.","Cuando termine esto quiero volver a caminar por mi barrio.","Si encontrás un lugar reparado, guardame un poco de espacio."};
  }
  public static void RefineContent()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();var root=All<SupplyDispatch>(scene).Single().transform;ExpandAmbient(scene,Array.Empty<SpeakerScript>());
   var after=AssetDatabase.LoadAssetAtPath<ConversationSO>(Data+"/OutpostAfter.asset");var line=new SerializedObject(after.Dialogues[0]);line.FindProperty("_dialogueText").stringValue="Benítez está en el puesto de radio. Yo me quedo revisando estos suministros.";line.ApplyModifiedProperties();EditorUtility.SetDirty(after.Dialogues[0]);
   var letter=All<LetterManager>(scene).Single();var returned=AssetDatabase.LoadAssetAtPath<ConversationSO>(Data+"/LetterReturn.asset");
   if(!returned)
   {
    var source=AssetDatabase.LoadAssetAtPath<ConversationSO>("Assets/Systems/DialogueSystem/ScriptableObjects/Conversations/Mateo/LostLetter/Return.asset");var entry=UnityEngine.Object.Instantiate(source.Dialogues[0]);entry.name="Carta reconstruida - devolución";var ed=new SerializedObject(entry);ed.FindProperty("_dialogueText").stringValue="La reconstruiste... ¿Me la devolvés? Quiero leerla otra vez, tranquilo.";var choice=ed.FindProperty("choices").GetArrayElementAtIndex(0);choice.FindPropertyRelative("label").stringValue="Acá tenés tu carta.";choice.FindPropertyRelative("nextConversation").objectReferenceValue=Conversation("MateoThanks",ESpeakers.Mateo,"Gracias por devolverme la carta. Me hacía falta leer algo de casa.","El sargento Ibarra te está buscando. Está junto a la carpa de mando, en el campamento.");ed.ApplyModifiedProperties();AssetDatabase.CreateAsset(entry,Data+"/LetterReturnLine.asset");returned=ScriptableObject.CreateInstance<ConversationSO>();returned.Dialogues=new[]{entry};var rd=new SerializedObject(returned);rd.FindProperty("_autoNext").boolValue=false;rd.ApplyModifiedProperties();AssetDatabase.CreateAsset(returned,Data+"/LetterReturn.asset");
   }
   Set(letter,"returnConversation",returned);
   if(!All<Transform>(scene).Any(t=>t.name=="Cajón del puesto de radio"))
   {
    var source=All<InteractableScript>(scene).First(i=>i.name=="SmallBox"||i.name=="MediumBox").GetComponentInChildren<MeshFilter>(true);var crate=New("Cajón del puesto de radio",root);crate.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;crate.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;var bounds=BoundsOf(crate);crate.transform.localScale*=.8f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);Seat(crate,Ground(47,499));crate.AddComponent<BoxCollider>();
    var radioSource=All<InteractableScript>(scene).FirstOrDefault(i=>i.name=="Spica");var mesh=radioSource?radioSource.GetComponentInChildren<MeshFilter>(true):null;
    if(mesh){var radio=New("Radio del puesto de Benítez",root);radio.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;radio.AddComponent<MeshRenderer>().sharedMaterials=mesh.GetComponent<Renderer>().sharedMaterials;var b=BoundsOf(radio);radio.transform.localScale*=.4f/Mathf.Max(b.size.x,b.size.y,b.size.z);Seat(radio,new Vector3(crate.GetComponent<Renderer>().bounds.center.x,crate.GetComponent<Renderer>().bounds.max.y,crate.GetComponent<Renderer>().bounds.center.z));}
   }
   ClearGrass(All<MeshRenderer>(scene).Where(r=>r.name=="Mesa de repuestos del puesto"||r.name=="Cajón del puesto de radio").Select(r=>r.gameObject).ToArray());Save(scene);
  }
  static void ClearGrass(GameObject[] objects)
  {
   var data=terrain.terrainData;string backup="Library/CampExpansion-backup/Terrain.asset";if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(data),backup);var areas=objects.Select(BoundsOf).ToArray();int cleared=0;
   for(int layer=0;layer<data.detailPrototypes.Length;layer++){var prototype=data.detailPrototypes[layer];string name=prototype.prototypeTexture?prototype.prototypeTexture.name:prototype.prototype?prototype.prototype.name:"";if(!name.ToLowerInvariant().Contains("grass"))continue;var values=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);int changes=0;for(int z=0;z<data.detailHeight;z++)for(int x=0;x<data.detailWidth;x++){if(values[z,x]==0)continue;var p=terrain.transform.position+new Vector3((x+.5f)/data.detailWidth*data.size.x,0,(z+.5f)/data.detailHeight*data.size.z);foreach(var b in areas)if(p.x>b.min.x-.5f&&p.x<b.max.x+.5f&&p.z>b.min.z-.5f&&p.z<b.max.z+.5f){values[z,x]=0;changes++;break;}}if(changes>0){Undo.RegisterCompleteObjectUndo(data,"Clear grass only under the new camp props");data.SetDetailLayer(0,0,layer,values);cleared+=changes;}}if(cleared>0)EditorUtility.SetDirty(data);File.WriteAllText("Library/CampExpansion-grass.txt",cleared.ToString());
  }
  public static void BakeFront()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();var player=All<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>(scene).Single().transform;var bounds=new Bounds(Ground(75,590),new Vector3(160,130,180));var sources=new System.Collections.Generic.List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new System.Collections.Generic.List<NavMeshBuildMarkup>(),sources);sources.RemoveAll(source=>source.component&&(source.component.transform.IsChildOf(player)||source.component.GetComponentInParent<SpeakerScript>()||PathOf(source.component.transform).Contains("/VFX/")||source.component.name.Contains("Route_")||source.component.name.Contains("MissionGate")||source.component is Collider c&&(c.isTrigger||!c.enabled)));
   var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.45f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;settings.minRegionArea=2;var built=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!built)throw new InvalidOperationException("No front navigation");var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(Data+"/DistantFrontNavigation.asset");if(existing){Undo.RecordObject(existing,"Distant front navigation");EditorUtility.CopySerialized(built,existing);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(built,Data+"/DistantFrontNavigation.asset");existing=built;}Set(All<CombatNavigation>(scene).Single(),"data",existing);Save(scene);
  }
  public static void FinalizeLayout(){RefineContent();RelocateDispatchTable();BakeFront();}
  public static void RelocateDispatchTable()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();var table=All<MeshRenderer>(scene).Single(r=>r.name=="Mesa de repuestos del puesto");var packet=All<RadioPartsPickup>(scene).Single();var fires=All<CampfireMotion>(scene);var center=table.bounds.center;Vector3 chosen=Vector3.zero;bool found=false;
   if(table.GetComponentInChildren<CampfireMotion>())throw new InvalidOperationException("The cloned table contains an unintended campfire");
   for(int ring=4;ring<=7&&!found;ring++)for(int i=0;i<12;i++)
   {
    float angle=i*Mathf.PI/6;var point=Ground(101+Mathf.Cos(angle)*ring,350+Mathf.Sin(angle)*ring);var b=new Bounds(point+Vector3.up*.6f,new Vector3(2.7f,1.2f,2.5f));
    if(fires.Any(f=>new Vector2(f.transform.position.x-point.x,f.transform.position.z-point.z).sqrMagnitude<20.25f))continue;
    if(All<Collider>(scene).Any(c=>c.enabled&&!c.isTrigger&&!(c is TerrainCollider)&&!c.transform.IsChildOf(table.transform)&&!c.transform.IsChildOf(packet.transform)&&!c.name.Contains("Route_")&&!c.name.Contains("MissionGate")&&c.bounds.Intersects(b)))continue;
    chosen=point;found=true;break;
   }
   if(!found)throw new InvalidOperationException("No safe dispatch table position");Seat(table.gameObject,chosen);Undo.RecordObject(packet.transform,"Keep the package on its table");packet.transform.position+=table.bounds.center-center;ClearGrass(new[]{table.gameObject});Save(scene);File.WriteAllText("Library/CampExpansion-table-placement.txt",chosen.ToString());
  }
  public static void Inspect()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var log=new StringBuilder();
   foreach(var renderer in All<MeshRenderer>(scene))if(renderer.name.StartsWith("Tent_")&&renderer.bounds.center.z<320)log.AppendLine("TENT "+PathOf(renderer.transform)+" bounds="+renderer.bounds+" scale="+renderer.transform.lossyScale+" colliders="+renderer.GetComponentsInChildren<Collider>().Length);
   foreach(var actor in All<DialogueSystem.Scripts.SpeakerScript>(scene))log.AppendLine("NPC "+actor.name+" "+actor.transform.position);
   var map=All<MapZoneOverlay>(scene).Single();foreach(var zone in map.Zones)log.AppendLine("MAP "+zone.name+" "+PathOf(zone.location)+" "+zone.location.position);
   foreach(var filter in All<MeshFilter>(scene))if(filter.name.Contains("Caja")||filter.name=="wooden_table (3)"||filter.name=="Mesa de cocina"||filter.name=="MediumBox")log.AppendLine("PROP "+PathOf(filter.transform)+" "+filter.GetComponent<Renderer>().bounds);
   File.WriteAllText("Library/CampExpansion-inspection.txt",log.ToString());
  }
 }
}
#endif
