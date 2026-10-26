#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DialogueSystem.Scripts;
using Systems.Missions;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using StoryTell.PhysicalMap;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.WeaponSystem;
using Systems.WalkieSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class MainIntegrationAuthoring
    {
        public const string ScenePath="Assets/Scenes/Level1.unity";
        const string Data="Assets/Systems/MainIntegration/Data";
        static TMP_FontAsset bodyFont, headingFont;
        [Serializable]public sealed class Baseline {public UIEntry[] ui;public PositionEntry[] positions;}
        [Serializable]public sealed class UIEntry {public string id,path,font;public Vector2 anchorMin,anchorMax,pivot,size,position;public Vector3 scale;public float fontSize;public Color color;}
        [Serializable]public sealed class PositionEntry {public string id,name;public Vector3 position,scale;public Quaternion rotation;}
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        public static void FinishPolish()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var presentation=All<MainPresentation>(scene).Single();var instructions=All<TextMesh>(scene).FirstOrDefault(t=>t.name=="Example Instructions");if(instructions)Set(presentation,"combatInstructions",instructions.gameObject);foreach(var doc in All<InspectableDocument>(scene).Where(d=>d.name.StartsWith("MateoLetterFragment"))){Undo.RecordObject(doc,"Keep the title readable in main style");doc.title="Carta de Lanús · "+doc.name.Last()+"/4";EditorUtility.SetDirty(doc);}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=EditorSceneManager.OpenScene(ScenePath);if(All<MainPresentation>(scene).Any())throw new InvalidOperationException("Main integration already authored.");
            Directory.CreateDirectory("Library/MainIntegrationBackups");File.Copy(ScenePath,"Library/MainIntegrationBackups/Level1-before-apply-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();
            var canvas=All<Canvas>(scene).Single();var missions=All<MissionsUIManager>(scene).Single();var missionData=new SerializedObject(missions);
            var template=(TMP_Text)missionData.FindProperty("itemLabel").objectReferenceValue;var titleTemplate=(RectTransform)missionData.FindProperty("titleTemplate").objectReferenceValue;
            bodyFont=template.font;headingFont=titleTemplate.GetComponentInChildren<TMP_Text>(true).font;
            var baseline=new Baseline{ui=canvas.GetComponentsInChildren<RectTransform>(true).Select(r=>{var t=r.GetComponent<TMP_Text>();return new UIEntry{id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),path=PathOf(r),anchorMin=r.anchorMin,anchorMax=r.anchorMax,pivot=r.pivot,size=r.sizeDelta,position=r.anchoredPosition,scale=r.localScale,font=t?AssetDatabase.GetAssetPath(t.font):"",fontSize=t?t.fontSize:0,color=t?t.color:Color.clear};}).ToArray(),positions=All<Transform>(scene).Where(t=>t.GetComponent<InteractableScript>()||t.GetComponent<SpeakerScript>()||t.GetComponent<PlayerControllerScript>()||t.GetComponent<Systems.BattleSystem.SpawnArea>()).Select(t=>new PositionEntry{id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),name=t.name,position=t.position,rotation=t.rotation,scale=t.localScale}).ToArray()};
            File.WriteAllText(Data+"/MainUIBaseline.json",JsonUtility.ToJson(baseline,true));
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Extend main gameplay while preserving its UI and route");
            var player=All<PlayerControllerScript>(scene).Single();var inspection=player.GetComponent<InspectionScript>();var camera=player.GetComponentInChildren<Camera>();
            var root=new GameObject("MainGameplay_AdditionalFeatures");Undo.RegisterCreatedObjectUndo(root,"Create main extensions");var presentation=Undo.AddComponent<MainPresentation>(root);
            var ui=UI("MainExtensionsUI",canvas.transform);Stretch(ui.transform);
            Undo.AddComponent<ReferenceCanvasSpace>(ui).Fit();
            Set(presentation,"inspection",inspection);Set(presentation,"dialogue",All<Systems.DialogueSystem.Scripts.DialogueManagerScript>(scene).Single());Set(presentation,"letter",All<LetterManager>(scene).Single());Set(presentation,"missions",missions);Set(presentation,"movement",player);Set(presentation,"look",player.GetComponent<CameraControllerScript>());Set(presentation,"interaction",player.GetComponent<InteractionScript>());Set(presentation,"radio",player.GetComponent<WalkieTalkieScript>());Set(presentation,"weapon",player.GetComponent<PlayerWeaponController>());
            var parts=new SerializedObject(presentation);var fragmentTexts=parts.FindProperty("letterParts");fragmentTexts.arraySize=4;for(int i=0;i<4;i++)fragmentTexts.GetArrayElementAtIndex(i).stringValue=FragmentText(i);parts.ApplyModifiedProperties();
            var actions=ScriptableObject.CreateInstance<InputActionAsset>();var gameplay=new InputActionMap("MainExtensions");gameplay.AddAction("Interact",InputActionType.Button,"<Keyboard>/e");gameplay.AddAction("CloseInspection",InputActionType.Button,"<Keyboard>/e");actions.AddActionMap(gameplay);AssetDatabase.CreateAsset(actions,Data+"/MainExtensionsInput.asset");
            InputActionReference Reference(string action){var reference=InputActionReference.Create(actions.FindAction(action));reference.name=action;AssetDatabase.CreateAsset(reference,Data+"/"+action+".asset");return reference;}
            Set(player.GetComponent<InteractionScript>(),"interactAction",Reference("Interact"));Undo.RecordObject(inspection,"Keep inspection close independent from world interaction");inspection.inspectAction=Reference("CloseInspection");
            var documents=All<LetterInteractableScript>(scene).ToArray();
            foreach(var item in documents)
            {
                var doc=Undo.AddComponent<InspectableDocument>(item.gameObject);doc.title=item.name.StartsWith("MateoLetterFragment")?"Carta de Lanús · "+item.name.Last()+"/4":"Carta desde las islas";
                doc.transcription=item.name.StartsWith("MateoLetterFragment")?FragmentText(item.name.Last()-'1'):"28 de mayo, Islas Malvinas\n\nMamá:\n¿Cómo están allá? Acá todo bien, no se preocupen. Hace frío, pero entre todos nos arreglamos.\nContame cómo está el perro, si extraña que no esté. Los quiero mucho, decile a papá que se…\n\n[El fragmento termina aquí.]";
                doc.inspectionSize=0.43f;doc.inspectionOffset=new Vector3(-0.18f,0,0);doc.inspectionEulerAngles=new Vector3(-65,0,0);EditorUtility.SetDirty(doc);SetBool(item,"inspectInteraction",true);SetBool(item,"focusInteraction",false);SetBool(item,"subInteractions",true);
            }
            foreach(var item in All<InspectableInteractableScript>(scene).Where(i=>i.name.StartsWith("Diario1")))
            {
                var doc=Undo.AddComponent<InspectableDocument>(item.gameObject);doc.title="Crónica · 2 de abril de 1982";doc.transcription="Transcripción de los titulares visibles del recurso:\n\nARGENTINAZO:\n¡LAS MALVINAS RECUPERADAS!\n\nACCIÓN CONJUNTA DE NUESTRAS FUERZAS ARMADAS; MARCHAN AVIONES Y BARCOS EN GRAN OPERATIVO; EL TIEMPO CONSPIRA.\n\nCrónica · Firme junto al pueblo\nViernes 2 de abril de 1982\nEdición dominical $6.000\nRecargo vía aérea $100\n\nSe conservan las palabras del periódico histórico. El resto de la página se puede observar en el objeto; esta transcripción no reproduce las columnas completas.";doc.inspectionSize=0.43f;doc.inspectionOffset=new Vector3(-0.18f,0,0);EditorUtility.SetDirty(doc);
            }
            foreach(var npc in All<NPCInteractableScript>(scene)){SetBool(npc,"inspectInteraction",false);SetBool(npc,"subInteractions",false);SetBool(npc,"focusInteraction",false);}
            foreach(var equipment in All<Systems.InteractionSystem.Scripts.Interactables.Equipment.EquipmentInteractableScript>(scene)){Undo.RecordObject(equipment,"Finish inspection before collecting native equipment");equipment.equipAfterInspection=true;}
            foreach(var speaker in All<SpeakerScript>(scene)){var facing=Undo.AddComponent<NpcConversationFacing>(speaker.gameObject);Set(speaker,"conversationFacing",facing);}
            var held=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Maximo/Interactables/Cartas/CartaLanus/Carta3.fbx"),root.transform);Undo.RegisterCreatedObjectUndo(held,"Keep the recovered letter available for optional reading");held.name="Carta_Reconstruida_Consulta";var full=Undo.AddComponent<InspectableDocument>(held);full.title="Carta de Lanús · 20 de abril";full.transcription=string.Join("\n\n",Enumerable.Range(0,4).Select(FragmentText));full.hideAfterInspection=true;full.inspectionSize=0.43f;EditorUtility.SetDirty(full);held.SetActive(false);Set(presentation,"completedLetter",full);
            BuildReader(ui.transform,inspection,titleTemplate.gameObject);
            var map=BuildMap(root.transform,ui.transform,presentation,player.transform,camera,titleTemplate.gameObject);Set(presentation,"map",map);
            BuildPuzzle(ui.transform,presentation,titleTemplate.gameObject);
            BuildWeaponUI(ui.transform,player.GetComponent<PlayerWeaponController>(),camera,titleTemplate.gameObject);
            var hint=UI("LetterAssemblyHint",ui.transform);Anchor(hint.transform,new Vector2(0.5f,0),new Vector2(0,60),new Vector2(900,56));var hintText=Text("Label",hint.transform,"",24);Stretch(hintText.transform);hintText.alignment=TextAlignmentOptions.Center;Set(presentation,"assemblyHint",hint);Set(presentation,"assemblyHintText",hintText);hint.SetActive(false);
            var announcement=UI("MissionAnnouncement",ui.transform);Anchor(announcement.transform,new Vector2(0.5f,1),new Vector2(0,-58),new Vector2(760,60));var header=Header("NewMission",announcement.transform,titleTemplate.gameObject);var component=Undo.AddComponent<MissionAnnouncement>(ui);Set(component,"missions",missions);Set(component,"panel",announcement);Set(component,"label",header);announcement.SetActive(false);
            var freeze=new MonoBehaviour[]{player,player.GetComponent<CameraControllerScript>(),player.GetComponent<InteractionScript>(),player.GetComponent<ZoomScript>(),player.GetComponent<PlayerWeaponInput>(),player.GetComponent<WalkieTalkieScript>(),map};SetArray(inspection,"onOffScripts",freeze);
            foreach(var item in All<Component>(scene))if(item&&PrefabUtility.IsPartOfPrefabInstance(item))PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();File.WriteAllText("Library/MainIntegration-applied.txt","Native UI and route preserved; document reading, puzzle, map and conversation facing installed.");
        }
        public static string FragmentText(int index)
        {
            string[] text={"Lanús, 20 de abril\nQuerido Marcos:\n\nNo sabés la alegría que nos dio cuando el cartero trajo tu carta. La leí cuatro veces y se la leí a toda la cuadra. Doña Pepa me dijo que prendiera una vela a la Virgen y ya la tengo prendida desde que te fuiste.","Acá no se habla de otra cosa. En la tele pasan marchas y… [pasaje tachado en el recurso]. Tu papá es de callar, y ya sabés cómo es, pero anoche lo vi sentado en tu cuarto, a oscuras; de vez en cuando arregla nuevamente el foquito de la mesa de luz.","Te mando las medias que te tejí, la yerba y unos alfajores. No sé si te van a llegar, pero lo mando igual. Comé todo lo que te den y abrigate, no te hagas el guapo.","Lo único que pedimos todos los días es que vuelvas.\n\nTe lo manda con cariño,\nTu mamá"};return text[index];
        }
        static void BuildReader(Transform parent,InspectionScript inspection,GameObject titleTemplate)
        {
            var host=UI("DocumentTextReader",parent);Stretch(host.transform);var reader=Undo.AddComponent<DocumentReader>(host);
            var panel=UI("ReadingPanel",host.transform);Rect(panel.transform,new Vector2(0.59f,0.13f),new Vector2(0.98f,0.91f),Vector2.zero,Vector2.zero);Background(panel);
            var header=Header("DocumentTitle",panel.transform,titleTemplate);Rect(header.transform.parent,new Vector2(0,1),Vector2.one,new Vector2(0,-80),Vector2.zero);
            var viewport=UI("TextViewport",panel.transform);Rect(viewport.transform,Vector2.zero,Vector2.one,new Vector2(26,26),new Vector2(-26,-100));Undo.AddComponent<RectMask2D>(viewport);
            var body=Text("PlainText",viewport.transform,"",26);Rect(body.transform,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);body.rectTransform.pivot=new Vector2(0.5f,1);body.richText=false;body.overflowMode=TextOverflowModes.Overflow;var fit=Undo.AddComponent<ContentSizeFitter>(body.gameObject);fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=Undo.AddComponent<ScrollRect>(panel);scroll.viewport=(RectTransform)viewport.transform;scroll.content=body.rectTransform;scroll.horizontal=false;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var hint=UI("ReadingControls",host.transform);Anchor(hint.transform,new Vector2(0.5f,0),new Vector2(0,60),new Vector2(1600,80));var controls=Text("Controls",hint.transform,"",23);Stretch(controls.transform);controls.alignment=TextAlignmentOptions.Center;
            Set(reader,"inspection",inspection);Set(reader,"panel",panel);Set(reader,"hint",hint);Set(reader,"title",header);Set(reader,"body",body);Set(reader,"controls",controls);Set(reader,"scroll",scroll);panel.SetActive(false);hint.SetActive(false);
        }
        static PhysicalMapController BuildMap(Transform root,Transform ui,MainPresentation presentation,Transform player,Camera camera,GameObject titleTemplate)
        {
            var mapObj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/PhysicalMapSystem/Prefabs/PhysicalMap.prefab"),root);Undo.RegisterCreatedObjectUndo(mapObj,"Add physical map without replacing native mission items");mapObj.name="Main_PhysicalMap";
            var map=mapObj.GetComponent<PhysicalMapController>();var settings=Object.Instantiate(AssetDatabase.LoadAssetAtPath<MapSettings>("Assets/Systems/PhysicalMapSystem/Data/SO_MapSettings_Level1.asset"));settings.name="SO_MapSettings_Main";AssetDatabase.CreateAsset(settings,Data+"/SO_MapSettings_Main.asset");map.Configure(settings,camera.transform);Set(map,"toggleMapAction",AssetDatabase.LoadAssetAtPath<InputActionReference>("Assets/Systems/PhysicalMapSystem/Data/Level1_ToggleMap.asset"));
            map.GetComponent<PaperDeformationController>().RebuildMesh(settings);var filter=map.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);AssetDatabase.CreateAsset(mesh,Data+"/Map_Main_Mesh.asset");filter.sharedMesh=mesh;map.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/PhysicalMapSystem/Materials/Level1PaperMap.mat");
            var origin=new GameObject("MainMapWorldOrigin");Undo.RegisterCreatedObjectUndo(origin,"Set geographic map reference");origin.transform.SetParent(root,false);origin.transform.position=new Vector3(73.5f,0,433);
            var reference=mapObj.GetComponent<MapWorldReference>();reference.SetWorldOrigin(origin.transform);reference.SetWorldSize(new Vector2(564,376));
            foreach(var marker in mapObj.GetComponentsInChildren<MapMarkerController>(true)){marker.Configure(reference,map,player,false);marker.SetVisible(marker.name.Contains("Player"));}
            int layer=LayerMask.NameToLayer("MainPhysicalMap");if(layer<0){var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layer=Enumerable.Range(10,22).First(i=>string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));Undo.RecordObject(tags.targetObject,"Add an unused layer for the map without changing Equipment");layers.GetArrayElementAtIndex(layer).stringValue="MainPhysicalMap";tags.ApplyModifiedProperties();}
            foreach(var t in mapObj.GetComponentsInChildren<Transform>(true)){Undo.RecordObject(t.gameObject,"Separate paper from Equipment layer");t.gameObject.layer=layer;}
            var view=new GameObject("Main_PhysicalMapCamera");Undo.RegisterCreatedObjectUndo(view,"Render held paper without world clipping");view.transform.SetParent(camera.transform,false);var paperCamera=Undo.AddComponent<Camera>(view);paperCamera.CopyFrom(camera);paperCamera.clearFlags=CameraClearFlags.Depth;paperCamera.cullingMask=1<<layer;paperCamera.nearClipPlane=0.01f;paperCamera.enabled=false;paperCamera.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Overlay;var cameraSettings=new SerializedObject(paperCamera.GetUniversalAdditionalCameraData());cameraSettings.FindProperty("m_ClearDepth").boolValue=true;cameraSettings.ApplyModifiedProperties();paperCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.GetUniversalAdditionalCameraData().cameraStack.Add(paperCamera);Undo.RecordObject(camera,"Exclude only the new map layer");camera.cullingMask&=~(1<<layer);
            var viewController=map.GetComponent<PhysicalMapViewCamera>()??Undo.AddComponent<PhysicalMapViewCamera>(mapObj);viewController.Configure(map,camera,paperCamera);
            var pickup=GameObject.CreatePrimitive(PrimitiveType.Cube);Undo.RegisterCreatedObjectUndo(pickup,"Place an optional map near native equipment");pickup.name="Mapa_Consulta_Main";pickup.transform.SetParent(root,false);pickup.transform.position=new Vector3(90.1f,29.18f,260.6f);pickup.transform.localScale=new Vector3(0.28f,0.006f,0.21f);pickup.layer=6;pickup.GetComponent<Collider>().isTrigger=true;var item=Undo.AddComponent<MapPickup>(pickup);item.interactableName="Mapa del sector";item.UIInteraction=new Systems.InteractionSystem.Scripts.Utils.UIInteraction{label="Guardar mapa",interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress};SetBool(item,"inspectInteraction",false);SetBool(item,"focusInteraction",false);SetBool(item,"stateDescription",false);SetBool(item,"subInteractions",false);Set(item,"presentation",presentation);
            var legend=UI("MapLegend",ui);Rect(legend.transform,new Vector2(0.70f,0.20f),new Vector2(0.98f,0.77f),Vector2.zero,Vector2.zero);Background(legend);Header("MapTitle",legend.transform,titleTemplate).text="MAPA DEL SECTOR";var body=Text("Zones",legend.transform,"1 · Equipamiento / Diego\n2 · Mateo / carta\n3 · Puesto avanzado\n4 · Frente\n\nPunto celeste: tu ubicación\n[M] Guardar mapa\n[L] Carta, al recuperar las 4 partes",23);Rect(body.transform,Vector2.zero,Vector2.one,new Vector2(22,20),new Vector2(-22,-82));Set(presentation,"mapLegend",legend);legend.SetActive(false);
            var scene=player.gameObject.scene;var transforms=All<Transform>(scene);Transform Named(string name)=>transforms.Single(t=>t.name==name);
            var locations=new[]{Named("NPC - Diego (equipamiento)"),Named("NPC - Mateo (carta perdida)"),Named("NPC - Pedro"),Named("Shadow Spawn Area")};var host=UI("MainMapZones",ui);Stretch(host.transform);var overlay=Undo.AddComponent<MapZoneOverlay>(host);var content=UI("Numbers",host.transform);Stretch(content.transform);
            var entries=new SerializedObject(overlay);entries.FindProperty("map").objectReferenceValue=map;entries.FindProperty("reference").objectReferenceValue=reference;entries.FindProperty("mapCamera").objectReferenceValue=paperCamera;entries.FindProperty("overlay").objectReferenceValue=content;var zones=entries.FindProperty("zones");zones.arraySize=4;
            for(int i=0;i<4;i++){var marker=UI("Zone_"+(i+1),content.transform);Anchor(marker.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(28,28));var image=Undo.AddComponent<Image>(marker);image.color=new Color(0.14f,0.12f,0.09f);image.raycastTarget=false;var number=Text("Number",marker.transform,(i+1).ToString(),21);Stretch(number.transform);number.alignment=TextAlignmentOptions.Center;var leader=UI("Leader_"+(i+1),content.transform);Anchor(leader.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(10,2));var stroke=Undo.AddComponent<Image>(leader);stroke.color=new Color(0.14f,0.12f,0.09f);stroke.raycastTarget=false;var zone=zones.GetArrayElementAtIndex(i);zone.FindPropertyRelative("name").stringValue=locations[i].name;zone.FindPropertyRelative("location").objectReferenceValue=locations[i];zone.FindPropertyRelative("marker").objectReferenceValue=marker.transform;zone.FindPropertyRelative("leader").objectReferenceValue=leader.transform;zone.FindPropertyRelative("labelOffset").vector2Value=new Vector2(i%2==0?-30:30,i<2?-18:18);}
            entries.ApplyModifiedProperties();content.SetActive(false);return map;
        }
        static void BuildPuzzle(Transform parent,MainPresentation controller,GameObject titleTemplate)
        {
            var host=UI("LetterReconstruction",parent);Stretch(host.transform);var puzzle=Undo.AddComponent<LetterAssemblyPuzzle>(host);var panel=UI("PuzzlePanel",host.transform);Stretch(panel.transform);Background(panel);Header("PuzzleHeader",panel.transform,titleTemplate).text="RECONSTRUIR LA CARTA";
            var board=UI("Board",panel.transform);Anchor(board.transform,new Vector2(0.5f,0.53f),Vector2.zero,new Vector2(720,500));Background(board);var slots=new RectTransform[4];var pieces=new LetterPuzzlePiece[4];
            for(int i=0;i<4;i++){var slot=UI("Slot_"+i,board.transform);Anchor(slot.transform,Vector2.one*0.5f,new Vector2(i%2==0?-180:180,i/2==0?125:-125),new Vector2(360,250));slots[i]=(RectTransform)slot.transform;var piece=UI("Fragment_"+i,panel.transform);Anchor(piece.transform,new Vector2(i%2==0?0.12f:0.88f,i/2==0?0.69f:0.33f),Vector2.zero,new Vector2(360,250));piece.transform.localRotation=Quaternion.Euler(0,0,new[]{90,-90,180,0}[i]);pieces[i]=Undo.AddComponent<LetterPuzzlePiece>(piece);pieces[i].slotIndex=i;pieces[i].owner=puzzle;pieces[i].color=new Color(0.94f,0.89f,0.76f);var ink=Text("Text",piece.transform,FragmentText(i),21);Stretch(ink.transform,22);ink.color=new Color(0.12f,0.10f,0.08f);ink.enableAutoSizing=true;ink.fontSizeMin=17;ink.fontSizeMax=21;ink.overflowMode=TextOverflowModes.Ellipsis;}
            var status=Text("PuzzleControls",panel.transform,"",24);Anchor(status.transform,new Vector2(0.5f,0),new Vector2(0,80),new Vector2(1500,120));status.alignment=TextAlignmentOptions.Center;Set(puzzle,"controller",controller);Set(puzzle,"panel",panel);Set(puzzle,"board",board.transform);Set(puzzle,"status",status);SetArray(puzzle,"slots",slots);SetArray(puzzle,"pieces",pieces);Set(controller,"puzzle",puzzle);panel.SetActive(false);
            var eventSystem=All<EventSystem>(controller.gameObject.scene).Single();foreach(var legacy in eventSystem.GetComponents<StandaloneInputModule>()){Undo.RecordObject(legacy,"Use Input System for puzzle pointers");legacy.enabled=false;}var module=eventSystem.GetComponent<InputSystemUIInputModule>()??Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);if(!module.actionsAsset)module.AssignDefaultActions();var canvas=parent.GetComponentInParent<Canvas>();if(!canvas.GetComponent<GraphicRaycaster>())Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
        }
        static void BuildWeaponUI(Transform parent,PlayerWeaponController weapon,Camera camera,GameObject titleTemplate)
        {
            var panel=UI("NativeWeaponAmmo",parent);Anchor(panel.transform,new Vector2(1,1),new Vector2(-160,-120),new Vector2(250,170));Background(panel);Header("WeaponTitle",panel.transform,titleTemplate).text="FN FAL";var label=Text("Ammo",panel.transform,"",24);Rect(label.transform,Vector2.zero,Vector2.one,new Vector2(18,14),new Vector2(-18,-70));Set(weapon,"ammoPanel",panel);Set(weapon,"ammoLabel",label);Set(weapon,"firingCamera",camera);panel.SetActive(false);
            var vignetteObject=UI("WeaponPeripheralStress",parent);Stretch(vignetteObject.transform);var vignette=Undo.AddComponent<WeaponVignetteGraphic>(vignetteObject);vignette.raycastTarget=false;Set(weapon,"vignette",vignette);
            var soundObject=new GameObject("NativeFALAudio");Undo.RegisterCreatedObjectUndo(soundObject,"Reuse provisional FAL sound");soundObject.transform.SetParent(weapon.transform,false);var audio=Undo.AddComponent<AudioSource>(soundObject);audio.playOnAwake=false;audio.spatialBlend=0;Set(weapon,"shotAudio",audio);Set(weapon,"shotClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/WeaponSystem/Data/FAL_TestShot.wav"));
        }
        static GameObject UI(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Add UI matching main");obj.transform.SetParent(parent,false);obj.layer=5;return obj;}
        static void Background(GameObject obj){var image=Undo.AddComponent<Image>(obj);image.color=new Color(0.025f,0.025f,0.025f,0.94f);image.raycastTarget=false;}
        static TMP_Text Header(string name,Transform parent,GameObject template){var clone=Object.Instantiate(template,parent);clone.name=name;Undo.RegisterCreatedObjectUndo(clone,"Reuse main title styling");clone.SetActive(true);Rect(clone.transform,new Vector2(0,1),Vector2.one,new Vector2(0,-64),Vector2.zero);return clone.GetComponentInChildren<TMP_Text>(true);}
        static TMP_Text Text(string name,Transform parent,string value,float size){var obj=UI(name,parent);var text=Undo.AddComponent<TextMeshProUGUI>(obj);text.font=bodyFont;text.fontSharedMaterial=bodyFont.material;text.fontSize=size;text.color=Color.white;text.margin=Vector4.zero;text.text=value;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;}
        static void Stretch(Transform transform,float margin=0){Rect(transform,Vector2.zero,Vector2.one,Vector2.one*margin,-Vector2.one*margin);}
        static void Rect(Transform transform,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){var r=(RectTransform)transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;}
        static void Anchor(Transform transform,Vector2 anchor,Vector2 position,Vector2 size){var r=(RectTransform)transform;r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*0.5f;r.anchoredPosition=position;r.sizeDelta=size;}
        static void Set(Object target,string field,Object value){var data=new SerializedObject(target);var property=data.FindProperty(field);if(property==null)throw new InvalidOperationException(target.name+" missing "+field);property.objectReferenceValue=value;data.ApplyModifiedProperties();}
        static void SetBool(Object target,string field,bool value){var data=new SerializedObject(target);data.FindProperty(field).boolValue=value;data.ApplyModifiedProperties();}
        static void SetArray(Object target,string field,Object[] values){var data=new SerializedObject(target);var property=data.FindProperty(field);property.arraySize=values.Length;for(int i=0;i<values.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=values[i];data.ApplyModifiedProperties();}
        public static void Inspect()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var report=new StringBuilder();
            foreach(var root in scene.GetRootGameObjects())report.AppendLine("ROOT "+root.name);
            foreach(var component in All<Component>(scene).Where(c=>c is SpeakerScript||c is EquipmentManager||c is LetterManager||c is MissionsUIManager||c is InspectionScript||c is Systems.BattleSystem.BattleManager||c is Systems.BattleSystem.ShadowSpawner||c is Systems.BattleSystem.SpawnArea||c is Systems.WeaponSystem.PlayerWeaponController))
                report.AppendLine($"COMPONENT {component.GetType().Name} {PathOf(component.transform)} pos={component.transform.position}\n{EditorJsonUtility.ToJson(component,true)}");
            foreach(var interaction in All<InteractableScript>(scene))report.AppendLine($"INTERACTION {interaction.GetType().Name} {PathOf(interaction.transform)} pos={interaction.transform.position} inspect={interaction.InspectInteractable} ui={JsonUtility.ToJson(interaction.UIInteraction)}");
            foreach(var canvas in All<Canvas>(scene))report.AppendLine($"CANVAS {PathOf(canvas.transform)} mode={canvas.renderMode} active={canvas.gameObject.activeInHierarchy}");
            foreach(var text in All<TMP_Text>(scene))report.AppendLine($"TEXT {PathOf(text.transform)} font={text.font?.name} size={text.fontSize} color={text.color} rect={text.rectTransform.rect} text={text.text}");
            File.WriteAllText("Library/MainIntegration-audit.txt",report.ToString());
        }
    }
}
#endif
