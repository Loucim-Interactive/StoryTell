#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.DialogueSystem.Scripts;
using Systems.Global;
using Systems.InmersionSystem;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.WalkieSystem.Scripts;
using Systems.WeaponSystem;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class ProgrammingPolishAuthoring
    {
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static void Set(Object target,string field,Object value){var data=new SerializedObject(target);var p=data.FindProperty(field);if(p==null)throw new InvalidOperationException(field);p.objectReferenceValue=value;data.ApplyModifiedProperties();}
        static void Array(Object target,string field,Object[] values){var data=new SerializedObject(target);var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];data.ApplyModifiedProperties();}
        static T Add<T>(GameObject obj)where T:Component=>Undo.AddComponent<T>(obj);
        static GameObject New(string name,Transform parent,bool ui=false){var g=ui?new GameObject(name,typeof(RectTransform)):new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Programming polish");g.transform.SetParent(parent,false);if(ui)g.layer=5;return g;}
        static void Box(Transform t,Vector2 anchor,Vector2 position,Vector2 size){var r=(RectTransform)t;r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;}
        static void Stretch(Transform t){var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static TMP_FontAsset font;
        static void BindNarrations(Scene scene,CheckpointController checkpoints)
        {
            var zones=All<CharacterNarrationZoneScript>(scene);var data=new SerializedObject(checkpoints);var entries=data.FindProperty("narrations");entries.arraySize=zones.Length;
            for(int i=0;i<zones.Length;i++){var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("id").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(zones[i]).ToString();e.FindPropertyRelative("zone").objectReferenceValue=zones[i];}data.ApplyModifiedProperties();
        }
        public static void Finish()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");BindNarrations(scene,All<CheckpointController>(scene).Single());ScaleTexts(scene);SplitSharedAudio(scene);
            foreach(var slider in All<Slider>(scene))if(slider.handleRect){Undo.RecordObject(slider.handleRect,"Separate slider handles");slider.handleRect.sizeDelta=new Vector2(20,0);}
            var spawnData=new SerializedObject(All<CheckpointController>(scene).Single()).FindProperty("safeSpawns");
            for(int i=1;i<spawnData.arraySize;i++){var spawn=(Transform)spawnData.GetArrayElementAtIndex(i).objectReferenceValue;var target=i+1<spawnData.arraySize?(Transform)spawnData.GetArrayElementAtIndex(i+1).objectReferenceValue:null;Undo.RecordObject(spawn,"Face the continuation of the route");Vector3 direction=target?target.position-spawn.position:Vector3.forward;direction.y=0;spawn.rotation=Quaternion.LookRotation(direction);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void SplitSharedAudio(Scene scene)
        {
            var hosts=All<AudioSource>(scene).Select(a=>a.gameObject).Distinct().Where(g=>g.GetComponents<AudioSource>().Length>1).ToArray();
            foreach(var host in hosts)
            {
                var gain=host.GetComponent<ComfortAudioGain>();if(gain)Undo.DestroyObjectImmediate(gain);
                foreach(var source in host.GetComponents<AudioSource>())
                {
                    var child=New("AudioChannel_"+source.GetInstanceID(),host.transform);var copy=Add<AudioSource>(child);EditorUtility.CopySerialized(source,copy);
                    foreach(var component in All<Component>(scene))
                    {
                        if(!component||component==source)continue;var data=new SerializedObject(component);var property=data.GetIterator();bool changed=false;
                        while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==source){property.objectReferenceValue=copy;changed=true;}
                        if(changed){data.ApplyModifiedProperties();if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
                    }
                    Add<ComfortAudioGain>(child);Undo.DestroyObjectImmediate(source);
                }
            }
        }
        static void ScaleTexts(Scene scene)
        {
            foreach(var text in All<TMP_Text>(scene))
            {
                if(text.GetComponent<ComfortTextScale>())continue;var scale=Add<ComfortTextScale>(text.gameObject);var data=new SerializedObject(scale);data.FindProperty("originalSize").floatValue=text.fontSize;data.ApplyModifiedProperties();if(PrefabUtility.IsPartOfPrefabInstance(text))PrefabUtility.RecordPrefabInstancePropertyModifications(scale);
            }
        }
        static TMP_Text Text(string name,Transform parent,string value,float size){var g=New(name,parent,true);var t=Add<TextMeshProUGUI>(g);t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
        static Image Image(GameObject g,Color color){var image=Add<Image>(g);image.color=color;return image;}
        static Button Button(string name,Transform parent,string label,Vector2 position,Vector2 size)
        {
            var g=New(name,parent,true);Box(g.transform,new Vector2(.5f,1),position,size);var image=Image(g,new Color(.77f,.36f,.08f));var button=Add<Button>(g);button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(1,.82f,.52f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
            var text=Text("Label",g.transform,label,25);Stretch(text.transform);text.alignment=TextAlignmentOptions.Center;return button;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<CheckpointController>(scene).Length>0)throw new InvalidOperationException("Already applied.");
            Directory.CreateDirectory("Library/LevelPolishBackups");File.Copy("Assets/Scenes/Level1.unity","Library/LevelPolishBackups/BeforeProgramming-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Add checkpoints, shared UI ownership, options and route states");
            var player=All<PlayerControllerScript>(scene).Single();var root=New("Gameplay_Reliability",null);var session=Add<UiSessionCoordinator>(root);
            var presentation=All<MainPresentation>(scene).Single();var inspection=All<InspectionScript>(scene).Single();var dialogue=All<DialogueManagerScript>(scene).Single();var radio=All<WalkieTalkieScript>(scene).Single();var weapon=All<PlayerWeaponController>(scene).Single();var interaction=All<InteractionScript>(scene).Single();var look=All<CameraControllerScript>(scene).Single();
            Array(session,"movementControls",new Behaviour[]{player,look,interaction,All<ZoomScript>(scene).Single(),radio,All<PlayerWeaponInput>(scene).Single()});
            Array(session,"interactionControls",new Behaviour[]{interaction,All<PlayerWeaponInput>(scene).Single()});
            Set(presentation,"session",session);Set(inspection,"session",session);Set(dialogue,"session",session);Set(radio,"session",session);
            foreach(var old in All<DialoguePlayerLock>(scene)){Undo.RecordObject(old,"Use shared UI owner");old.enabled=false;}
            var progress=Add<LevelProgress>(root);Set(progress,"route",All<RouteBoundary>(scene).Single());
            var objective=All<ObjectiveHud>(scene).Single();Set(objective,"progress",progress);Set(objective,"session",session);
            var checkpoints=Add<CheckpointController>(root);Set(checkpoints,"equipment",All<EquipmentManager>(scene).Single());Set(checkpoints,"letter",All<LetterManager>(scene).Single());Set(checkpoints,"presentation",presentation);Set(checkpoints,"progress",progress);Set(checkpoints,"session",session);Set(checkpoints,"dialogue",dialogue);Set(checkpoints,"radio",All<WalkieInteractionMachine>(scene).Single());Set(checkpoints,"player",player.transform);Set(checkpoints,"feedback",All<MissionAnnouncement>(scene).Single());Set(checkpoints,"objective",objective);Set(checkpoints,"mapPickup",All<MapPickup>(scene).Single().gameObject);
            var settings=new SerializedObject(checkpoints);var actors=All<SpeakerScript>(scene).OrderBy(a=>a.name).ToArray();var entries=settings.FindProperty("speakers");entries.arraySize=actors.Length;
            for(int i=0;i<actors.Length;i++){var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("id").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(actors[i]).ToString();e.FindPropertyRelative("speaker").objectReferenceValue=actors[i];}
            var triggers=All<TriggerZoneScript>(scene);entries=settings.FindProperty("storyTriggers");entries.arraySize=triggers.Length;for(int i=0;i<triggers.Length;i++){var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("id").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(triggers[i]).ToString();e.FindPropertyRelative("trigger").objectReferenceValue=triggers[i];}settings.ApplyModifiedProperties();
            BindNarrations(scene,checkpoints);
            var terrain=All<Terrain>(scene).Single();var cc=player.GetComponent<CharacterController>();float baseOffset=cc.center.y-cc.height*.5f;
            Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y-baseOffset+.08f,z);
            var points=new[]{new Vector2(85,279),new Vector2(44,365),new Vector2(72,405),new Vector2(56,512)};var spawns=new Transform[4];
            for(int i=0;i<4;i++)
            {
                var spawn=New("Checkpoint_"+(RouteStage)i,root.transform);spawn.transform.position=Ground(points[i].x,points[i].y);spawn.transform.rotation=player.transform.rotation;spawns[i]=spawn.transform;
                if(i==0)continue;
                var zone=New("RouteStage_"+(RouteStage)i,root.transform);zone.transform.position=Ground(points[i].x,points[i].y)+Vector3.up;var box=Add<BoxCollider>(zone);box.isTrigger=true;box.size=i==1?new Vector3(26,5,20):new Vector3(18,5,6);var component=Add<RouteProgressZone>(zone);Set(component,"progress",progress);Set(component,"player",player.transform);var data=new SerializedObject(component);data.FindProperty("stage").enumValueIndex=i;data.ApplyModifiedProperties();
            }
            Array(checkpoints,"safeSpawns",spawns);
            for(int i=1;i<spawns.Length;i++){Vector3 direction=i+1<spawns.Length?spawns[i+1].position-spawns[i].position:Vector3.forward;direction.y=0;spawns[i].rotation=Quaternion.LookRotation(direction);}
            var performance=Add<RuntimePerformanceMonitor>(root);Set(performance,"session",session);Set(performance,"progress",progress);
            var pause=Add<PauseMenu>(root);Set(checkpoints,"pause",pause);Set(pause,"session",session);Set(pause,"checkpoints",checkpoints);Set(pause,"performance",performance);Set(pause,"presentation",presentation);Set(pause,"inspection",inspection);Set(pause,"dialogue",dialogue);Set(pause,"radio",radio);
            BuildPause(scene,pause);
            var applier=Add<ComfortSettingsApplier>(root);Set(applier,"look",look);var dataFonts=new SerializedObject(applier);entries=dataFonts.FindProperty("texts");var texts=All<TMP_Text>(scene);entries.arraySize=texts.Length;for(int i=0;i<texts.Length;i++){var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("text").objectReferenceValue=texts[i];e.FindPropertyRelative("size").floatValue=texts[i].fontSize;}dataFonts.ApplyModifiedProperties();
            ScaleTexts(scene);
            SplitSharedAudio(scene);
            var dialogueAudio=(AudioSource)new SerializedObject(All<UIDialogueScript>(scene).Single()).FindProperty("_audio").objectReferenceValue;
            foreach(var source in All<AudioSource>(scene))
            {
                if(source.GetComponent<ComfortAudioGain>())continue;var gain=Add<ComfortAudioGain>(source.gameObject);var data=new SerializedObject(gain);data.FindProperty("voice").boolValue=source==dialogueAudio||source.name=="RadioConversationAudio";data.ApplyModifiedProperties();
            }
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Library/Programming-authoring.txt",$"Created shared UI owner, 3 route zones and 4 safe checkpoints. NPC bindings: {actors.Length}; story triggers: {triggers.Length}; audio channels: {All<ComfortAudioGain>(scene).Length}. Player capsule height={cc.height}, radius={cc.radius}, center={cc.center}.");
        }
        static void BuildPause(Scene scene,PauseMenu pause)
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");var safe=All<CanvasSafeArea>(scene).Single().transform;
            var overlay=New("PauseAndOptions",safe,true);Stretch(overlay.transform);Image(overlay,new Color(.015f,.025f,.035f,.95f));
            var content=New("Content",overlay.transform,true);Box(content.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1000,1000));
            var title=Text("Title",content.transform,"PAUSA",38);title.color=new Color(1,.62f,.2f);Box(title.transform,new Vector2(.5f,1),new Vector2(0,-42),new Vector2(940,65));title.alignment=TextAlignmentOptions.Center;
            var resume=Button("Continue",content.transform,"Continuar",new Vector2(-245,-115),new Vector2(460,65));var restart=Button("Restart",content.transform,"Reiniciar checkpoint",new Vector2(245,-115),new Vector2(460,65));var fresh=Button("NewGame",content.transform,"Nueva partida",new Vector2(245,-115),new Vector2(460,65));fresh.gameObject.SetActive(false);
            var labels=new[]{"Sensibilidad del mouse","Tamaño del texto","Movimiento de cámara","Oscurecimiento al disparar","Volumen general","Voces y radio","Efectos y ambiente"};var sliders=new Slider[7];var values=new TMP_Text[7];
            for(int i=0;i<7;i++)
            {
                float y=-220-i*84;var label=Text("OptionLabel_"+i,content.transform,labels[i],25);Box(label.transform,new Vector2(0,1),new Vector2(240,y),new Vector2(440,60));
                var g=New("Option_"+i,content.transform,true);Box(g.transform,new Vector2(.5f,1),new Vector2(170,y),new Vector2(360,42));var background=Image(g,new Color(.15f,.19f,.22f));var slider=Add<Slider>(g);slider.minValue=i==0?.2f:i==1?.85f:0;slider.maxValue=i==0?3:i==1?1.25f:1;slider.value=1;
                var fill=New("Fill",g.transform,true);Stretch(fill.transform);var fillImage=Image(fill,new Color(.85f,.46f,.13f));fillImage.raycastTarget=false;slider.fillRect=(RectTransform)fill.transform;
                var handle=New("Handle",g.transform,true);Box(handle.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,0));slider.targetGraphic=Image(handle,Color.white);slider.handleRect=(RectTransform)handle.transform;sliders[i]=slider;
                values[i]=Text("Value_"+i,content.transform,"",23);Box(values[i].transform,new Vector2(1,1),new Vector2(-52,y),new Vector2(100,60));values[i].alignment=TextAlignmentOptions.MidlineRight;
            }
            var export=Button("ExportPerformance",content.transform,"Guardar informe de rendimiento",new Vector2(0,-845),new Vector2(700,58));UnityEventTools.AddPersistentListener(export.onClick,pause.ExportPerformance);
            var status=Text("Status",content.transform,"",23);Box(status.transform,new Vector2(.5f,1),new Vector2(0,-930),new Vector2(940,100));status.alignment=TextAlignmentOptions.Center;
            Set(pause,"panel",overlay);Set(pause,"title",title);Set(pause,"status",status);Set(pause,"continueLabel",resume.GetComponentInChildren<TMP_Text>());Set(pause,"continueButton",resume);Set(pause,"restartButton",restart);Set(pause,"newGameButton",fresh);Array(pause,"sliders",sliders);Array(pause,"values",values);overlay.SetActive(false);
        }
    }
}
#endif
