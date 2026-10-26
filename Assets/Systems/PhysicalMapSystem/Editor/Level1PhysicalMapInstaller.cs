#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using Systems.InteractionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace StoryTell.PhysicalMap.Editor
{
    [InitializeOnLoad]
    public static class Level1PhysicalMapInstaller
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string Root = "Assets/Systems/PhysicalMapSystem";
        private const string Request = "Library/Level1PhysicalMap.request";
        private const string Report = "Library/Level1PhysicalMap.report.json";
        private static double nextPoll;
        private static TestRunnerApi runner;

        [Serializable] private sealed class State
        {
            public string status, message, activeScene;
            public bool playing, dirty;
            public int maps;
        }

        static Level1PhysicalMapInstaller()
        {
            EditorApplication.update += Poll;
            if (SessionState.GetBool("Level1MapTestsRunning", false)) RegisterTestCallbacks();
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request)) return;
            string action = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (action == "inspect") WriteState("inspected", "Editor state read.");
                else if (action == "install") Install();
                else if (action == "tests") RunTests();
                else WriteState("error", "Unknown request.");
            }
            catch (Exception exception) { WriteState("error", exception.ToString()); Debug.LogException(exception); }
        }

        private static void WriteState(string status, string message)
        {
            Scene scene = SceneManager.GetActiveScene();
            File.WriteAllText(Report, JsonUtility.ToJson(new State {
                status = status, message = message, activeScene = scene.path,
                playing = EditorApplication.isPlaying, dirty = scene.isDirty,
                maps = scene.IsValid() ? InScene<PhysicalMapController>(scene).Length : 0
            }, true));
        }

        [MenuItem("StoryTell/Physical Map/Install In Level1")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { WriteState("waiting", "Exit Play Mode first."); return; }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty))
                { WriteState("waiting", "Save the currently open scenes first."); return; }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            PlayerControllerScript player = InScene<PlayerControllerScript>(scene).Single();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (camera == null) throw new InvalidOperationException("Player camera is missing.");
            string backup = "Library/Level1MapBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, backup + "/Level1.unity");
            if (scene.isDirty)
            {
                if (!AssetDatabase.IsValidFolder("Assets/SceneBackups")) AssetDatabase.CreateFolder("Assets", "SceneBackups");
                string snapshot = "Assets/SceneBackups/Level1BeforeMap-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity";
                if (!EditorSceneManager.SaveScene(scene, snapshot, true))
                    throw new IOException("Could not preserve the unsaved Level1 edits before installation.");
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Level1 physical map");
            MapSettings settings = AssetDatabase.LoadAssetAtPath<MapSettings>(Root + "/Data/SO_MapSettings_Level1.asset");
            if (settings == null)
            {
                settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MapSettings>(Root + "/Data/SO_MapSettings_Default.asset"));
                settings.name = "SO_MapSettings_Level1";
                settings.localHoldPosition = new Vector3(0, -0.1f, 0.8f);
                settings.localHoldEulerAngles = new Vector3(-68, 0, 0);
                settings.windStrength = 0.12f;
                settings.windTorque = 0.015f;
                settings.bendStrength = 0.045f;
                settings.foldAmount = 0.035f;
                AssetDatabase.CreateAsset(settings, Root + "/Data/SO_MapSettings_Level1.asset");
            }
            settings.localHoldPosition = new Vector3(0, -0.1f, 0.8f);
            EditorUtility.SetDirty(settings);
            PhysicalMapController map = InScene<PhysicalMapController>(scene).SingleOrDefault();
            if (map == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PhysicalMap.prefab");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "PhysicalMap_Level1";
                Undo.RegisterCreatedObjectUndo(instance, "Install Level1 map");
                map = instance.GetComponent<PhysicalMapController>();
            }
            Undo.RecordObject(map, "Configure Level1 map");
            map.Configure(settings, camera.transform);
            var mapSerialized = new SerializedObject(map);
            mapSerialized.FindProperty("startHidden").boolValue = true;
            mapSerialized.FindProperty("keyboardInputEnabled").boolValue = true;
            mapSerialized.FindProperty("toggleMapAction").objectReferenceValue = EnsureInputAction();
            mapSerialized.ApplyModifiedProperties();

            Bounds bounds = new Bounds(player.transform.position, Vector3.one * 60);
            foreach (InteractableScript item in InScene<InteractableScript>(scene)) bounds.Encapsulate(item.transform.position);
            foreach (var zone in InScene<Systems.Global.TriggerZoneScript>(scene)) bounds.Encapsulate(zone.transform.position);
            bounds.Expand(40f);
            Vector2 size = new Vector2(Mathf.Max(bounds.size.x, 100), Mathf.Max(bounds.size.z, 100));
            if (size.x / size.y < settings.width / settings.height) size.x = size.y * settings.width / settings.height;
            else size.y = size.x * settings.height / settings.width;
            GameObject origin = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "PhysicalMap_WorldOrigin");
            if (origin == null)
            {
                origin = new GameObject("PhysicalMap_WorldOrigin");
                SceneManager.MoveGameObjectToScene(origin, scene);
                Undo.RegisterCreatedObjectUndo(origin, "Create map origin");
            }
            Undo.RecordObject(origin.transform, "Align map origin");
            origin.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
            MapWorldReference reference = map.GetComponent<MapWorldReference>();
            if (reference == null) reference = Undo.AddComponent<MapWorldReference>(map.gameObject);
            Undo.RecordObject(reference, "Configure map bounds");
            reference.Configure(origin.transform, size, map);
            var referenceSerialized = new SerializedObject(reference);
            referenceSerialized.FindProperty("markerHeight").floatValue = 0.025f;
            referenceSerialized.ApplyModifiedProperties();
            MapMarkerController marker = map.GetComponentsInChildren<MapMarkerController>(true).First();
            Undo.RecordObject(marker, "Track Level1 player");
            marker.Configure(reference, map, player.transform);

            Material material = EnsureSurveyMaterial(scene, bounds.center, size);
            Undo.RecordObject(map.GetComponent<MeshRenderer>(), "Assign Level1 survey");
            map.GetComponent<MeshRenderer>().sharedMaterial = material;
            int mapLayer = EnsureMapLayer();
            foreach (Transform child in map.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Keep held map out of interaction raycasts");
                child.gameObject.layer = mapLayer;
                if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
            ConfigureViewCamera(map, camera, mapLayer);
            foreach (Collider collider in map.GetComponentsInChildren<Collider>(true))
            {
                Undo.RecordObject(collider, "Prevent held map collisions");
                collider.isTrigger = true;
            }
            foreach (Component component in map.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Level1.");
            WriteState("installed", "Map saved in Level1. M uses Gameplay/ToggleMap. Backup: " + backup);
        }

        private static int EnsureMapLayer()
        {
            int layer = LayerMask.NameToLayer("PhysicalMap");
            if (layer >= 0) return layer;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                layers.GetArrayElementAtIndex(i).stringValue = "PhysicalMap";
                tags.ApplyModifiedProperties();
                return i;
            }
            throw new InvalidOperationException("No free layer for the first-person paper view.");
        }

        private static void ConfigureViewCamera(PhysicalMapController map, Camera playerCamera, int mapLayer)
        {
            var view = map.GetComponent<PhysicalMapViewCamera>();
            if (view == null) view = Undo.AddComponent<PhysicalMapViewCamera>(map.gameObject);
            Camera overlay = view.MapCamera;
            if (overlay == null)
            {
                GameObject host = new GameObject("PhysicalMapCamera");
                Undo.RegisterCreatedObjectUndo(host, "Create paper view camera");
                host.transform.SetParent(playerCamera.transform, false);
                overlay = host.AddComponent<Camera>();
            }
            Undo.RecordObject(playerCamera, "Exclude paper from world depth");
            playerCamera.cullingMask &= ~(1 << mapLayer);
            overlay.cullingMask = 1 << mapLayer;
            overlay.nearClipPlane = 0.01f;
            overlay.farClipPlane = 4f;
            overlay.fieldOfView = playerCamera.fieldOfView;
            overlay.enabled = false;
            var overlayData = overlay.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            var overlaySerialized = new SerializedObject(overlayData);
            overlaySerialized.FindProperty("m_ClearDepth").boolValue = true;
            overlaySerialized.ApplyModifiedProperties();
            overlayData.renderPostProcessing = false;
            var baseData = playerCamera.GetUniversalAdditionalCameraData();
            if (!baseData.cameraStack.Contains(overlay)) baseData.cameraStack.Add(overlay);
            Undo.RecordObject(view, "Connect paper view camera");
            view.Configure(map, playerCamera, overlay);
            foreach (Component component in new Component[] { view, playerCamera, baseData, overlay, overlayData })
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static InputActionReference EnsureInputAction()
        {
            string assetPath = Root + "/Data/Level1MapInput.inputactions";
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (asset == null)
            {
                var generated = ScriptableObject.CreateInstance<InputActionAsset>();
                generated.AddActionMap("Gameplay").AddAction("ToggleMap", InputActionType.Button, "<Keyboard>/m");
                File.WriteAllText(assetPath, generated.ToJson());
                UnityEngine.Object.DestroyImmediate(generated);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            }
            string referencePath = Root + "/Data/Level1_ToggleMap.asset";
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
            if (reference == null)
            {
                reference = InputActionReference.Create(asset.FindAction("Gameplay/ToggleMap", true));
                AssetDatabase.CreateAsset(reference, referencePath);
            }
            return reference;
        }

        public static void ReframeSurvey(PhysicalMapController map, Vector3 center, Vector2 size)
        {
            var reference=map.GetComponent<MapWorldReference>();
            Undo.RecordObject(reference.WorldOrigin,"Frame the playable route on the map");
            reference.WorldOrigin.position=center;
            Undo.RecordObject(reference,"Map the playable route");reference.Configure(reference.WorldOrigin,size,map);
            Undo.RecordObject(map.GetComponent<MeshRenderer>(),"Refresh the route survey");
            map.GetComponent<MeshRenderer>().sharedMaterial=EnsureSurveyMaterial(map.gameObject.scene,center,size);
        }

        private static Material EnsureSurveyMaterial(Scene scene, Vector3 center, Vector2 size)
        {
            const int width = 768, height = 512;
            var pixels = new Color[width * height];
            Terrain terrain = InScene<Terrain>(scene).FirstOrDefault();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float wx = center.x + (x / (float)(width - 1) - 0.5f) * size.x;
                float wz = center.z + (y / (float)(height - 1) - 0.5f) * size.y;
                float elevation = terrain != null ? terrain.SampleHeight(new Vector3(wx, 0, wz)) : 0;
                float contour = Mathf.Repeat(elevation, 2f);
                bool line = contour < 0.1f;
                bool grid = x % 64 == 0 || y % 64 == 0;
                bool border = x < 10 || x >= width - 10 || y < 10 || y >= height - 10;
                pixels[y * width + x] = border ? new Color(0.24f, 0.22f, 0.15f) :
                    line ? new Color(0.46f, 0.40f, 0.26f) : grid ? new Color(0.66f, 0.60f, 0.43f) : new Color(0.86f, 0.81f, 0.65f);
            }
            foreach (InteractableScript item in InScene<InteractableScript>(scene))
            {
                int px = Mathf.RoundToInt(((item.transform.position.x - center.x) / size.x + 0.5f) * (width - 1));
                int py = Mathf.RoundToInt(((item.transform.position.z - center.z) / size.y + 0.5f) * (height - 1));
                for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
                    if (px + dx >= 0 && px + dx < width && py + dy >= 0 && py + dy < height)
                        pixels[(py + dy) * width + px + dx] = new Color(0.3f, 0.25f, 0.18f);
            }
            // North arrow: terrain and MapWorldReference share the same world X/Z axes.
            for (int y = 32; y < 74; y++)
            for (int x = -12; x <= 12; x++)
                if ((y < 58 && Mathf.Abs(x) < 2) || (y >= 58 && Mathf.Abs(x) <= (74 - y)))
                    pixels[(height - 96 + y) * width + width - 46 + x] = new Color(0.22f, 0.2f, 0.14f);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.SetPixels(pixels); texture.Apply();
            string texturePath = Root + "/Data/Level1Survey.png";
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            string materialPath = Root + "/Materials/Level1PaperMap.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("StoryTell/PhysicalMap/PaperDeformation"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T[] InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        private static void RunTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { WriteState("waiting", "Exit Play Mode before verification."); return; }
            RegisterTestCallbacks();
            SessionState.SetBool("Level1MapTestsRunning", true);
            WriteState("testing", "Running Level1 physical map integration tests.");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "Level1PhysicalMapIntegrationTests" } }));
        }

        private static void RegisterTestCallbacks()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new TestResults());
        }

        private sealed class TestResults : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                SessionState.SetBool("Level1MapTestsRunning", false);
                WriteState(result.FailCount == 0 ? "tests-passed" : "tests-failed", $"Passed {result.PassCount}, failed {result.FailCount}. {result.Message}");
                File.WriteAllText("Library/Level1PhysicalMap.tests.xml", result.ToXml().OuterXml);
            }
        }
    }
}
#endif
