#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SceneManagementEditor
{
    public static class StoryTellBuild
    {
        private static readonly string[] RequiredScenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameSelect.unity",
            "Assets/Scenes/Level1.unity"
        };

        public static void BuildWindows()
        {
            ValidateRequiredAssets();

            string configuredPath = Environment.GetEnvironmentVariable("STORYTELL_BUILD_PATH");
            string outputPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath("Builds/Windows/StoryTell.exe")
                : Path.GetFullPath(configuredPath);

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            EditorBuildSettings.scenes = Array.ConvertAll(
                RequiredScenes,
                path => new EditorBuildSettingsScene(path, true));

            var options = new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"StoryTell build failed: {summary.result}, {summary.totalErrors} errors and {summary.totalWarnings} warnings.");
            }

            Debug.Log(
                $"StoryTell Windows build completed at '{outputPath}' ({summary.totalSize} bytes, {summary.totalWarnings} warnings)." );
        }

        private static void ValidateRequiredAssets()
        {
            foreach (string scenePath in RequiredScenes)
            {
                if (!File.Exists(scenePath))
                    throw new BuildFailedException($"Required scene is missing: {scenePath}");
            }

            RequireAsset<UnityEngine.UIElements.VisualTreeAsset>(
                "Assets/Systems/SceneManagement/UI/Resources/StoryTellMainMenu/MainMenu.uxml");
            RequireAsset<UnityEngine.UIElements.VisualTreeAsset>(
                "Assets/Systems/SceneManagement/UI/Resources/StoryTellMainMenu/GameSelect.uxml");
            RequireAsset<UnityEngine.UIElements.ThemeStyleSheet>(
                "Assets/Systems/SceneManagement/UI/Resources/StoryTellMainMenu/RuntimeTheme.tss");
            RequireAsset<Texture2D>(
                "Assets/Systems/SceneManagement/UI/Resources/StoryTellMainMenu/CampaignNotebook.png");
            RequireAsset<Texture2D>(
                "Assets/Systems/SceneManagement/UI/Resources/StoryTellMainMenu/MalvinasPhoto.png");
        }

        private static void RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
                throw new BuildFailedException($"Required asset could not be imported: {path}");
        }
    }
}
#endif
