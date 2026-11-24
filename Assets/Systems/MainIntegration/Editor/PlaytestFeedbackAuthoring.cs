#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using Systems.InspectionSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class PlaytestFeedbackAuthoring
{
 const string Data="Assets/Systems/MainIntegration/Data/PlaytestFeedback";
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static void Set(UnityEngine.Object o,string field,UnityEngine.Object value){var d=new SerializedObject(o);d.FindProperty(field).objectReferenceValue=value;d.ApplyModifiedProperties();}
 static void Rect(RectTransform r,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){Undo.RecordObject(r,"Readable playtest interface");r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
 static GameObject UI(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(g,"Readable map legend");g.transform.SetParent(parent,false);return g;}
 static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,string value){var g=UI(name,parent);var t=g.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.text=value;t.raycastTarget=false;t.color=Color.white;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;return t;}
 static ConversationSO Conversation(string name,ESpeakers speaker,string text)
 {
  var path=Data+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<ConversationSO>(path);if(existing)return existing;
  var line=ScriptableObject.CreateInstance<DialogueSO>();var d=new SerializedObject(line);d.FindProperty("_speaker").enumValueIndex=(int)speaker;d.FindProperty("_dialogueText").stringValue=text;d.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/"+name+"Line.asset");
  var conversation=ScriptableObject.CreateInstance<ConversationSO>();conversation.Dialogues=new[]{line};var c=new SerializedObject(conversation);c.FindProperty("_autoNext").boolValue=false;c.ApplyModifiedProperties();AssetDatabase.CreateAsset(conversation,path);return conversation;
 }
 static void Line(string path,string value){var line=AssetDatabase.LoadAssetAtPath<DialogueSO>(path);if(!line)throw new InvalidOperationException(path);var d=new SerializedObject(line);d.FindProperty("_dialogueText").stringValue=value;d.ApplyModifiedProperties();EditorUtility.SetDirty(line);}
 public static void PreserveMenuArtProportions()
 {
  var report=new System.Text.StringBuilder();foreach(var name in new[]{"CampaignNotebook","MainMenuBackground","MalvinasPhoto"})
  {
   var path="Assets/Systems/MainIntegration/Menu/Resources/StoryMenus/Images/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);report.AppendLine(name+" "+image.width+"x"+image.height);
  }
  File.WriteAllText("Library/PlaytestFeedback-menu-art.txt",report.ToString());
 }
 public static void FixMateoOffer()
 {
  Line("Assets/Systems/MainIntegration/Data/ExperienceRevision/MateoOffer_2.asset","Preguntales a Simón y José. Cuando tengas los cuatro pedazos, unilos con cuidado y traeme la carta.");AssetDatabase.SaveAssets();
 }
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
  var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<Transform>(s).Any(t=>t.name=="MapLegend_Legacy"))throw new InvalidOperationException("Already applied");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fix playtest guidance and map readability");
  var routine=All<CampRoutine>(s).Single();var diego=All<SpeakerScript>(s).Single(a=>a.name.Contains("Diego"));Set(diego,"campRoutine",routine);
  Set(routine,"beforeEquipment",Conversation("MateoBeforeEquipment",ESpeakers.Marcos,"Primero hablá con Diego y recogé tu equipo. Está junto a la mesa de equipamiento."));Set(routine,"beforeBriefing",Conversation("MateoBeforeBriefing",ESpeakers.Marcos,"Ya tenés el equipo. Pasá por Diego antes de venir al comedor."));Set(routine,"beforeFood",Conversation("MateoBeforeMeal",ESpeakers.Marcos,"Comé algo en la mesa de Máximo, acá al lado. Después charlamos."));Set(routine,"diegoAfterFood",Conversation("DiegoAfterMeal",ESpeakers.Diego,"Ya tenés el equipo y comiste. Si necesitás ubicarte, consultá el mapa."));
  Line("Assets/Systems/DialogueSystem/ScriptableObjects/Dialogues/Diego/Equipment/Completed.asset","Ya tenés el equipo. Andá al comedor, entre las carpas del sector oeste, y comé en la mesa de Máximo.");
  Line("Assets/Systems/DialogueSystem/ScriptableObjects/Dialogues/Mateo/LostLetter/Accepted.asset","Gracias, che. Son cuatro pedazos, por el suelo de este sector. Si los encontrás, ayudame a unirlos y devolveme la carta. Es lo único que tengo de mi vieja acá.");
  var inspection=All<InspectionControls>(s).Single();var help=(TMP_Text)new SerializedObject(inspection).FindProperty("label").objectReferenceValue;Undo.RecordObject(help,"Explain held click rotation");help.fontSize=28;help.enableAutoSizing=false;help.alignment=TextAlignmentOptions.Center;var helpPanel=UI("InspectionControlsPanel",help.transform.parent);Rect((RectTransform)helpPanel.transform,new Vector2(.06f,.025f),new Vector2(.94f,.125f),Vector2.zero,Vector2.zero);Undo.SetTransformParent(help.transform,helpPanel.transform,"Readable inspection panel");Rect(help.rectTransform,Vector2.zero,Vector2.one,new Vector2(12,6),new Vector2(-12,-6));var helpBackground=helpPanel.AddComponent<Image>();helpBackground.color=new Color(.025f,.035f,.04f,.88f);helpBackground.raycastTarget=false;Set(inspection,"panel",helpPanel);help.gameObject.SetActive(true);helpPanel.SetActive(false);
  var map=All<PhysicalMapController>(s).Single();var settings=map.Settings;Undo.RecordObject(settings,"Fit readable map in its screen area");settings.localHoldPosition=new Vector3(-.18f,-.005f,.62f);settings.width=.66f;settings.height=.44f;EditorUtility.SetDirty(settings);
  map.GetComponent<PaperDeformationController>().RebuildMesh(settings);var mesh=UnityEngine.Object.Instantiate(map.GetComponent<MeshFilter>().sharedMesh);mesh.name="PlaytestReadableMap";AssetDatabase.CreateAsset(mesh,Data+"/MapMesh.asset");map.GetComponent<MeshFilter>().sharedMesh=mesh;
  foreach(var marker in map.GetComponentsInChildren<MapMarkerController>(true).Where(m=>m.IsVisible)){var d=new SerializedObject(marker);var visual=(Transform)d.FindProperty("markerVisual").objectReferenceValue;if(!visual)visual=marker.transform;Undo.RecordObject(visual,"Legible player map dot");visual.localScale*=1.5f;}
  var overlay=All<MapZoneOverlay>(s).Single();var data=new SerializedObject(overlay);var old=(TMP_Text)data.FindProperty("legend").objectReferenceValue;var oldRoot=old.transform.parent;var font=old.font;oldRoot.gameObject.name="MapLegend_Legacy";oldRoot.gameObject.SetActive(false);
  var panel=UI("MapLegend",oldRoot.parent);Rect((RectTransform)panel.transform,new Vector2(.715f,.12f),new Vector2(.975f,.88f),Vector2.zero,Vector2.zero);var bg=panel.AddComponent<Image>();bg.color=new Color(.025f,.035f,.045f,.95f);bg.raycastTarget=false;
  var header=UI("Header",panel.transform);Rect((RectTransform)header.transform,new Vector2(0,.88f),Vector2.one,Vector2.zero,Vector2.zero);var bar=header.AddComponent<Image>();bar.color=new Color(.96f,.43f,.08f);bar.raycastTarget=false;var title=Text("MapTitle",header.transform,font,30,"MAPA DEL SECTOR");title.color=Color.black;title.alignment=TextAlignmentOptions.Center;Rect(title.rectTransform,Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-12,0));
  var body=Text("Zones",panel.transform,font,27,"");body.alignment=TextAlignmentOptions.TopLeft;Rect(body.rectTransform,Vector2.zero,new Vector2(1,.88f),new Vector2(22,20),new Vector2(-22,-20));Set(overlay,"legend",body);Set(All<MainPresentation>(s).Single(),"mapLegend",panel);panel.SetActive(false);
  foreach(var zone in overlay.Zones){Undo.RecordObject(zone.marker,"Readable destination label");zone.marker.sizeDelta=new Vector2(38,38);var number=zone.marker.GetComponentInChildren<TMP_Text>();Undo.RecordObject(number,"Readable destination number");number.fontSize=25;number.enableAutoSizing=false;}
  foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);File.WriteAllText("Library/PlaytestFeedback-applied.txt","Dialogue gates, meal instruction, inspection help, map fit and legend applied.");
 }
}
#endif
