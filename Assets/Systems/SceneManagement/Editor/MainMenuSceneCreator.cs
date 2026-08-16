#if UNITY_EDITOR
using System.IO;
using SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneManagementEditor
{
    public static class MainMenuSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string LevelPath = "Assets/Scenes/Level1.unity";

        [MenuItem("StoryTell/Scene Management/Create Prototype Main Menu")]
        public static void CreatePrototypeMainMenu()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.GetComponent<Camera>().backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
            new GameObject("Scene Management", typeof(MainMenuController));
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(LevelPath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {ScenePath} and placed it first in Build Settings.");
        }
    }
}
#endif
