#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public static class TeamPlaytestBuild
{
    public static void FixReticleAndBuild()
    {
        const string path="Assets/Scenes/Level1.unity";File.Copy(path,"Library/TeamPlaytest-2026-11-23-backup/Level1-before-reticle.unity");
        var scene=EditorSceneManager.OpenScene(path);var graphics=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Graphic>(true)).ToArray();
        var dots=graphics.Where(g=>g.name=="Dot"&&g.rectTransform.sizeDelta.x<=20&&g.rectTransform.sizeDelta.y<=20).ToArray();if(dots.Length!=1)throw new InvalidOperationException("Expected exactly one centered HUD Dot");
        foreach(var graphic in dots){Undo.RecordObject(graphic,"Remove the actual central HUD dot");Undo.RecordObject(graphic.gameObject,"Hide the aiming dot");graphic.enabled=false;graphic.raycastTarget=false;graphic.gameObject.SetActive(false);if(PrefabUtility.IsPartOfPrefabInstance(graphic)){PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);PrefabUtility.RecordPrefabInstancePropertyModifications(graphic.gameObject);}}
        foreach(var graphic in graphics.OfType<RawImage>().Where(g=>g.name=="Cross")){Undo.RecordObject(graphic.gameObject,"Restore the native dialogue choice decoration");graphic.gameObject.SetActive(true);if(PrefabUtility.IsPartOfPrefabInstance(graphic))PrefabUtility.RecordPrefabInstancePropertyModifications(graphic.gameObject);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(path);if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Graphic>(true)).Any(g=>g.name=="Dot"&&(g.enabled||g.gameObject.activeSelf)))throw new InvalidOperationException("HUD dot override did not persist");
        Run();
    }
    public static void FinalizeInputAndBuild()
    {
        const string path="Assets/Scenes/Level1.unity";File.Copy(path,"Library/TeamPlaytest-2026-11-23-backup/Level1-before-input-final.unity");var scene=EditorSceneManager.OpenScene(path);int changes=0;
        foreach(var item in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Systems.InteractionSystem.Scripts.InteractableScript>(true)).Where(i=>i.UIInteraction!=null&&i.InspectInteractable&&!i.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()))
        {
            if(item.UIInteraction.interactionType==InteractionSystem.Scripts.Utils.EInteractions.EKeypress)continue;Undo.RecordObject(item,"Consistent inspection input");item.UIInteraction.interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress;EditorUtility.SetDirty(item);if(PrefabUtility.IsPartOfPrefabInstance(item))PrefabUtility.RecordPrefabInstancePropertyModifications(item);changes++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(path);
        foreach(var item in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Systems.InteractionSystem.Scripts.InteractableScript>(true)).Where(i=>i.UIInteraction!=null&&i.InspectInteractable&&!i.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()))if(item.UIInteraction.interactionType!=InteractionSystem.Scripts.Utils.EInteractions.EKeypress)throw new InvalidOperationException("Inspection input did not persist: "+item.name);
        File.WriteAllText("Library/TeamPlaytest-2026-11-23-input-final.txt","Additional inspectable inputs normalized: "+changes);Run();
    }
    public static void Run()
    {
        string folder=Path.GetFullPath("Builds/StoryTell-Prueba-2026-11-23-r3");
        if(Directory.Exists(folder))throw new InvalidOperationException("The output already exists; preserve the previous build.");
        string[] scenes={"Assets/Scenes/MainMenu.unity","Assets/Scenes/GameSelect.unity","Assets/Scenes/Level1.unity"};
        foreach(var path in scenes)
        {
            if(!File.Exists(path))throw new FileNotFoundException("Required scene missing",path);
            var scene=EditorSceneManager.OpenScene(path);int missing=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            if(missing>0)throw new InvalidOperationException(path+" contains missing scripts: "+missing);
        }
        Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(folder,"StoryTell.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        var summary=report.summary;var text=new StringBuilder();text.AppendLine("Result: "+summary.result).AppendLine("Errors: "+summary.totalErrors).AppendLine("Warnings: "+summary.totalWarnings).AppendLine("Size: "+summary.totalSize).AppendLine("Duration: "+summary.totalTime).AppendLine("Output: "+summary.outputPath).AppendLine("Unity: "+Application.unityVersion).AppendLine("Backend: "+PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)).AppendLine("Scenes: "+string.Join(", ",scenes));
        foreach(var step in report.steps)foreach(var message in step.messages.Where(m=>m.type==LogType.Error||m.type==LogType.Exception||m.type==LogType.Warning))text.AppendLine(message.type+": "+message.content);
        File.WriteAllText("Library/TeamPlaytest-2026-11-23-build-summary.txt",text.ToString());
        if(summary.result!=BuildResult.Succeeded||summary.totalErrors>0)throw new InvalidOperationException("Player build failed; see the build summary.");
    }
}
#endif
