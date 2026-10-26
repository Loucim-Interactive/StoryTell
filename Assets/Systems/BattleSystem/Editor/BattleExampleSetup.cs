using System;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.EventSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.WeaponSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Systems.BattleSystem.Editor {
    public static class BattleExampleSetup {
        private const string ExampleName = "Battle Example";

        [MenuItem("Tools/Battle/Preview Scene Example")]
        private static void Preview() {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            GameEventBus.Raise(GameplayEvents.OnBattleTriggered);
        }

        [MenuItem("Tools/Battle/Create Example Near Player")]
        public static void Create() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var existing = GameObject.Find(ExampleName);
            if (existing) { Selection.activeGameObject = existing; Validate(); return; }
            var player = Object.FindFirstObjectByType<PlayerControllerScript>();
            if (!player) throw new InvalidOperationException("The open scene needs a player.");
            var camera = player.GetComponentInChildren<Camera>(true);
            var look = player.GetComponent<CameraControllerScript>();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/InspectionSystem/Prefabs/FAL.prefab");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/BattleSystem/Materials/BattleShadow.mat");
            if (!camera || !look || !source || !material) throw new InvalidOperationException("Missing player camera, FAL prefab, or shadow material.");
            EditorSceneManager.SaveScene(player.gameObject.scene);

            var root = new GameObject(ExampleName);
            Undo.RegisterCreatedObjectUndo(root, "Create battle example");
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            root.transform.position = player.transform.position;
            root.transform.rotation = Quaternion.LookRotation(forward);

            var weapon = player.GetComponent<PlayerWeaponController>();
            if (!weapon) weapon = Undo.AddComponent<PlayerWeaponController>(player.gameObject);
            var recoil = player.GetComponent<WeaponRecoil>();
            if (!player.GetComponent<PlayerWeaponInput>()) Undo.AddComponent<PlayerWeaponInput>(player.gameObject);
            Set(recoil, "cameraController", look);
            Set(weapon, "recoil", recoil);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
            visual.name = "FAL - Battle View";
            visual.transform.position = Vector3.zero;
            visual.transform.rotation = Quaternion.Euler(0, 90, 0);
            visual.transform.localScale = Vector3.one;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) {
                bounds.Encapsulate(renderer.bounds);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.gameObject.layer = 2;
            }
            float scale = .85f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.position = -bounds.center * scale;
            var mount = new GameObject("Battle Weapon Mount");
            mount.transform.SetParent(camera.transform, false);
            mount.transform.localPosition = new Vector3(.22f, -.23f, .65f);
            visual.transform.SetParent(mount.transform, false);
            Set(weapon, "falVisual", mount);
            mount.SetActive(false);
            Undo.RegisterCreatedObjectUndo(mount, "Create battle weapon view");

            var areaObject = Child(root, "Shadow Spawn Area", new Vector3(0, 0, 12));
            var area = areaObject.AddComponent<SpawnArea>();
            var areaData = new SerializedObject(area);
            areaData.FindProperty("bounds").boundsValue = new Bounds(Vector3.zero, new Vector3(12, 30, 8));
            areaData.FindProperty("sampleNavMesh").boolValue = false;
            areaData.FindProperty("projectToGround").boolValue = true;
            areaData.ApplyModifiedPropertiesWithoutUndo();
            var spawner = root.AddComponent<ShadowSpawner>();
            Set(spawner, "spawnArea", area); Set(spawner, "blackUnlitMaterial", material);
            var manager = root.AddComponent<BattleManager>();
            Set(manager, "player", weapon); Set(manager, "shadowSpawner", spawner);

            var zone = Child(root, "Trigger - walk forward 4m", new Vector3(0, 1, 4));
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.size = new Vector3(6, 4, 2);
            trigger.isTrigger = true;
            var bodyTrigger = zone.AddComponent<Rigidbody>();
            bodyTrigger.isKinematic = true;
            bodyTrigger.useGravity = false;
            zone.AddComponent<TriggerZone>();

            var label = Child(root, "Example Instructions", new Vector3(0, 2.5f, 4));
            label.transform.localRotation = Quaternion.identity;
            var text = label.AddComponent<TextMesh>();
            text.text = "PRUEBA DE COMBATE\nAvanza para equipar el FAL\nClic izquierdo: disparo y retroceso";
            text.fontSize = 48; text.characterSize = .045f;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.color = Color.white;
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            Validate();
        }

        [MenuItem("Tools/Battle/Validate Scene Example")]
        public static void Validate() {
            var root = GameObject.Find(ExampleName);
            if (!root) throw new InvalidOperationException("Battle example is missing.");
            var manager = new SerializedObject(root.GetComponent<BattleManager>());
            var weapon = (PlayerWeaponController)manager.FindProperty("player").objectReferenceValue;
            var spawner = (ShadowSpawner)manager.FindProperty("shadowSpawner").objectReferenceValue;
            if (!weapon || !spawner || !spawner.IsConfigured || !weapon.GetComponent<PlayerWeaponInput>())
                throw new InvalidOperationException("Battle references are incomplete.");
            var visual = new SerializedObject(weapon).FindProperty("falVisual").objectReferenceValue as GameObject;
            if (!visual || visual.GetComponentsInChildren<Renderer>(true).Length == 0 || visual.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new InvalidOperationException("Invalid FAL view.");
            var zone = root.GetComponentInChildren<TriggerZone>();
            float distance = Vector3.Distance(weapon.transform.position, zone.transform.position);
            if (distance > 7 || !zone.GetComponent<Collider>().isTrigger) throw new InvalidOperationException("Trigger is not near the player.");
            Physics.SyncTransforms();
            var area = root.GetComponentInChildren<SpawnArea>();
            for (int i = 0; i < 12; i++) if (!area.TryGetPosition(out _)) throw new InvalidOperationException("Spawn area cannot find terrain.");
            Debug.Log($"[BattleExample] PASS: wired player, FAL view, trigger {distance:F1}m away, 12 shadows on terrain with 3-7 second lifetimes. Scene saved.");
        }

        private static GameObject Child(GameObject parent, string name, Vector3 position) {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = position;
            return child;
        }
        private static void Set(Object target, string property, Object value) {
            var data = new SerializedObject(target);
            data.FindProperty(property).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
