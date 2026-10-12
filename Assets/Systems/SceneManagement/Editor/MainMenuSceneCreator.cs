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
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GameSelectPath = "Assets/Scenes/GameSelect.unity";
        private const string LevelPath = "Assets/Scenes/Level1.unity";

        [MenuItem("StoryTell/Scene Management/Rebuild Menu Flow")]
        public static void RebuildMenuFlow()
        {
            Directory.CreateDirectory("Assets/Scenes");
            CreateMenuScene<MainMenuController>(MainMenuPath, "Scene Management");
            CreateMenuScene<GameSelectController>(GameSelectPath, "Game Select");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GameSelectPath, true),
                new EditorBuildSettingsScene(LevelPath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("StoryTell menu flow rebuilt: MainMenu -> GameSelect -> Level1.");
        }

        private static void CreateMenuScene<T>(string scenePath, string controllerName)
            where T : MonoBehaviour
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.GetComponent<Camera>().backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
            new GameObject(controllerName, typeof(T));
            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}
#endif
