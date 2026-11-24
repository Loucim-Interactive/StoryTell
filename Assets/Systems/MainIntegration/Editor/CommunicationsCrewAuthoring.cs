#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using DialogueSystem.Scripts;
using StoryTell.MainIntegration;
using StoryTell.MainIntegration.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CommunicationsCrewAuthoring
{
 const string Data="Assets/Systems/MainIntegration/Data/CommunicationsCrew";
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Seat(GameObject g,Vector3 point){var b=BoundsOf(g);g.transform.position+=new Vector3(point.x-b.center.x,point.y+.012f-b.min.y,point.z-b.center.z);}
 static GameObject Empty(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Communications crew and equipment");g.transform.SetParent(parent,false);return g;}
 static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(m,Data+"/"+name+".mat");return m;}
 static GameObject Shape(string name,Transform root,PrimitiveType type,Vector3 point,Vector3 scale,Material material)
 {
  var g=GameObject.CreatePrimitive(type);Undo.RegisterCreatedObjectUndo(g,"Communications detail");g.name=name;g.transform.SetParent(root,true);g.transform.position=point;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;Undo.DestroyObjectImmediate(g.GetComponent<Collider>());return g;
 }
 static ConversationSO Conversation(string name,ESpeakers who,string[] words)
 {
  var c=ScriptableObject.CreateInstance<ConversationSO>();c.Dialogues=new DialogueSO[words.Length];for(int i=0;i<words.Length;i++){var line=ScriptableObject.CreateInstance<DialogueSO>();var d=new SerializedObject(line);d.FindProperty("_speaker").enumValueIndex=(int)who;d.FindProperty("_dialogueText").stringValue=words[i];d.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/"+name+i+".asset");c.Dialogues[i]=line;}var data=new SerializedObject(c);data.FindProperty("_autoNext").boolValue=false;data.ApplyModifiedProperties();AssetDatabase.CreateAsset(c,Data+"/"+name+".asset");return c;
 }
 static void Voice(Scene s,SpeakerScript actor,string display,string[] words)
 {
  var d=new SerializedObject(All<CampAmbientDialogue>(s).Single());var voices=d.FindProperty("voices");int i=voices.arraySize;voices.arraySize++;var entry=voices.GetArrayElementAtIndex(i);entry.FindPropertyRelative("actor").objectReferenceValue=actor;entry.FindPropertyRelative("displayName").stringValue=display;var lines=entry.FindPropertyRelative("lines");lines.arraySize=words.Length;for(int k=0;k<words.Length;k++)lines.GetArrayElementAtIndex(k).stringValue=words[k];d.ApplyModifiedProperties();
 }
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<SpeakerScript>(s).Any(a=>a.name=="NPC - Luis"||a.name=="NPC - Ernesto"))throw new InvalidOperationException("Already applied");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Populate camp communications");
  var terrain=All<Terrain>(s).Single();Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);var root=All<Transform>(s).Single(t=>t.name=="Comunicaciones del campamento");var source=All<SpeakerScript>(s).Single(a=>a.name=="NPC - Simon");var author=typeof(CampExpansionAuthoring);author.GetField("terrain",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,terrain);var create=author.GetMethod("Soldier",BindingFlags.Static|BindingFlags.NonPublic);
  var luis=(SpeakerScript)create.Invoke(null,new object[]{source,root,"Luis","Luis",Ground(64,288)});var ernesto=(SpeakerScript)create.Invoke(null,new object[]{source,root,"Ernesto","Ernesto",Ground(60.5f,286.5f)});var added=new[]{luis,ernesto};foreach(var actor in added){actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(Ground(63,286.5f)-actor.transform.position,Vector3.up));var state=new SerializedObject(actor);state.FindProperty("campRoutine").objectReferenceValue=null;state.FindProperty("_canRestart").boolValue=true;state.ApplyModifiedProperties();}
  luis.SetConversation(Conversation("Luis",ESpeakers.Luis,new[]{"Soy Luis. Estoy al lado de la radio, tratando de mantener el enlace con los otros puestos.","Si una palabra se pierde entre los ruidos, pedimos que la repitan. Prefiero preguntar otra vez antes que pasar un mensaje mal.","Cuando tengo un rato libre, pienso en lo que le escribiría a mi familia. Acá una noticia de casa te cambia el día."}));
  ernesto.SetConversation(Conversation("Ernesto",ESpeakers.Ernesto,new[]{"Soy Ernesto. Me encargo de revisar los cables y las baterías de este puesto.","La humedad nos obliga a revisar los contactos. Dejamos los repuestos a mano y los mensajes anotados en la libreta.","Mi viejo arreglaba radios en casa. Nunca pensé que iba a extrañar tanto verlo trabajar en silencio."}));
  author.GetMethod("RegisterSoldiers",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{s,added});
  Voice(s,luis,"Luis",new[]{"Probá otra vez. Esa última parte no se entendió.","Anotá la hora al lado del mensaje, por favor.","Un momento... creo que vuelve la señal.","Ojalá pudiera escuchar un rato la voz de mi vieja.","No quiero pasar una palabra equivocada."});
  Voice(s,ernesto,"Ernesto",new[]{"Este contacto todavía está húmedo.","Dejé una batería de repuesto junto a la mesa.","Cuidado con ese cable, que no quede tirante.","Mi viejo siempre decía que primero había que revisar lo más simple.","Después de esta revisión me acerco un rato al fuego."});
  var table=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de comunicaciones");var dark=Mat("HeadphonesAndCable",new Color(.045f,.055f,.06f));var paper=Mat("MessagePaper",new Color(.57f,.58f,.49f));var cover=Mat("NotebookCover",new Color(.18f,.23f,.20f));var wood=Mat("CableReelFlanges",new Color(.21f,.18f,.14f));float top=table.bounds.max.y;
  var notebook=Empty("Libreta de mensajes - provisional",root);Shape("Tapa de libreta",notebook.transform,PrimitiveType.Cube,new Vector3(62.15f,top+.025f,286.65f),new Vector3(.30f,.025f,.21f),cover);Shape("Hojas de libreta",notebook.transform,PrimitiveType.Cube,new Vector3(62.15f,top+.041f,286.65f),new Vector3(.28f,.008f,.19f),paper);for(int i=0;i<5;i++)Shape("Renglón "+i,notebook.transform,PrimitiveType.Cube,new Vector3(62.15f,top+.046f,286.59f+i*.025f),new Vector3(.21f,.001f,.003f),dark);
  var phones=Empty("Auriculares de radio - provisionales",root);for(int i=0;i<10;i++){float a=i*Mathf.PI/9;var band=Shape("Arco "+i,phones.transform,PrimitiveType.Cube,new Vector3(63.65f+Mathf.Cos(a)*.13f,top+.023f,286.55f+Mathf.Sin(a)*.13f),new Vector3(.052f,.026f,.028f),dark);band.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}for(int i=0;i<2;i++)Shape("Almohadilla "+i,phones.transform,PrimitiveType.Cylinder,new Vector3(63.65f+(i==0?-.13f:.13f),top+.037f,286.54f),new Vector3(.08f,.028f,.10f),dark);
  var reel=Empty("Bobina de cable - provisional",root);var ground=Ground(60.7f,288.5f);Shape("Cable enrollado",reel.transform,PrimitiveType.Cylinder,ground+Vector3.up*.18f,new Vector3(.54f,.15f,.54f),dark);foreach(float height in new[]{.03f,.34f})Shape("Disco de bobina",reel.transform,PrimitiveType.Cylinder,ground+Vector3.up*height,new Vector3(.68f,.025f,.68f),wood);
  var points=new[]{ground+new Vector3(.22f,.06f,0),Ground(61.4f,288.5f)+Vector3.up*.025f,Ground(61.6f,287.4f)+Vector3.up*.025f,new Vector3(62,top+.04f,286.6f)};for(int i=1;i<points.Length;i++){var segment=Shape("Cable de repuesto "+i,root,PrimitiveType.Cube,(points[i-1]+points[i])*.5f,new Vector3(.015f,.015f,Vector3.Distance(points[i-1],points[i])),dark);segment.transform.rotation=Quaternion.LookRotation(points[i]-points[i-1]);}
  var bench=UnityEngine.Object.Instantiate(table.gameObject,root);Undo.RegisterCreatedObjectUndo(bench,"Auxiliary receiver bench");bench.name="Mesa del receptor auxiliar";Seat(bench,Ground(66.5f,290));var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Outsourced/Props/Defensive_props/models/radio.fbx");if(!model)throw new InvalidOperationException("Existing radio model missing");var receiver=(GameObject)PrefabUtility.InstantiatePrefab(model,root);Undo.RegisterCreatedObjectUndo(receiver,"Existing radio model");receiver.name="Receptor auxiliar";var bounds=BoundsOf(receiver);receiver.transform.localScale*=.65f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);Seat(receiver,new Vector3(66.5f,bench.GetComponent<Renderer>().bounds.max.y,290));
  var converted=new Dictionary<Material,Material>();foreach(var renderer in receiver.GetComponentsInChildren<Renderer>()){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){var original=materials[i];if(!original)continue;if(!converted.TryGetValue(original,out var replacement)){replacement=Mat("ReceiverMaterial"+converted.Count,new Color(.82f,.88f,.9f));var texture=original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.HasProperty("_MainTex")?original.GetTexture("_MainTex"):null;if(texture)replacement.SetTexture("_BaseMap",texture);converted.Add(original,replacement);}materials[i]=replacement;}renderer.sharedMaterials=materials;}
  Collisions(s);foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);File.WriteAllText("Library/CommunicationsCrew-applied.txt","Added Luis and Ernesto, registered names/checkpoints, 10 ambient lines, notebook, headphones, cable reel and existing radio model.\nSoldiers: "+All<SpeakerScript>(s).Length);
 }
 static void Collisions(Scene s)
 {
  foreach(string name in new[]{"Bobina de cable - provisional","Receptor auxiliar"})
  {
   var target=All<Transform>(s).Single(t=>t.name==name);if(target.GetComponentInChildren<Collider>())continue;var b=BoundsOf(target.gameObject);var hit=Empty("Volumen de "+name,target);hit.transform.position=b.center;hit.transform.rotation=Quaternion.identity;hit.transform.localScale=new Vector3(1/target.lossyScale.x,1/target.lossyScale.y,1/target.lossyScale.z);hit.AddComponent<BoxCollider>().size=b.size;
  }
 }
 public static void FinalizeCollisions(){var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");Collisions(s);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}
 public static void RefineOperatorClearance()
 {
  var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var ground=All<Terrain>(s).Single();var actor=All<SpeakerScript>(s).Single(a=>a.name=="NPC - Luis");Undo.RecordObject(actor.transform,"Clear auxiliary table corner");var old=actor.transform.position;var point=new Vector3(64,old.y,288);point.y+=ground.SampleHeight(point)-ground.SampleHeight(old);actor.transform.position=point;actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(new Vector3(63,point.y,286.5f)-point,Vector3.up));foreach(var c in actor.GetComponentsInChildren<Component>())if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
 }
}
#endif
