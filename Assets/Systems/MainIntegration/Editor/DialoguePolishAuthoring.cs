#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DialogueSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Animations;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.DialogueSystem.Scripts;
using Systems.DecisionSystem.UI;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.Missions;
using Systems.Missions.FindObjects;
using Systems.WalkieSystem.Scripts;
using StoryTell.PhysicalMap;
using Object=UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class DialoguePolishAuthoring
    {
        public const string Data="Assets/Systems/MainIntegration/Data/DialoguePolish";
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        [Serializable] public sealed class Placement {public string id,name;public Vector3 before,after;}
        [Serializable] public sealed class Placements {public Placement[] actors;}
        static TMP_FontAsset body,bold;
        static T Ensure<T>(GameObject obj)where T:Component{var c=obj.GetComponent<T>();return c?c:Undo.AddComponent<T>(obj);}
        static void Set(Object target,string field,Object value){var data=new SerializedObject(target);var p=data.FindProperty(field);if(p==null)throw new InvalidOperationException(field);p.objectReferenceValue=value;data.ApplyModifiedProperties();}
        static void Array(Object target,string field,Object[] values){var data=new SerializedObject(target);var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];data.ApplyModifiedProperties();}
        static GameObject UI(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Readable UI");obj.layer=5;obj.transform.SetParent(parent,false);return obj;}
        static void Rect(Transform transform,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){var r=(RectTransform)transform;Undo.RecordObject(r,"Responsive layout");r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;}
        static void Box(Transform t,Vector2 anchor,Vector2 position,Vector2 size){var r=(RectTransform)t;Undo.RecordObject(r,"Responsive layout");r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
        static TMP_Text Text(string name,Transform parent,string value,float size){var g=UI(name,parent);var t=Undo.AddComponent<TextMeshProUGUI>(g);t.font=body;t.fontSize=size;t.text=value;t.color=Color.white;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
        static void Background(GameObject g,float alpha){var image=Ensure<Image>(g);Undo.RecordObject(image,"Readable text contrast");image.color=new Color(.025f,.03f,.035f,alpha);image.raycastTarget=false;}
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<CanvasSafeArea>(scene).Any())throw new InvalidOperationException("Already applied.");
            Directory.CreateDirectory(Data);Directory.CreateDirectory("Library/LevelPolishBackups");File.Copy("Assets/Scenes/Level1.unity","Library/LevelPolishBackups/BeforeDialogue-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");AssetDatabase.Refresh();
            body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");bold=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Bold SDF.asset");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Readable dialogue and grounded NPCs");
            var canvas=All<Canvas>(scene).Single();var scaler=canvas.GetComponent<CanvasScaler>();Undo.RecordObject(scaler,"Scale UI with the screen");scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            var children=canvas.transform.Cast<Transform>().ToArray();var safe=UI("SafeArea",canvas.transform);Rect(safe.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Undo.AddComponent<CanvasSafeArea>(safe);
            foreach(var child in children){Undo.SetTransformParent(child,safe.transform,"Respect the display safe area");child.localScale=Vector3.one;}
            foreach(var text in safe.GetComponentsInChildren<TMP_Text>(true))
            {Undo.RecordObject(text,"Consistent Inter Tight typography");if(!text.font||!text.font.name.StartsWith("InterTight"))text.font=text.name.Contains("Name")?bold:body;}
            var ui=All<UIDialogueScript>(scene).Single();var uiData=new SerializedObject(ui);var narration=(TMP_Text)uiData.FindProperty("_dialogueTextMesh").objectReferenceValue;var name=(TMP_Text)uiData.FindProperty("_nameTextMesh").objectReferenceValue;var dialogueRoot=narration.transform.parent;
            Rect(dialogueRoot,new Vector2(.1f,0),new Vector2(.9f,0),new Vector2(0,28),new Vector2(0,262));Background(dialogueRoot.gameObject,.86f);
            var groupUI=Ensure<CanvasGroup>(dialogueRoot.gameObject);groupUI.alpha=0;groupUI.blocksRaycasts=false;Set(ui,"panel",groupUI);Set(ui,"_defaultFont",body);
            Rect(narration.transform,Vector2.zero,Vector2.one,new Vector2(28,52),new Vector2(-28,-58));Undo.RecordObject(narration,"Paginated readable subtitles");narration.font=body;narration.fontSize=30;narration.enableAutoSizing=false;narration.margin=Vector4.zero;narration.alignment=TextAlignmentOptions.TopLeft;narration.textWrappingMode=TextWrappingModes.Normal;narration.overflowMode=TextOverflowModes.Page;
            Rect(name.transform,new Vector2(0,1),Vector2.one,new Vector2(28,-44),new Vector2(-28,-10));Undo.RecordObject(name,"Correct negative-height name label");name.font=bold;name.fontSize=26;name.alignment=TextAlignmentOptions.MidlineLeft;name.color=new Color(1,.68f,.25f);name.margin=Vector4.zero;
            var controls=Text("DialogueControls",dialogueRoot,"",22);Rect(controls.transform,Vector2.zero,new Vector2(1,0),new Vector2(28,12),new Vector2(-28,42));controls.color=new Color(.78f,.8f,.82f);Set(ui,"controls",controls);
            BuildChoices(scene,safe.transform);GroundNpcs(scene);
            var player=All<PlayerControllerScript>(scene).Single();var dialogue=All<DialogueManagerScript>(scene).Single();var playerLock=Undo.AddComponent<DialoguePlayerLock>(player.gameObject);Set(playerLock,"dialogue",dialogue);Array(playerLock,"controls",new Behaviour[]{player,player.GetComponent<InteractionScript>(),player.GetComponent<CameraControllerScript>(),player.GetComponent<ZoomScript>(),player.GetComponent<WalkieTalkieScript>(),player.GetComponent<DialogueScript>()});
            BuildObjective(scene,safe.transform,player.transform);
            var missionsRoot=safe.transform.Find("MissionsUI");if(missionsRoot){Box(missionsRoot,new Vector2(0,1),new Vector2(264,-516),new Vector2(480,640));LayoutMissions(scene);}
            var interaction=safe.transform.Find("InteractionUI");if(interaction){Box(interaction,new Vector2(.5f,0),new Vector2(0,318),new Vector2(760,100));var label=interaction.Find("Name");if(label){Rect(label,new Vector2(0,.4f),Vector2.one,Vector2.zero,Vector2.zero);var t=label.GetComponent<TMP_Text>();t.fontSize=28;}var action=interaction.Find("Action");if(action){Rect(action,Vector2.zero,new Vector2(1,.4f),Vector2.zero,Vector2.zero);action.GetComponent<TMP_Text>().fontSize=24;}}
            var info=safe.transform.Find("InteractionInfoUI");if(info){Box(info,new Vector2(.5f,0),new Vector2(0,460),new Vector2(900,180));foreach(var text in info.GetComponentsInChildren<TMP_Text>(true)){Rect(text.transform,Vector2.zero,Vector2.one,new Vector2(18,12),new Vector2(-18,-12));text.fontSize=26;text.textWrappingMode=TextWrappingModes.Normal;}}
            foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            ui.CleanTexts();Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void LayoutMissions(Scene scene)
        {
            var manager=All<MissionsUIManager>(scene).Single();var data=new SerializedObject(manager);var title=(RectTransform)data.FindProperty("titleTemplate").objectReferenceValue;var items=(RectTransform)data.FindProperty("itemsTemplate").objectReferenceValue;var row=(RectTransform)data.FindProperty("itemTemplate").objectReferenceValue;var label=(TMP_Text)data.FindProperty("itemLabel").objectReferenceValue;
            Box(title,new Vector2(0,1),Vector2.zero,new Vector2(480,54));title.pivot=new Vector2(0,1);var heading=title.GetComponentInChildren<TMP_Text>(true);Rect(heading.transform,Vector2.zero,Vector2.one,new Vector2(16,8),new Vector2(-16,-8));heading.fontSize=25;
            Box(items,new Vector2(0,1),new Vector2(12,-70),new Vector2(456,0));items.pivot=new Vector2(0,1);Box(row,new Vector2(.5f,1),Vector2.zero,new Vector2(456,38));
            var layout=Ensure<VerticalLayoutGroup>(items.gameObject);layout.childControlWidth=true;layout.childControlHeight=false;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.spacing=6;layout.padding=new RectOffset();
            Rect(label.transform,Vector2.zero,Vector2.one,new Vector2(38,0),Vector2.zero);label.fontSize=24;label.font=body;var mark=(GameObject)data.FindProperty("itemCompletedMark").objectReferenceValue;
            if(mark){Rect(mark.transform,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(30,0));var text=mark.GetComponent<TMP_Text>();if(text){text.font=bold;text.fontSize=24;text.text=bold.HasCharacter('✓')?"✓":"X";}}
        }
        static void BuildChoices(Scene scene,Transform safe)
        {
            var presenter=All<WalkieUIScript>(scene).Single();var data=new SerializedObject(presenter);var root=(GameObject)data.FindProperty("choicesPanelRoot").objectReferenceValue;var content=(GameObject)data.FindProperty("choicesContent").objectReferenceValue;
            var decisions=safe.Find("DecisionsUI");Rect(decisions,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Rect(decisions.Find("Main"),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            if(!root)root=decisions.Find("Main/ChoicesPanel").gameObject;if(!content)content=root.transform.Find("Content").gameObject;
            var radio=All<WalkieTalkieScript>(scene).Single();var machine=Ensure<WalkieInteractionMachine>(radio.gameObject);Set(radio,"_stateMachine",machine);Set(machine,"dialogueManager",All<DialogueManagerScript>(scene).Single());var audioObject=new GameObject("RadioConversationAudio");Undo.RegisterCreatedObjectUndo(audioObject,"Separate caller audio from player effects");audioObject.transform.SetParent(radio.transform,false);var audio=Undo.AddComponent<AudioSource>(audioObject);audio.playOnAwake=false;Set(machine,"callerAudioSource",audio);
            Set(presenter,"choicesPanelRoot",root);Set(presenter,"choicesContent",content);Set(presenter,"_root",Ensure<CanvasGroup>(decisions.gameObject));Set(presenter,"_state",machine);Set(presenter,"walkieTalkie",radio);Set(presenter,"decisionManager",All<DecisionManagerScript>(scene).Single());Set(presenter,"choicesRadioIcon",root.transform.Find("WalkieIcon").gameObject);
            if(!data.FindProperty("indicatorPanel").objectReferenceValue)Set(presenter,"indicatorPanel",decisions.Find("Main/IndicatorWalkieIcon").gameObject);if(!data.FindProperty("timerPanel").objectReferenceValue)Set(presenter,"timerPanel",decisions.Find("Countdown").gameObject);
            Rect(root.transform,new Vector2(.50f,.31f),new Vector2(.95f,.80f),Vector2.zero,Vector2.zero);Background(root,.87f);
            var viewport=UI("ChoiceViewport",root.transform);Rect(viewport.transform,Vector2.zero,Vector2.one,new Vector2(24,68),new Vector2(-24,-22));Undo.AddComponent<RectMask2D>(viewport);
            Undo.SetTransformParent(content.transform,viewport.transform,"Scrollable long responses");var r=(RectTransform)content.transform;Undo.RecordObject(r,"Fit response rows");r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.anchoredPosition=Vector2.zero;r.sizeDelta=Vector2.zero;r.localScale=Vector3.one;
            foreach(var layout in content.GetComponents<LayoutGroup>())if(!(layout is VerticalLayoutGroup)){Undo.RecordObject(layout,"Use vertical choices");layout.enabled=false;}
            var vertical=Ensure<VerticalLayoutGroup>(content);vertical.childControlWidth=true;vertical.childControlHeight=true;vertical.childForceExpandWidth=true;vertical.childForceExpandHeight=false;vertical.spacing=10;vertical.padding=new RectOffset();
            var fit=Ensure<ContentSizeFitter>(content);fit.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=Ensure<ScrollRect>(root);scroll.content=r;scroll.viewport=(RectTransform)viewport.transform;scroll.horizontal=false;scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            var hint=root.transform.Find("Hint");if(hint){foreach(Transform child in hint)Undo.RecordObject(child.gameObject,"Replace misplaced instruction labels");foreach(Transform child in hint)child.gameObject.SetActive(false);Rect(hint,new Vector2(0,0),new Vector2(1,0),new Vector2(24,12),new Vector2(-24,58));var text=Text("ChoiceControls",hint,"Tab / ↑↓ Elegir · Espacio / Enter Responder\nTambién podés hacer clic · Esc Salir",20);Rect(text.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);}
            var line=root.transform.Find("Side bar");if(line)Rect(line,new Vector2(0,0),new Vector2(0,1),Vector2.zero,new Vector2(4,0));var icon=root.transform.Find("WalkieIcon");if(icon)Box(icon,Vector2.one,new Vector2(-34,-34),new Vector2(44,44));
            var indicator=(GameObject)data.FindProperty("indicatorPanel").objectReferenceValue;if(indicator)Box(indicator.transform,new Vector2(1,1),new Vector2(-70,-65),new Vector2(58,58));var timer=(GameObject)data.FindProperty("timerPanel").objectReferenceValue;if(timer)Rect(timer.transform,new Vector2(.2f,.27f),new Vector2(.8f,.27f),new Vector2(0,-5),new Vector2(0,5));
            var source=(GameObject)data.FindProperty("choicePrefab").objectReferenceValue;string sourcePath=AssetDatabase.GetAssetPath(source);var prefab=PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                var rect=(RectTransform)prefab.transform;rect.sizeDelta=new Vector2(780,64);var hit=Ensure<Image>(prefab);hit.color=Color.clear;hit.raycastTarget=true;var button=prefab.GetComponent<WalkieDecisionButton>();var buttonData=new SerializedObject(button);var text=(TMP_Text)buttonData.FindProperty("_textMesh").objectReferenceValue;text.font=bold;text.fontSize=26;text.enableAutoSizing=false;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;Rect(text.transform,Vector2.zero,Vector2.one,new Vector2(18,10),new Vector2(-18,-10));var selected=(GameObject)buttonData.FindProperty("_selectedBackground").objectReferenceValue;Rect(selected.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var copy=PrefabUtility.SaveAsPrefabAsset(prefab,Data+"/MainResponse.prefab");Set(presenter,"choicePrefab",copy);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var manager=All<DecisionManagerScript>(scene).Single();var settings=new SerializedObject(manager);settings.FindProperty("useActions").boolValue=false;settings.ApplyModifiedProperties();
        }
        static void BuildObjective(Scene scene,Transform safe,Transform player)
        {
            var g=UI("CurrentObjective",safe);Box(g.transform,new Vector2(0,1),new Vector2(264,-95),new Vector2(480,150));Background(g,.62f);
            var title=Text("ObjectiveTitle",g.transform,"OBJETIVO ACTUAL",22);title.font=bold;title.color=new Color(1,.68f,.25f);Rect(title.transform,new Vector2(0,1),Vector2.one,new Vector2(16,-34),new Vector2(-16,-8));var label=Text("ObjectiveText",g.transform,"Hablá con Diego",24);Rect(label.transform,Vector2.zero,Vector2.one,new Vector2(16,10),new Vector2(-16,-38));
            var controller=Undo.AddComponent<ObjectiveHud>(safe.gameObject);Set(controller,"equipment",All<EquipmentManager>(scene).Single());Set(controller,"letter",All<LetterManager>(scene).Single());Set(controller,"player",player);Set(controller,"map",All<PhysicalMapController>(scene).Single());Set(controller,"inspection",All<InspectionScript>(scene).Single());Set(controller,"dialogue",All<DialogueManagerScript>(scene).Single());Set(controller,"puzzle",All<LetterAssemblyPuzzle>(scene).Single());Set(controller,"weapon",All<Systems.WeaponSystem.PlayerWeaponController>(scene).Single());Set(controller,"panel",g);Set(controller,"label",label);
        }
        static void GroundNpcs(Scene scene)
        {
            var actors=All<SpeakerScript>(scene);var terrain=All<Terrain>(scene).Single();var source=actors[0].GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController).runtimeAnimatorController;
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),Data+"/GroundedNPC.controller");var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Data+"/GroundedNPC.controller");var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;EditorUtility.SetDirty(controller);
            var placements=new System.Collections.Generic.List<Placement>();
            foreach(var actor in actors)
            {
                var primary=actor.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController);Undo.RecordObject(primary,"Use one grounded animator");primary.runtimeAnimatorController=controller;primary.applyRootMotion=false;
                foreach(var extra in actor.GetComponentsInChildren<Animator>(true).Where(a=>a!=primary&&!a.runtimeAnimatorController)){Undo.RecordObject(extra,"Avoid duplicate animation ownership");extra.enabled=false;extra.applyRootMotion=false;}
                var driver=actor.GetComponentInChildren<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.NPCAnimatorScript>(true);if(driver){Set(driver,"animator",primary);Set(driver,"speaker",actor);Set(driver,"movementRoot",actor.transform);}
                var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");float support=terrain.SampleHeight(actor.transform.position)+terrain.transform.position.y;Vector3 before=actor.transform.position;Undo.RecordObject(actor.transform,"Seat NPC feet on terrain");actor.transform.position+=Vector3.up*(support+.015f-boots.bounds.min.y);
                var placement=Undo.AddComponent<NpcFootPlacement>(primary.gameObject);Set(placement,"actor",actor.transform);Set(placement,"soleMesh",boots);
                placements.Add(new Placement{id=GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString(),name=actor.name,before=before,after=actor.transform.position});
            }
            File.WriteAllText(Data+"/NpcPlacements.json",JsonUtility.ToJson(new Placements{actors=placements.ToArray()},true));
        }
        public static void RefineGround()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var placements=JsonUtility.FromJson<Placements>(File.ReadAllText(Data+"/NpcPlacements.json"));
            foreach(var actor in All<SpeakerScript>(scene))
            {
                var boots=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Botas");float surface=terrain.SampleHeight(actor.transform.position)+terrain.transform.position.y;
                Undo.RecordObject(actor.transform,"Seat the authored body before runtime foot calibration");actor.transform.position+=Vector3.up*(surface+.015f-boots.bounds.min.y);
                var feet=actor.GetComponentInChildren<NpcFootPlacement>(true);Set(feet,"soleMesh",boots);var row=placements.actors.Single(p=>p.name==actor.name);row.after=actor.transform.position;
            }
            File.WriteAllText(Data+"/NpcPlacements.json",JsonUtility.ToJson(placements,true));foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void RefineFonts()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var regular=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");
            foreach(var text in All<TMP_Text>(scene))
            {
                if(text.font&&text.font.name.StartsWith("InterTight"))continue;var data=new SerializedObject(text);data.FindProperty("m_fontAsset").objectReferenceValue=regular;data.FindProperty("m_sharedMaterial").objectReferenceValue=regular.material;data.ApplyModifiedProperties();EditorUtility.SetDirty(text);if(PrefabUtility.IsPartOfPrefabInstance(text))PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void RefineResponseLayout()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var root=All<CanvasSafeArea>(scene).Single().transform.Find("DecisionsUI/Main/ChoicesPanel");
            var manager=new SerializedObject(All<DialogueManagerScript>(scene).Single());manager.FindProperty("manualNpcAdvance").boolValue=true;manager.ApplyModifiedProperties();
            foreach(var host in new[]{root,root.Find("Hint")})foreach(var behaviour in host.GetComponents<Behaviour>())if(behaviour is LayoutGroup||behaviour is ContentSizeFitter||behaviour is AspectRatioFitter){Undo.RecordObject(behaviour,"Keep fixed panel anchors");behaviour.enabled=false;}
            foreach(var text in root.Find("Hint").GetComponentsInChildren<TMP_Text>(true))if(text.name=="ChoiceControls"){text.margin=Vector4.zero;text.alignment=TextAlignmentOptions.MidlineLeft;Rect(text.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);}
            var prefab=PrefabUtility.LoadPrefabContents(Data+"/MainResponse.prefab");try
            {
                foreach(var behaviour in prefab.GetComponentsInChildren<Behaviour>(true))if(behaviour is LayoutGroup||behaviour is ContentSizeFitter||behaviour is AspectRatioFitter)behaviour.enabled=false;
                foreach(var transform in prefab.GetComponentsInChildren<RectTransform>(true)){transform.localScale=Vector3.one;transform.localRotation=Quaternion.identity;}
                var button=new SerializedObject(prefab.GetComponent<WalkieDecisionButton>());var text=(TMP_Text)button.FindProperty("_textMesh").objectReferenceValue;text.margin=Vector4.zero;text.fontStyle=FontStyles.Normal;text.alignment=TextAlignmentOptions.MidlineLeft;Rect(text.transform,Vector2.zero,Vector2.one,new Vector2(18,10),new Vector2(-18,-10));var selected=(GameObject)button.FindProperty("_selectedBackground").objectReferenceValue;Rect(selected.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);PrefabUtility.SaveAsPrefabAsset(prefab,Data+"/MainResponse.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void BuildCleanResponse()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var presenter=All<WalkieUIScript>(scene).Single();var current=(GameObject)new SerializedObject(presenter).FindProperty("choicePrefab").objectReferenceValue;var original=new SerializedObject(current.GetComponent<WalkieDecisionButton>());var oldBackground=(GameObject)original.FindProperty("_selectedBackground").objectReferenceValue;var oldImage=oldBackground.GetComponent<Image>();
            var root=new GameObject(current.name,typeof(RectTransform),typeof(Image),typeof(LayoutElement),typeof(WalkieDecisionButton));root.layer=5;
            try
            {
                var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(780,64);var hit=root.GetComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;var layout=root.GetComponent<LayoutElement>();layout.preferredHeight=64;layout.minHeight=64;layout.minWidth=0;layout.preferredWidth=-1;layout.flexibleWidth=1;
                var background=UI("Selection",root.transform);Rect(background.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var image=Undo.AddComponent<Image>(background);image.color=oldImage?oldImage.color:new Color(1,.55f,.16f);if(oldImage)image.sprite=oldImage.sprite;image.type=Image.Type.Simple;image.raycastTarget=false;
                body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Bold SDF.asset");var text=Text("ResponseText",root.transform,"Respuesta",26);text.fontStyle=FontStyles.Normal;text.alignment=TextAlignmentOptions.MidlineLeft;text.margin=Vector4.zero;Rect(text.transform,Vector2.zero,Vector2.one,new Vector2(18,10),new Vector2(-18,-10));var button=root.GetComponent<WalkieDecisionButton>();Set(button,"_textMesh",text);Set(button,"_selectedBackground",background);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Data+"/MainResponse.prefab");Set(presenter,"choicePrefab",prefab);
            }
            finally{Object.DestroyImmediate(root);}
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void ApplyGuidance()
        {
            BuildCleanResponse();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var objective=All<ObjectiveHud>(scene).Single();Set(objective,"feedback",All<MissionAnnouncement>(scene).Single());Set(objective,"missions",All<MissionsUIManager>(scene).Single());Set(objective,"mateo",All<SpeakerScript>(scene).Single(s=>s.name.Contains("Mateo")));
            var overlay=All<MapZoneOverlay>(scene).Single();Set(overlay,"objective",objective);
            var legend=All<TMP_Text>(scene).Single(t=>t.name=="Zones"&&t.transform.parent.name=="MapLegend");Set(overlay,"legend",legend);
            Undo.RecordObject(legend,"Readable current destination");legend.fontSize=24;legend.enableAutoSizing=false;legend.overflowMode=TextOverflowModes.Overflow;
            Rect(legend.transform.parent,new Vector2(.70f,.12f),new Vector2(.98f,.86f),Vector2.zero,Vector2.zero);
            var manager=new SerializedObject(All<DialogueManagerScript>(scene).Single());manager.FindProperty("manualNpcAdvance").boolValue=true;manager.ApplyModifiedProperties();
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Audit()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var b=new StringBuilder();
            foreach(var canvas in All<Canvas>(scene))b.AppendLine("CANVAS "+PathOf(canvas.transform)+" "+EditorJsonUtility.ToJson(canvas.GetComponent<CanvasScaler>()));
            foreach(var rect in All<RectTransform>(scene).Where(r=>PathOf(r).Contains("Dialogue")||PathOf(r).Contains("Decisions")||r.parent&&r.parent.GetComponent<Canvas>()))
            {var text=rect.GetComponent<TMP_Text>();b.AppendLine($"UI {PathOf(rect)} rect={rect.rect} pos={rect.anchoredPosition} anchors={rect.anchorMin}/{rect.anchorMax} pivot={rect.pivot} active={rect.gameObject.activeSelf} font={(text?text.font.name:"")} fontsize={(text?text.fontSize:0)}\n{EditorJsonUtility.ToJson(rect)}");}
            var terrain=All<Terrain>(scene).Single();
            foreach(var speaker in All<SpeakerScript>(scene))
            {
                b.AppendLine($"NPC {PathOf(speaker.transform)} pos={speaker.transform.position} ground={terrain.SampleHeight(speaker.transform.position)+terrain.transform.position.y}");
                foreach(var r in speaker.GetComponentsInChildren<SkinnedMeshRenderer>(true))b.AppendLine($" BODY {PathOf(r.transform)} min={r.bounds.min.y} max={r.bounds.max.y} root={(r.rootBone?r.rootBone.name:"")}");
                foreach(var animator in speaker.GetComponentsInChildren<Animator>(true))
                {b.AppendLine($" ANIM {PathOf(animator.transform)} controller={(animator.runtimeAnimatorController?AssetDatabase.GetAssetPath(animator.runtimeAnimatorController):"")} rootMotion={animator.applyRootMotion} human={animator.isHuman} active={animator.enabled}");if(animator.runtimeAnimatorController)b.AppendLine(" CLIPS "+string.Join(",",animator.runtimeAnimatorController.animationClips.Select(c=>c.name)));}
            }
            File.WriteAllText("Library/DialoguePolish-audit.txt",b.ToString());
        }
    }
}
#endif
