#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using StoryTell.PhysicalMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTell.GameplayPath.Editor
{
    public static class GameplayPathHudAuthoring
    {
        public static void WidenCombatMargin()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var boundary=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameplayPathBoundary>(true)).Single();
            var data=new SerializedObject(boundary);var outline=data.FindProperty("outline");
            outline.GetArrayElementAtIndex(4).vector2Value=new Vector2(120,510);
            outline.GetArrayElementAtIndex(5).vector2Value=new Vector2(135,625);data.ApplyModifiedProperties();
            var points=boundary.Outline;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length],mid=(a+b)*0.5f,delta=b-a;var wall=boundary.Barriers[i];
                Undo.RecordObject(wall.transform,"Keep movement space outside rifle zone");Undo.RecordObject(wall,"Align widened boundary");
                wall.transform.position=new Vector3(mid.x,75,mid.y);wall.transform.rotation=Quaternion.Euler(0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0);wall.size=new Vector3(delta.magnitude+1,200,0.6f);
            }
            EditorUtility.SetDirty(boundary);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void ApplyBatch()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            Directory.CreateDirectory("Library/GameplayPathBackups");
            File.Copy(scene.path, "Library/GameplayPathBackups/Level1-before-hud-boundaries-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
            var controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameplayPathController>(true)).Single();
            if (controller.GetComponentInChildren<GameplayPathTaskHud>(true)) throw new InvalidOperationException("HUD already authored.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Quest checklist and narrative route boundaries");
            var data = new SerializedObject(controller); var map = (PhysicalMapController)data.FindProperty("map").objectReferenceValue;
            var canvas = controller.GetComponentInChildren<Canvas>(true); var safe = canvas.transform.Find("SafeArea");
            var root = UI("TaskHud", safe); var hud = Undo.AddComponent<GameplayPathTaskHud>(root); Stretch(root.transform);
            var panel = UI("Checklist", root.transform); Rect(panel.transform, new Vector2(0,1), new Vector2(28,-28), new Vector2(370,360), new Vector2(0,1));
            var background = Undo.AddComponent<Image>(panel); background.color = new Color(0.025f,0.035f,0.04f,0.73f); background.raycastTarget = false;
            var checklist = Text("TaskList", panel.transform, 24); Stretch(checklist.transform, 18); checklist.text = "TAREAS\n[ ] Hablar con Rubén";
            var banner = UI("NewMission", root.transform); Rect(banner.transform, new Vector2(0.5f,1),new Vector2(0,-28),new Vector2(770,125),new Vector2(0.5f,1));
            var shade = Undo.AddComponent<Image>(banner); shade.color = new Color(0.025f,0.035f,0.04f,0.85f); shade.raycastTarget = false;
            var announcement = Text("MissionText",banner.transform,26); Stretch(announcement.transform,18); announcement.alignment=TextAlignmentOptions.Center; announcement.text="Nueva misión";
            Set(hud,"controller",controller);Set(hud,"map",map);Set(hud,"checklistPanel",panel);Set(hud,"newMissionPanel",banner);Set(hud,"checklist",checklist);Set(hud,"newMission",announcement);
            var host = new GameObject("RouteBoundaries"); Undo.RegisterCreatedObjectUndo(host,"Create route limits");host.transform.SetParent(controller.transform,false);
            var boundary=Undo.AddComponent<GameplayPathBoundary>(host);Set(boundary,"controller",controller);
            // Broad camp perimeter, narrowing around the existing switchback route and widening at combat.
            Vector2[] points={new(12,220),new(155,220),new(165,375),new(131,419),new(120,510),new(135,625),new(-6,625),new(12,501),new(40,401),new(69,341),new(12,340)};
            var walls=new BoxCollider[points.Length];
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length],mid=(a+b)*0.5f,delta=b-a;
                var wall=new GameObject("InvisibleBoundary_"+i);Undo.RegisterCreatedObjectUndo(wall,"Create deferred invisible wall");wall.transform.SetParent(host.transform,false);
                wall.transform.position=new Vector3(mid.x,75,mid.y);wall.transform.rotation=Quaternion.Euler(0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0);
                walls[i]=Undo.AddComponent<BoxCollider>(wall);walls[i].size=new Vector3(delta.magnitude+1,200,0.6f);walls[i].enabled=false;
            }
            var serialized=new SerializedObject(boundary);var outline=serialized.FindProperty("outline");outline.arraySize=points.Length;var barriers=serialized.FindProperty("barriers");barriers.arraySize=walls.Length;
            for(int i=0;i<points.Length;i++){outline.GetArrayElementAtIndex(i).vector2Value=points[i];barriers.GetArrayElementAtIndex(i).objectReferenceValue=walls[i];}serialized.ApplyModifiedProperties();
            foreach(var component in controller.GetComponentsInChildren<Component>(true))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Library/GameplayPath-hud-authoring.txt","Checklist and 11 deferred boundary colliders saved.");
        }
        static GameObject UI(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Create quest UI");obj.transform.SetParent(parent,false);obj.layer=5;return obj;}
        static TMP_Text Text(string name,Transform parent,float size){var obj=UI(name,parent);var text=Undo.AddComponent<TextMeshProUGUI>(obj);text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");text.fontSharedMaterial=text.font.material;text.fontSize=size;text.color=new Color(0.94f,0.92f,0.85f);text.raycastTarget=false;text.margin=Vector4.zero;text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Overflow;return text;}
        static void Stretch(Transform t,float margin=0){var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*margin;r.offsetMax=-Vector2.one*margin;}
        static void Rect(Transform t,Vector2 anchor,Vector2 position,Vector2 size,Vector2 pivot){var r=(RectTransform)t;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;}
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedProperties();}
    }
}
#endif

