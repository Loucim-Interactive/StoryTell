#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Utils;
using InteractionSystem.Scripts.Utils;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StoryTell.Weapons.Editor
{
    [InitializeOnLoad]
    public static class Level1WeaponInstaller
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string Root = "Assets/Systems/WeaponSystem";
        private const string Request = "Library/Level1Weapons.request";
        private static double nextPoll;
        private static TestRunnerApi runner;
        static Level1WeaponInstaller()
        {
            EditorApplication.update += Poll;
            if (SessionState.GetBool("Level1WeaponsTesting", false)) RegisterTests();
        }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request)) return;
            string action = File.ReadAllText(Request).Trim(); File.Delete(Request);
            try
            {
                if (action == "install") Install();
                else if (action == "tests") RunTests();
                else Report("inspected", SceneManager.GetActiveScene().path + " playing=" + EditorApplication.isPlaying);
            }
            catch (Exception e) { Report("error", e.ToString()); Debug.LogException(e); }
        }
        [Serializable] private sealed class Result { public string status, message; }
        private static void Report(string status, string message) => File.WriteAllText("Library/Level1Weapons.report.json", JsonUtility.ToJson(new Result { status = status, message = message }, true));

        [MenuItem("StoryTell/Weapons/Install Trial In Level1")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Report("waiting", "Exit Play Mode first."); return; }
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty))
                    throw new InvalidOperationException("Save open scenes before installing the weapon trial.");
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            if (InScene<Level1WeaponTrial>(scene).Any())
            {
                string correctionBackup = "Library/Level1WeaponBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                Directory.CreateDirectory(correctionBackup); File.Copy(ScenePath, correctionBackup + "/Level1.unity");
                Undo.IncrementCurrentGroup(); int correction = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Remove imported FAL studio cameras and lights");
                foreach (var pickup in InScene<WeaponTrialPickup>(scene).Where(p => !p.IsAmmunition)) CleanImportedModel(pickup.gameObject);
                foreach (var installedWeapon in InScene<WeaponController>(scene))
                {
                    var serialized = new SerializedObject(installedWeapon);
                    var visualObject = serialized.FindProperty("weaponVisual").objectReferenceValue as GameObject;
                    if (visualObject != null)
                    {
                        CleanImportedModel(visualObject);
                        Transform rifleModel = visualObject.transform.GetChild(0);
                        Undo.RecordObject(rifleModel, "Point FAL barrel forward");
                        rifleModel.localRotation = Quaternion.Euler(0, 90, 0);
                        CenterModel(rifleModel.gameObject, 0.95f);
                    }
                }
                Undo.CollapseUndoOperations(correction); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Report("installed", "FAL presentation updated. Model cameras=" + InScene<WeaponTrialPickup>(scene).Sum(p => p.GetComponentsInChildren<Camera>(true).Length) + " model lights=" + InScene<WeaponTrialPickup>(scene).Sum(p => p.GetComponentsInChildren<Light>(true).Length) + ". Backup: " + correctionBackup); return;
            }
            string backup = "Library/Level1WeaponBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup); File.Copy(ScenePath, backup + "/Level1.unity");
            if (scene.isDirty)
            {
                if (!AssetDatabase.IsValidFolder("Assets/SceneBackups")) AssetDatabase.CreateFolder("Assets", "SceneBackups");
                if (!EditorSceneManager.SaveScene(scene, "Assets/SceneBackups/Level1BeforeWeapons-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity", true))
                    throw new IOException("Cannot preserve unsaved scene edits.");
            }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install Level1 FAL trial");
            var player = InScene<PlayerControllerScript>(scene).Single();
            Camera camera = player.GetComponentsInChildren<Camera>(true).First(c => c.CompareTag("MainCamera"));
            var interaction = InScene<InteractionScript>(scene).First();
            GameObject root = Create("WeaponTrial_Level1", null);
            var trial = Undo.AddComponent<Level1WeaponTrial>(root);
            GameObject zoneObject = Create("WeaponUseZone_MoveAndResize", root.transform);
            zoneObject.transform.position = player.transform.position;
            var zone = Undo.AddComponent<BoxCollider>(zoneObject); zone.isTrigger = true;
            zone.size = new Vector3(36, 12, 36); zone.center = new Vector3(0, 2, 0);

            GameObject host = Create("PlayerFAL", player.transform);
            var inventory = Undo.AddComponent<MagazineInventory>(host);
            Undo.AddComponent<WeaponCondition>(host);
            var controller = Undo.AddComponent<WeaponController>(host);
            var definition = Definition();
            controller.Configure(definition, inventory, host.GetComponent<WeaponCondition>(), camera);
            Set(controller, "useRestriction", trial);
            // Ignore held models, player body and pickups; the terrain and targets still occlude shots.
            int viewLayer = EnsureViewLayer();
            Set(controller, "hitMask", ~(1 << viewLayer | 1 << 9 | 1 << 6));
            GameObject viewRoot = Create("FAL_View", camera.transform);
            GameObject hip = Create("HipPose", camera.transform); hip.transform.localPosition = new Vector3(0.22f, -0.23f, 0.45f);
            GameObject aim = Create("AimPose", camera.transform); aim.transform.localPosition = new Vector3(0, -0.12f, 0.40f);
            viewRoot.transform.localPosition = hip.transform.localPosition;
            GameObject recoil = Create("RecoilAndShake", viewRoot.transform);
            GameObject visual = Create("HeldFAL", recoil.transform);
            GameObject model = Model(visual.transform);
            model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            CenterModel(model, 0.95f);
            SetLayer(visual, viewLayer);
            controller.ConfigurePresentation(visual, viewRoot.transform, hip.transform, aim.transform);
            var audio = Undo.AddComponent<AudioSource>(host); audio.playOnAwake = false; audio.spatialBlend = 0;
            Set(controller, "audioSource", audio);
            Set(controller, "fireClip", ShotSound());
            string flashPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Weapon Effects/Prefabs/MuzzleFlash.prefab";
            var flashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(flashPath);
            if (flashPrefab != null)
            {
                GameObject flash = (GameObject)PrefabUtility.InstantiatePrefab(flashPrefab, scene);
                Undo.RegisterCreatedObjectUndo(flash, "Create FAL muzzle flash");
                flash.transform.SetParent(visual.transform, false); flash.transform.localPosition = new Vector3(0, 0, 0.48f);
                flash.transform.localScale = Vector3.one * 0.18f;
                foreach (ParticleSystem ps in flash.GetComponentsInChildren<ParticleSystem>(true)) { var main = ps.main; main.playOnAwake = false; main.loop = false; }
                SetLayer(flash, viewLayer); Set(controller, "muzzleFlash", flash.GetComponentInChildren<ParticleSystem>());
                foreach (Component c in flash.GetComponentsInChildren<Component>(true)) if (c != null) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
            Camera overlay = Create("WeaponViewCamera", camera.transform).AddComponent<Camera>();
            overlay.cullingMask = 1 << viewLayer; overlay.nearClipPlane = 0.01f; overlay.farClipPlane = 4;
            overlay.fieldOfView = camera.fieldOfView; overlay.enabled = false;
            var data = overlay.GetUniversalAdditionalCameraData(); data.renderType = CameraRenderType.Overlay; data.renderPostProcessing = false;
            Set(data, "m_ClearDepth", true);
            Undo.RecordObject(camera, "Exclude held FAL from world camera"); camera.cullingMask &= ~(1 << viewLayer);
            var baseData = camera.GetUniversalAdditionalCameraData(); Undo.RecordObject(baseData, "Add FAL overlay"); baseData.cameraStack.Add(overlay);
            if (PrefabUtility.IsPartOfPrefabInstance(camera)) PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            if (PrefabUtility.IsPartOfPrefabInstance(baseData)) PrefabUtility.RecordPrefabInstancePropertyModifications(baseData);
            var input = Undo.AddComponent<WeaponInput>(host);
            Set(input, "controller", controller); Set(input, "trial", trial);
            var actions = Actions();
            Set(input, "fireAction", Reference(actions, "Fire")); Set(input, "aimAction", Reference(actions, "Aim")); Set(input, "reloadAction", Reference(actions, "Reload"));
            var feedback = Undo.AddComponent<WeaponRecoil>(host);
            var look = player.GetComponent<CameraControllerScript>();
            Set(feedback, "weapon", controller); Set(feedback, "look", look); Set(feedback, "kickPivot", recoil.transform);

            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = player.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 drop = Ground(player.transform.position + forward * 1.5f);
            GameObject rifle = Create("FAL_Pickup_Spawn", root.transform);
            GameObject floorModel = Model(rifle.transform); CenterModel(floorModel, 0.95f);
            rifle.transform.position = drop + Vector3.up * 0.12f;
            rifle.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, 0, 90);
            var rifleCollider = Undo.AddComponent<BoxCollider>(rifle); rifleCollider.size = new Vector3(1.1f, 0.32f, 0.35f);
            // Ground model lies flat; collider remains large enough for the interaction ray.
            rifle.transform.rotation = Quaternion.LookRotation(forward);
            Pickup(rifle, trial, false);
            GameObject ammo = Create("FAL_Ammo_3Magazines", root.transform);
            ammo.transform.position = Ground(player.transform.position + forward * 1.6f + right * 0.85f) + Vector3.up * 0.18f;
            for (int i = 0; i < 3; i++)
            {
                GameObject magazine = Primitive("Magazine_" + (i + 1), PrimitiveType.Cube, ammo.transform);
                magazine.transform.localPosition = new Vector3((i - 1) * 0.1f, 0, 0);
                magazine.transform.localScale = new Vector3(0.07f, 0.20f, 0.12f);
                UnityEngine.Object.DestroyImmediate(magazine.GetComponent<Collider>());
                magazine.GetComponent<Renderer>().sharedMaterial = Material("Ammo", new Color(0.22f, 0.28f, 0.17f));
            }
            var ammoCollider = Undo.AddComponent<BoxCollider>(ammo); ammoCollider.size = new Vector3(0.4f, 0.35f, 0.3f);
            Pickup(ammo, trial, true);
            for (int i = 0; i < 3; i++)
            {
                GameObject target = Primitive("EnemyCylinder_" + (i + 1), PrimitiveType.Cylinder, root.transform);
                target.transform.position = Ground(player.transform.position + forward * (7 + i * 2) + right * ((i - 1) * 2)) + Vector3.up * 0.9f;
                target.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                target.GetComponent<Renderer>().sharedMaterial = Material("Target", new Color(0.6f, 0.16f, 0.1f));
                var receiver = Undo.AddComponent<WeaponCylinderTarget>(target); Set(receiver, "targetRenderer", target.GetComponent<Renderer>());
            }
            GameObject canvasObject = Create("WeaponHUD", root.transform);
            var canvas = Undo.AddComponent<Canvas>(canvasObject); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var scaler = Undo.AddComponent<CanvasScaler>(canvasObject); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var darkness = Create("SprayDarkness", canvasObject.transform, typeof(RectTransform)).AddComponent<Image>();
            Stretch(darkness.rectTransform); darkness.color = Color.clear; darkness.raycastTarget = false;
            var status = Text("AmmoAndControls", canvasObject.transform, 27, TextAlignmentOptions.BottomLeft);
            Stretch(status.rectTransform); status.margin = new Vector4(40, 40, 40, 40);
            var completion = Text("Scene1Complete", canvasObject.transform, 56, TextAlignmentOptions.Center);
            Stretch(completion.rectTransform); completion.text = "Finalización escena 1"; completion.gameObject.SetActive(false);
            Set(trial, "weapon", controller); Set(trial, "player", player.transform); Set(trial, "firingCamera", camera); Set(trial, "viewCamera", overlay);
            Set(trial, "heldVisual", visual); Set(trial, "useZone", zone); Set(trial, "map", InScene<PhysicalMapController>(scene).Single());
            Set(trial, "movement", player); Set(trial, "look", look); Set(trial, "interaction", interaction);
            Set(trial, "status", status); Set(trial, "completion", completion); Set(trial, "darkness", darkness);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Level1.");
            Report("installed", "FAL, ammunition, 3 cylinder targets, Input System actions and spray ending saved. Backup: " + backup);
        }
        private static void Pickup(GameObject g, Level1WeaponTrial trial, bool ammunition)
        {
            g.layer = LayerMask.NameToLayer("Interactable");
            var p = Undo.AddComponent<WeaponTrialPickup>(g); Set(p, "trial", trial); Set(p, "ammunition", ammunition);
            p.interactableName = ammunition ? "Cargadores FAL" : "FN FAL";
            p.UIInteraction = new UIInteraction { label = ammunition ? "Recoger munición" : "Recoger FN FAL", interactionType = EInteractions.Spacebar };
            foreach (string field in new[] { "stateDescription", "focusInteraction", "inspectInteraction", "subInteractions" }) Set(p, field, false);
        }
        private static GameObject Model(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/InspectionSystem/Assets/FN-FAL/FN FAL.fbx");
            if (prefab == null) throw new InvalidOperationException("FN FAL model is missing.");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(model, "Create FAL model instance"); model.transform.SetParent(parent, false);
            CleanImportedModel(model);
            return model;
        }
        private static void CleanImportedModel(GameObject root)
        {
            // The source FBX contains Blender studio cameras/lights. Keep only the rifle presentation.
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                Undo.RecordObject(camera, "Disable imported studio camera"); camera.enabled = false;
                var additional = camera.GetComponent<UniversalAdditionalCameraData>();
                if (additional != null) Undo.DestroyObjectImmediate(additional);
                Undo.DestroyObjectImmediate(camera);
            }
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                Undo.RecordObject(light, "Disable imported studio light"); light.enabled = false;
                var additional = light.GetComponent<UniversalAdditionalLightData>();
                if (additional != null) Undo.DestroyObjectImmediate(additional);
                Undo.DestroyObjectImmediate(light);
            }
        }
        private static void CenterModel(GameObject model, float length)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds; foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            model.transform.localScale *= length / Mathf.Max(b.size.x, b.size.y, b.size.z);
            b = renderers[0].bounds; foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            model.transform.position += model.transform.parent.position - b.center;
            PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
        }
        private static WeaponDefinition Definition()
        {
            string path = Root + "/Data/WD_Level1_FAL.asset";
            var d = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (d != null) return d;
            d = ScriptableObject.CreateInstance<WeaponDefinition>();
            Set(d, "displayName", "FN FAL"); Set(d, "fireInterval", 0.11f); Set(d, "hipSpreadDegrees", 1.3f); Set(d, "aimSpreadDegrees", 0.2f);
            AssetDatabase.CreateAsset(d, path); return d;
        }
        private static InputActionAsset Actions()
        {
            string path = Root + "/Data/Level1WeaponInput.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset != null) return asset;
            var temp = ScriptableObject.CreateInstance<InputActionAsset>(); var map = temp.AddActionMap("Weapons");
            map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            File.WriteAllText(path, temp.ToJson()); UnityEngine.Object.DestroyImmediate(temp);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        private static InputActionReference Reference(InputActionAsset asset, string name)
        {
            string path = Root + "/Data/Level1_" + name + ".asset";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (reference == null) { reference = InputActionReference.Create(asset.FindAction("Weapons/" + name, true)); AssetDatabase.CreateAsset(reference, path); }
            return reference;
        }
        private static AudioClip ShotSound()
        {
            // Temporary dry shot synthesized locally, replaceable by a final licensed recording.
            string path = Root + "/Data/FAL_TestShot.wav";
            if (!File.Exists(path))
            {
                using var file = File.Create(path); using var writer = new BinaryWriter(file);
                const int rate = 22050, samples = 4410;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                var random = new System.Random(41);
                for (int i = 0; i < samples; i++) { double t = i / (double)rate; double sample = (random.NextDouble() * 2 - 1) * Math.Exp(-t * 42) + Math.Sin(t * 2 * Math.PI * 95) * Math.Exp(-t * 26) * 0.45; writer.Write((short)(Math.Clamp(sample, -1, 1) * 23000)); }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static Material Material(string name, Color color)
        {
            string path = Root + "/Data/Level1_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(m, path); }
            return m;
        }
        private static Vector3 Ground(Vector3 position)
        {
            // Ignore narrative volumes and interactable props when finding the floor.
            if (Physics.Raycast(position + Vector3.up * 3, Vector3.down, out var hit, 30, ~(1 << 6 | 1 << 7 | 1 << 9 | 1 << 10), QueryTriggerInteraction.Ignore)) return hit.point;
            return position;
        }
        private static GameObject Create(string name, Transform parent, params Type[] components)
        {
            var g = new GameObject(name, components); Undo.RegisterCreatedObjectUndo(g, "Create " + name); if (parent != null) g.transform.SetParent(parent, false); return g;
        }
        private static GameObject Primitive(string name, PrimitiveType type, Transform parent)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; Undo.RegisterCreatedObjectUndo(g, "Create " + name); g.transform.SetParent(parent, false); return g;
        }
        private static TextMeshProUGUI Text(string name, Transform parent, float size, TextAlignmentOptions alignment)
        {
            var t = Create(name, parent, typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); t.font = TMP_Settings.defaultFontAsset; t.fontSize = size; t.alignment = alignment; t.raycastTarget = false; return t;
        }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static void SetLayer(GameObject g, int layer) { foreach (Transform t in g.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = layer; if (PrefabUtility.IsPartOfPrefabInstance(t)) { PrefabUtility.RecordPrefabInstancePropertyModifications(t); PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject); } } }
        private static int EnsureViewLayer()
        {
            int existing = LayerMask.NameToLayer("WeaponView"); if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); var layers = tags.FindProperty("layers");
            for (int i = 10; i < layers.arraySize; i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layers.GetArrayElementAtIndex(i).stringValue = "WeaponView"; tags.ApplyModifiedProperties(); return i; }
            throw new InvalidOperationException("No free layer for WeaponView.");
        }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var s = new SerializedObject(target); var p = s.FindProperty(field); if (p == null) throw new InvalidOperationException(target.GetType().Name + "." + field);
            if (value is UnityEngine.Object o) p.objectReferenceValue = o;
            else if (value == null) p.objectReferenceValue = null;
            else if (value is bool b) p.boolValue = b;
            else if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is string text) p.stringValue = text;
            s.ApplyModifiedProperties();
        }
        private static T[] InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void RunTests()
        {
            RegisterTests(); SessionState.SetBool("Level1WeaponsTesting", true); Report("testing", "Running weapon tests.");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "Level1WeaponIntegrationTests", "WeaponSystemTests" } }));
        }
        private static void RegisterTests() { runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results()); }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                SessionState.SetBool("Level1WeaponsTesting", false);
                File.WriteAllText("Library/Level1Weapons.tests.xml", result.ToXml().OuterXml);
                Report(result.FailCount == 0 ? "tests-passed" : "tests-failed", $"Passed {result.PassCount}, failed {result.FailCount}. {result.Message}");
            }
        }
    }
}
#endif
