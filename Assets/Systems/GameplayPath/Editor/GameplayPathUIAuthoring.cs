#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace StoryTell.GameplayPath.Editor
{
    [InitializeOnLoad]
    public static class GameplayPathUIAuthoring
    {
        static double next;
        static TestRunnerApi runner;
        static GameplayPathUIAuthoring() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            next=EditorApplication.timeSinceStartup+1;
            if(!File.Exists("Library/GameplayPathUI.request"))return;
            string action=File.ReadAllText("Library/GameplayPathUI.request").Trim();File.Delete("Library/GameplayPathUI.request");
            try { if(action=="apply")Apply();else if(action=="tests")Tests();else Inspect(); }
            catch(Exception e) { File.WriteAllText("Library/GameplayPathUI-result.txt",e.ToString());Debug.LogException(e); }
        }
        public static void Inspect()
        {
            var s=SceneManager.GetActiveScene();var all=All<Transform>(s);
            File.WriteAllText("Library/GameplayPathUI-inspect.txt","Scene: "+s.path+" dirty="+s.isDirty+" playing="+EditorApplication.isPlaying+"\n"+string.Join("\n",all.Where(t=>t.GetComponent<Canvas>()||t.GetComponent<TMP_Text>()||t.name.Contains("Diario")||t.GetComponent<InspectionScript>()).Select(t=>{
                var text=t.GetComponent<TMP_Text>();var r=t as RectTransform;var c=t.GetComponent<Canvas>();var scaler=t.GetComponent<CanvasScaler>();
                return Path(t)+" active="+t.gameObject.activeSelf+(c?" CANVAS "+c.renderMode+" order="+c.sortingOrder:"")+(scaler?" SCALE="+scaler.uiScaleMode+" ref="+scaler.referenceResolution:"")+(text?" FONT="+text.font?.name+" size="+text.fontSize+" text="+text.text:"")+(r?" anchors="+r.anchorMin+"/"+r.anchorMax+" rect="+r.rect+" local="+r.anchoredPosition:"");})));
        }
        [MenuItem("StoryTell/Gameplay Path/Polish Canvas and document inspection")]
        public static void ApplyBatch(){EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);Apply();}
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=GameplayPathInstaller.ScenePath)throw new InvalidOperationException("Open Level1 first.");
            if(All<DocumentInspectionPanel>(scene).Length!=0)throw new InvalidOperationException("Document UI already installed; do not duplicate it.");
            Directory.CreateDirectory("Library/GameplayPathBackups");
            if(!EditorSceneManager.SaveScene(scene,"Library/GameplayPathBackups/Level1-before-ui-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Cannot back up open Level1.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Polish readable Canvas and inspectable documents");
            var medium=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");
            var bold=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Bold SDF.asset");
            if(!medium||!bold)throw new InvalidOperationException("Missing project fonts.");
            foreach(var canvas in All<Canvas>(scene).Where(c=>c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace))
            {
                var scaler=canvas.GetComponent<CanvasScaler>()??Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                Undo.RecordObject(scaler,"Scale UI consistently");scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=0.5f;
                // Keep the ending/vignette full screen, including outside a device's safe area.
                if(canvas.name!="GameplayPath_UI")continue;
                var safe=canvas.transform.Find("SafeArea");
                if(!safe){var children=canvas.transform.Cast<Transform>().ToArray();safe=Create("SafeArea",canvas.transform).transform;Rect(safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Undo.AddComponent<UIScreenSafeArea>(safe.gameObject);foreach(var child in children)Undo.SetTransformParent(child,safe,"Respect screen safe area");}
            }
            foreach(var text in All<TMP_Text>(scene))
            {
                bool heading=text.name=="NameText"||text.name=="Name"||text.name=="Scene1Complete"||text.name.StartsWith("Rotulo");
                Style(text,heading?bold:medium);if(text is TextMeshProUGUI)text.raycastTarget=false;
            }
            foreach(var ui in All<UIDialogueScript>(scene))Set(ui,"_defaultFont",medium);
            var managerCanvas=All<Canvas>(scene).Single(c=>c.transform.parent&&c.transform.parent.name=="====MANAGERS====");
            Transform managerRoot=managerCanvas.transform;
            var dialogue=managerRoot.Find("DialogueUI");Rect(dialogue,new Vector2(0.14f,0),new Vector2(0.86f,0),new Vector2(0,24),new Vector2(0,222));
            var narration=dialogue.Find("NarrationText").GetComponent<TMP_Text>();Rect(narration.transform,new Vector2(0,0),Vector2.one,new Vector2(24,20),new Vector2(-24,-66));TextSize(narration,30,26);narration.alignment=TextAlignmentOptions.TopLeft;
            var name=dialogue.Find("NameText").GetComponent<TMP_Text>();Rect(name.transform,new Vector2(0,1),Vector2.one,new Vector2(24,-50),new Vector2(-24,-10));TextSize(name,25,25);name.alignment=TextAlignmentOptions.MidlineLeft;
            var background=dialogue.GetComponent<Image>()??Undo.AddComponent<Image>(dialogue.gameObject);Undo.RecordObject(background,"Readable subtitle background");background.color=new Color(0.025f,0.035f,0.04f,0.78f);background.raycastTarget=false;
            var visibility=dialogue.GetComponent<SubtitlePanelVisibility>()??Undo.AddComponent<SubtitlePanelVisibility>(dialogue.gameObject);Set(visibility,"background",background);Set(visibility,"narration",narration);Set(visibility,"speaker",name);
            var interaction=managerRoot.Find("InteractionUI");Rect(interaction,new Vector2(0.5f,0.27f),new Vector2(0.5f,0.27f),new Vector2(-280,0),new Vector2(280,110));
            var itemName=interaction.Find("Name").GetComponent<TMP_Text>();Rect(itemName.transform,new Vector2(0,0.45f),Vector2.one,Vector2.zero,Vector2.zero);TextSize(itemName,30,26);itemName.alignment=TextAlignmentOptions.Center;
            var action=interaction.Find("Action").GetComponent<TMP_Text>();Rect(action.transform,Vector2.zero,new Vector2(1,0.45f),Vector2.zero,Vector2.zero);TextSize(action,24,24);action.alignment=TextAlignmentOptions.Center;
            var info=managerRoot.Find("InteractionInfoUI");Rect(info,new Vector2(0.15f,0.06f),new Vector2(0.85f,0.19f),Vector2.zero,Vector2.zero);
            foreach(var t in info.GetComponentsInChildren<TMP_Text>(true)){Rect(t.transform,Vector2.zero,Vector2.one,new Vector2(16,8),new Vector2(-16,-8));TextSize(t,27,24);}
            var decisions=managerRoot.Find("DecisionsUI");Rect(decisions,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var main=decisions.Find("Main");Rect(main,new Vector2(0,0.30f),new Vector2(0.38f,0.86f),new Vector2(28,0),Vector2.zero);
            var controller=All<GameplayPathController>(scene).Single();var serialized=new SerializedObject(controller);
            var notice=(TMP_Text)serialized.FindProperty("noticeText").objectReferenceValue;Rect(notice.transform,new Vector2(0.16f,0.25f),new Vector2(0.84f,0.25f),new Vector2(0,0),new Vector2(0,98));TextSize(notice,27,24);notice.alignment=TextAlignmentOptions.Center;
            var journal=(GameObject)serialized.FindProperty("journalPanel").objectReferenceValue;Rect(journal.transform,new Vector2(0.67f,0.09f),new Vector2(0.98f,0.92f),Vector2.zero,Vector2.zero);
            var list=(TMP_Text)serialized.FindProperty("journalText").objectReferenceValue;TextSize(list,25,23);list.margin=Vector4.zero;
            var reading=(GameObject)serialized.FindProperty("readingPanel").objectReferenceValue;Rect(reading.transform,new Vector2(0.60f,0.09f),new Vector2(0.98f,0.92f),Vector2.zero,Vector2.zero);
            TextSize((TMP_Text)serialized.FindProperty("readingText").objectReferenceValue,27,23);
            var inspection=controller.Player.GetComponent<InspectionScript>();
            Undo.RecordObject(inspection.inspectAnchor,"Place document to the left of its transcription");inspection.inspectAnchor.localPosition=new Vector3(-0.10f,-0.03f,0.65f);
            var inspectorSettings=new SerializedObject(inspection);var paused=inspectorSettings.FindProperty("onOffScripts");
            var controls=new MonoBehaviour[]{controller.Player.GetComponent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>(),controller.Player.GetComponent<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>(),controller.Player.GetComponent<InteractionScript>()};
            paused.arraySize=controls.Length;for(int i=0;i<controls.Length;i++)paused.GetArrayElementAtIndex(i).objectReferenceValue=controls[i];inspectorSettings.ApplyModifiedProperties();
            var panelHost=Create("DocumentInspection",notice.GetComponentInParent<Canvas>().transform.Find("SafeArea"));Rect(panelHost.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var panelController=Undo.AddComponent<DocumentInspectionPanel>(panelHost);
            var panel=Create("Transcripcion",panelHost.transform);Rect(panel.transform,new Vector2(0.60f,0.08f),new Vector2(0.98f,0.93f),Vector2.zero,Vector2.zero);
            var image=Undo.AddComponent<Image>(panel);image.color=new Color(0.025f,0.035f,0.04f,0.94f);image.raycastTarget=false;
            var title=Text("DocumentTitle",panel.transform,bold,29);Rect(title.transform,new Vector2(0,1),Vector2.one,new Vector2(28,-96),new Vector2(-28,-24));TextSize(title,29,25);
            var viewport=Create("Viewport",panel.transform);Rect(viewport.transform,Vector2.zero,Vector2.one,new Vector2(28,108),new Vector2(-28,-110));Undo.AddComponent<RectMask2D>(viewport);
            var body=Text("Transcription",viewport.transform,medium,26);Rect(body.transform,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);body.rectTransform.pivot=new Vector2(0.5f,1);body.richText=false;body.overflowMode=TextOverflowModes.Overflow;
            var fitter=Undo.AddComponent<ContentSizeFitter>(body.gameObject);fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=Undo.AddComponent<ScrollRect>(panel);scroll.viewport=(RectTransform)viewport.transform;scroll.content=body.rectTransform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=false;
            var footer=Text("Controls",panel.transform,medium,21);footer.text="CLIC IZQ + RATÓN · Girar\nWASD · Mover    RUEDA · Leer\n[E] Dejar documento";Rect(footer.transform,Vector2.zero,new Vector2(1,0),new Vector2(28,20),new Vector2(-28,94));
            Set(panelController,"inspection",inspection);Set(panelController,"map",serialized.FindProperty("map").objectReferenceValue);Set(panelController,"panel",panel);Set(panelController,"title",title);Set(panelController,"body",body);Set(panelController,"scroll",scroll);panel.SetActive(false);
            foreach(var item in All<GameplayPathInteractable>(scene).Where(i=>i.Action==PathAction.Read))
            {
                var data=new SerializedObject(item);string text=data.FindProperty("readingText").stringValue;var doc=Undo.AddComponent<InspectableDocument>(item.gameObject);Fill(doc,text.Split('\n')[0],text);
                foreach(string field in new[]{"stateDescription","focusInteraction"})Set(item,field,false);
                Set(item,"inspectInteraction",true);Set(item,"subInteractions",true);
            }
            foreach(var item in All<InspectableInteractableScript>(scene).Where(i=>i.name.StartsWith("Diario1")))
            {
                var doc=item.GetComponent<InspectableDocument>()??Undo.AddComponent<InspectableDocument>(item.gameObject);
                Fill(doc,"Diario · lectura provisional",item.UIInteraction.characterDescription);
                Set(item,"stateDescription",false);Set(item,"focusInteraction",false);
            }
            foreach(string prefabPath in new[]{"Assets/Prefabs/Interactables/Diario1.prefab","Assets/Systems/DecisionSystem/Prefabs/Choice.prefab"})
            {
                var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
                try{
                    foreach(var text in prefab.GetComponentsInChildren<TMP_Text>(true))Style(text,medium);
                    if(prefabPath.Contains("Diario1")){var item=prefab.GetComponent<InspectableInteractableScript>();var doc=prefab.GetComponent<InspectableDocument>()??prefab.AddComponent<InspectableDocument>();Fill(doc,"Diario · lectura provisional",item.UIInteraction.characterDescription);Set(item,"stateDescription",false);Set(item,"focusInteraction",false);}
                    PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
                }finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            FinishDocuments();FinishPresentation();File.WriteAllText("Library/GameplayPathUI-result.txt","applied");Inspect();
        }
        static void Fill(InspectableDocument doc,string title,string text){Undo.RecordObject(doc,"Set editable document transcription");doc.title=title;doc.transcription=text;EditorUtility.SetDirty(doc);}
        public static void FinishPresentation()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            foreach(var text in All<TMP_Text>(scene).Where(t=>t.name=="NameText"||t.name=="NarrationText"))
            {Undo.RecordObject(text,"Remove legacy negative subtitle margins");text.margin=Vector4.zero;text.fontStyle=FontStyles.Normal;}
            var map=AssetDatabase.LoadAssetAtPath<StoryTell.PhysicalMap.MapSettings>("Assets/Systems/PhysicalMapSystem/Data/SO_MapSettings_Level1.asset");
            Undo.RecordObject(map,"Keep physical map beside its task list");map.localHoldPosition=new Vector3(-0.22f,-0.08f,1.0f);EditorUtility.SetDirty(map);
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void FinishDocuments()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            var controller=All<GameplayPathController>(scene).Single();
            string[] pieces=controller.Definition.motherLetter.Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries);
            foreach(var item in All<GameplayPathInteractable>(scene).Where(i=>i.Action==PathAction.Fragment))
            {
                var doc=item.GetComponent<InspectableDocument>()??Undo.AddComponent<InspectableDocument>(item.gameObject);
                Fill(doc,"Carta de la madre · fragmento "+(item.FragmentIndex+1)+"/4",pieces[Mathf.Min(item.FragmentIndex,pieces.Length-1)]+"\n\nFicción provisional. Fragmento recuperado para devolver a Rubén.");
                Set(item,"inspectInteraction",true);Set(item,"subInteractions",true);
            }
            var settings=new SerializedObject(controller);
            Set(controller.Player.GetComponent<InspectionScript>(),"inspectAction",settings.FindProperty("closeReading").objectReferenceValue);
            foreach(var doc in All<InspectableDocument>(scene)){Undo.RecordObject(doc,"Face the readable side toward the player");doc.inspectionEulerAngles=new Vector3(-65,0,0);}
            var diaryPrefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Interactables/Diario1.prefab");
            try{diaryPrefab.GetComponent<InspectableDocument>().inspectionEulerAngles=new Vector3(-65,0,0);PrefabUtility.SaveAsPrefabAsset(diaryPrefab,"Assets/Prefabs/Interactables/Diario1.prefab");}finally{PrefabUtility.UnloadPrefabContents(diaryPrefab);}
            if(settings.FindProperty("motherDocument").objectReferenceValue==null)
            {
                var assembled=GameObject.CreatePrimitive(PrimitiveType.Cube);assembled.name="Carta_Madre_Reconstruida";Undo.RegisterCreatedObjectUndo(assembled,"Prepare assembled mother letter");assembled.transform.SetParent(controller.transform,false);assembled.transform.localScale=new Vector3(0.31f,0.004f,0.22f);
                var reference=All<GameplayPathInteractable>(scene).First(i=>i.Action==PathAction.Fragment);assembled.GetComponent<Renderer>().sharedMaterial=reference.GetComponent<Renderer>().sharedMaterial;
                var doc=Undo.AddComponent<InspectableDocument>(assembled);Fill(doc,"Carta de la madre de Rubén · ficción provisional",controller.Definition.motherLetter);doc.hideAfterInspection=true;
                settings.FindProperty("motherDocument").objectReferenceValue=doc;settings.ApplyModifiedProperties();assembled.SetActive(false);
            }
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Style(TMP_Text text,TMP_FontAsset font){Undo.RecordObject(text,"Use project typography");text.font=font;text.fontSharedMaterial=font.material;EditorUtility.SetDirty(text);}
        static void TextSize(TMP_Text t,float size,float min){Undo.RecordObject(t,"Readable text sizing");t.margin=Vector4.zero;t.fontStyle=FontStyles.Normal;t.fontSize=size;t.enableAutoSizing=size!=min;t.fontSizeMin=min;t.fontSizeMax=size;t.overflowMode=TextOverflowModes.Ellipsis;t.textWrappingMode=TextWrappingModes.Normal;}
        static TextMeshProUGUI Text(string name,Transform parent,TMP_FontAsset font,float size){var host=Create(name,parent);var text=Undo.AddComponent<TextMeshProUGUI>(host);Style(text,font);text.fontSize=size;text.color=new Color(0.94f,0.92f,0.85f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.TopLeft;return text;}
        static GameObject Create(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Create document UI");obj.transform.SetParent(parent,false);obj.layer=5;return obj;}
        static void Rect(Transform t,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper){var r=(RectTransform)t;Undo.RecordObject(r,"Place UI within screen");r.anchorMin=min;r.anchorMax=max;r.offsetMin=lower;r.offsetMax=upper;r.localScale=Vector3.one;}
        static void Set(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedProperties();}
        static void Set(Object target,string field,bool value){var s=new SerializedObject(target);s.FindProperty(field).boolValue=value;s.ApplyModifiedProperties();}
        static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Tests(){runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"GameplayPathUITests","GameplayPathTests"}}));}
        class Results:ICallbacks{public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor r){}public void RunFinished(ITestResultAdaptor r){File.WriteAllText("Library/GameplayPathUI-tests.xml",r.ToXml().OuterXml);File.WriteAllText("Library/GameplayPathUI-tests-summary.txt","passed="+r.PassCount+" failed="+r.FailCount+" skipped="+r.SkipCount);}}
    }
}
#endif
