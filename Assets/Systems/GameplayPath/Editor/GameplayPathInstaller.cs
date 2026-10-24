#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using InteractionSystem.Scripts.Utils;
using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using Systems.DialogueSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Utils;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace StoryTell.GameplayPath.Editor
{
    [InitializeOnLoad]
    public static class GameplayPathInstaller
    {
        public const string ScenePath = "Assets/Scenes/Level1.unity";
        public const string AssetRoot = "Assets/Systems/GameplayPath";
        private static double nextPoll;
        static GameplayPathInstaller() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists("Library/GameplayPath.request")) return;
            string action = File.ReadAllText("Library/GameplayPath.request").Trim(); File.Delete("Library/GameplayPath.request");
            try
            {
                if (action == "apply") Apply();
                else if (action == "tests") GameplayPathVerification.Run();
                else if (action == "capture") Capture();
                else if (action == "polish") Polish();
                else Report("inspected", SceneManager.GetActiveScene().path + " dirty=" + SceneManager.GetActiveScene().isDirty + " playing=" + EditorApplication.isPlaying);
            }
            catch (Exception e) { Report("error", e.ToString()); Debug.LogException(e); }
        }
        [Serializable] private sealed class Result { public string status, message; }
        internal static void Report(string status, string message) => File.WriteAllText("Library/GameplayPath-report.json", JsonUtility.ToJson(new Result { status = status, message = message }, true));
        [MenuItem("StoryTell/Gameplay Path/Apply approved Scene 1 route")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || SceneManager.sceneCount != 1) throw new InvalidOperationException("Open only Level1 before authoring.");
            if (InScene<GameplayPathController>(scene).Any()) throw new InvalidOperationException("GameplayPath is already installed; do not duplicate it.");
            Directory.CreateDirectory("Library/GameplayPathBackups");
            string backup = "Library/GameplayPathBackups/Level1-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not preserve open Level1 including unsaved edits.");
            Directory.CreateDirectory(AssetRoot + "/Data"); AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build coherent Scene 1 GameplayPath");
            var player = InScene<PlayerControllerScript>(scene).Single();
            var trial = InScene<Level1WeaponTrial>(scene).Single();
            var map = InScene<PhysicalMapController>(scene).Single();
            var dialogue = InScene<DialogueManagerScript>(scene).Single();
            var camera = Camera.main != null ? Camera.main : player.GetComponentInChildren<Camera>();
            Transform gameplay = scene.GetRootGameObjects().Single(g => g.name == "====GAMEPLAY====").transform;
            // Preserve every legacy object, while retiring its conflicting prototype mission behavior.
            foreach (var speaker in InScene<SpeakerScript>(scene)) { Undo.RecordObject(speaker, "Retire old conversations"); speaker.enabled = false; }
            foreach (var target in InScene<WeaponCylinderTarget>(scene)) { Undo.RecordObject(target.gameObject, "Retire stationary trial targets"); target.gameObject.SetActive(false); }
            foreach (var pickup in InScene<WeaponTrialPickup>(scene)) { Undo.RecordObject(pickup.gameObject, "Retire spawn trial pickups"); pickup.gameObject.SetActive(false); }
            foreach (var behaviour in InScene<MonoBehaviour>(scene))
                if (behaviour != null && (behaviour.GetType().Name == "WalkieTalkieTriggerZone" || behaviour.gameObject.name == "LevelEnd"))
                { Undo.RecordObject(behaviour, "Retire previous quest trigger"); behaviour.enabled = false; }
            var root = Create("GameplayPath_Level1", gameplay);
            var controller = Undo.AddComponent<GameplayPathController>(root);
            var battle = Undo.AddComponent<GameplayPathBattle>(Create("Battle_45Enemies", root.transform));
            var definition = CreateDefinition();
            Set(controller, "definition", definition); Set(controller, "player", player.transform); Set(controller, "movement", player);
            Set(controller, "look", player.GetComponent<CameraControllerScript>()); Set(controller, "interaction", player.GetComponent<InteractionScript>());
            Set(controller, "dialogue", dialogue); Set(controller, "map", map); Set(controller, "trial", trial); Set(controller, "battle", battle);
            Set(trial, "gameplayPath", controller);
            var input = CreateInput(); Set(controller, "closeReading", Ref(input, "CloseReading")); Set(controller, "readMother", Ref(input, "ReadMother"));
            UI(root.transform, controller);
            Vector3 start = Ground(player.transform.position + new Vector3(2.5f, 0, 0.5f));
            var rubenStart = Actor("Ruben_01_Arrival", start, player.transform.position, root.transform, controller, PathAction.Ruben, "Hablar con Rubén");
            Vector3 kitchenPosition = Ground(new Vector3(89, 30, 292));
            var kitchenRoot = Create("Kitchen_CampLife", root.transform);
            var kitchenTable = Prop("Kitchen_Table", "Assets/Art/Outsourced/Props/Defensive_props/Prefabs/tables/wooden_table.prefab", kitchenPosition, 2.8f, kitchenRoot.transform);
            var meal = Primitive("Jarro_Comida", PrimitiveType.Cylinder, kitchenRoot.transform, kitchenPosition + new Vector3(0, 1.05f, 0), new Vector3(0.24f, 0.18f, 0.24f), Mat("Tin", new Color(0.34f, 0.38f, 0.37f)));
            Interactable(meal, controller, PathAction.Eat, "Comer junto a Gómez", false);
            Actor("Gomez_Cocina", Ground(kitchenPosition + new Vector3(0, 0, 2)), kitchenPosition, kitchenRoot.transform, controller, PathAction.Eat, "Hablar con Gómez y comer");
            var pot = Primitive("Olla_Campana", PrimitiveType.Cylinder, kitchenRoot.transform, Ground(kitchenPosition + new Vector3(2.2f, 0, 1.5f)) + Vector3.up * 0.45f, new Vector3(0.8f, 0.35f, 0.8f), Mat("Tin", new Color(0.34f, 0.38f, 0.37f)));
            Primitive("Tapa_Olla", PrimitiveType.Cylinder, kitchenRoot.transform, pot.transform.position + Vector3.up * 0.38f, new Vector3(0.9f, 0.04f, 0.9f), Mat("Tin", Color.gray));
            Sign("COCINA", Ground(kitchenPosition + new Vector3(-2.7f, 0, -1.7f)), 180, kitchenRoot.transform);
            var rubenKitchen = Actor("Ruben_02_Cocina_Carta", Ground(kitchenPosition + new Vector3(-3, 0, 2)), player.transform.position, kitchenRoot.transform, controller, PathAction.Ruben, "Hablar con Rubén / devolver carta");
            rubenKitchen.SetActive(false);
            Equipment(root.transform, controller, trial, Ground(new Vector3(78, 29, 277)));
            var letterArea = Create("LetterSearch_FourFragments", root.transform);
            Vector3[] fragmentPositions = { kitchenPosition + new Vector3(-2, 0, 5), kitchenPosition + new Vector3(4, 0, 5), kitchenPosition + new Vector3(7, 0, 0), kitchenPosition + new Vector3(-4, 0, -4) };
            for (int i = 0; i < fragmentPositions.Length; i++)
            {
                Vector3 ground = Ground(fragmentPositions[i]);
                var support = Prop("Carta_Apoyo_" + i, "Assets/Prefabs/World/Crates/SmallBox.prefab", ground, 0.85f, letterArea.transform);
                float top = Bounds(support).max.y;
                var paper = Primitive("Carta_Madre_Fragmento_" + (i + 1), PrimitiveType.Cube, letterArea.transform, new Vector3(ground.x, top + 0.025f, ground.z), new Vector3(0.42f, 0.035f, 0.29f), Mat("Paper", new Color(0.78f, 0.73f, 0.60f)));
                var interactable = Interactable(paper, controller, PathAction.Fragment, "Recoger fragmento de la carta", true); Set(interactable, "fragmentIndex", i);
                var ink = WorldText("Tinta", "Rubén,\n... mamá", paper.transform, 0.12f); ink.transform.localPosition = new Vector3(0, 0.53f, 0); ink.transform.localRotation = Quaternion.Euler(90, 0, 0); ink.transform.localScale = new Vector3(1 / 0.42f, 1 / 0.035f, 1 / 0.29f); ink.color = new Color(0.16f, 0.14f, 0.10f);
            }
            OptionalReadings(letterArea.transform, controller, kitchenPosition);
            Vector3 sergeantPosition = Ground(new Vector3(97, 29, 270));
            Actor("Sargento_Acosta_CarpaPrincipal", sergeantPosition, player.transform.position, root.transform, controller, PathAction.Sergeant, "Hablar con el sargento Acosta");
            Sign("CARPA PRINCIPAL", Ground(sergeantPosition + new Vector3(2, 0, 0)), 180, root.transform);
            var crate = Prop("Caja_Herramientas_Mantas_Recoger", "Assets/Prefabs/World/Crates/MediumBox.prefab", Ground(sergeantPosition + new Vector3(1, 0, -1.5f)), 0.85f, root.transform);
            Interactable(crate, controller, PathAction.Crate, "Llevar herramientas y mantas al puesto", true);
            var heldCrate = Prop("Caja_EnManos", "Assets/Prefabs/World/Crates/MediumBox.prefab", Vector3.zero, 0.65f, camera.transform);
            heldCrate.transform.localPosition = new Vector3(0, -0.5f, 0.85f); heldCrate.transform.localRotation = Quaternion.identity;
            foreach (var collider in heldCrate.GetComponentsInChildren<Collider>()) Undo.DestroyObjectImmediate(collider);
            heldCrate.SetActive(false); Set(controller, "heldCrate", heldCrate);
            var food = Primitive("Jarro_EnManos", PrimitiveType.Cylinder, camera.transform, Vector3.zero, new Vector3(0.18f, 0.12f, 0.18f), Mat("Tin", Color.gray));
            food.transform.localPosition = new Vector3(0.2f, -0.25f, 0.5f); Undo.DestroyObjectImmediate(food.GetComponent<Collider>()); food.SetActive(false); Set(controller, "eatingVisual", food);
            Vector3 campPosition = Ground(new Vector3(114, 39, 361));
            var frontCamp = Create("MiniCamp_AfterNarration5", root.transform);
            Prop("Carpa_PuestoAvanzado", "Assets/Art/Outsourced/Props/Military_Free/Prefabs/Buildings/Tent_002.prefab", campPosition + new Vector3(4, 0, 4), 6, frontCamp.transform);
            Actor("Oficial_Recepcion_Caja", campPosition, Ground(new Vector3(110, 38, 340)), frontCamp.transform, controller, PathAction.Officer, "Entregar la caja al oficial");
            var delivered = Prop("Caja_Entregada", "Assets/Prefabs/World/Crates/MediumBox.prefab", Ground(campPosition + new Vector3(1.3f, 0, 0)), 0.85f, frontCamp.transform); delivered.SetActive(false); Set(controller, "deliveredCrate", delivered);
            var rubenFront = Actor("Ruben_03_Frente", Ground(campPosition + new Vector3(-2.5f, 0, 3)), campPosition, frontCamp.transform, controller, PathAction.Ruben, "Hablar con Rubén"); rubenFront.SetActive(false);
            Set(controller, "rubenAtStart", rubenStart); Set(controller, "rubenAtKitchen", rubenKitchen); Set(controller, "rubenAtFront", rubenFront);
            Sign("PUESTO AVANZADO", Ground(campPosition + new Vector3(0, 0, -4)), 180, frontCamp.transform);
            Sign("PUESTO →", Ground(new Vector3(106, 38, 335)), 180, root.transform);
            Sign("PASO AL FRENTE →", Ground(new Vector3(102, 42, 383)), 180, root.transform);
            Sign("FRENTE ↑", Ground(new Vector3(77, 53, 416)), 180, root.transform);
            Sign("FRENTE ↑", Ground(new Vector3(60, 56, 468)), 180, root.transform);
            var narration5 = InScene<Transform>(scene).First(t => t.name == "Narration zone 5");
            foreach (var behaviour in narration5.GetComponents<MonoBehaviour>()) if (behaviour != null && behaviour.GetType().Name == "CharacterNarrationZoneScript")
            { Set(behaviour, "characterNarrationText", "El puesto tiene que estar del otro lado de este paso. La caja empieza a pesar."); Set(behaviour, "canRepeat", false); }
            Battlefield(root.transform, controller, battle, definition, trial, scene);
            // Consistent UI scaling without altering other scenes or shared prefabs.
            foreach (var scaler in InScene<CanvasScaler>(scene))
            { Undo.RecordObject(scaler, "Scale Level1 UI with resolution"); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f; }
            Undo.CollapseUndoOperations(group); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Level1 could not be saved.");
            Report("installed", "Scene 1 route, dialogue assets, objects, mini camp, navigation and 45-enemy battle saved. Open-scene backup: " + backup);
            Capture();
        }
        private static void Equipment(Transform root, GameplayPathController controller, Level1WeaponTrial trial, Vector3 position)
        {
            var area = Create("Equipment_FiveItems", root);
            var table = Prop("Mesa_Suministros", "Assets/Art/Outsourced/Props/Defensive_props/Prefabs/tables/wooden_table.prefab", position, 3, area.transform);
            float y = Bounds(table).max.y + 0.08f;
            var helmet = Create("Casco_Recoger", area.transform); helmet.transform.position = new Vector3(position.x - 0.8f, y, position.z);
            var filter = Undo.AddComponent<MeshFilter>(helmet); filter.sharedMesh = HelmetMesh();
            Undo.AddComponent<MeshRenderer>(helmet).sharedMaterial = Mat("Olive", new Color(0.22f, 0.25f, 0.16f));
            var helmetCollider = Undo.AddComponent<BoxCollider>(helmet); helmetCollider.center = Vector3.up * 0.1f; helmetCollider.size = new Vector3(0.4f, 0.23f, 0.44f);
            Interactable(helmet, controller, PathAction.Helmet, "Recoger casco", true);
            var rifle = Prop("FAL_Recoger_Equipo", "Assets/Systems/InspectionSystem/Assets/FN-FAL/FN FAL.fbx", Ground(position + new Vector3(0, 0, -1.6f)), 1, area.transform);
            Interactable(rifle, controller, PathAction.Rifle, "Recoger FN FAL", true);
            var ammo = Prop("Municion_Equipo", "Assets/Art/Outsourced/Props/Defensive_props/Prefabs/defence/ammo_box.prefab", new Vector3(position.x + 0.8f, y, position.z), 0.45f, area.transform);
            Interactable(ammo, controller, PathAction.Ammunition, "Recoger tres cargadores", true);
            var paper = Primitive("Mapa_Recoger", PrimitiveType.Cube, area.transform, new Vector3(position.x, y, position.z + 0.5f), new Vector3(0.55f, 0.035f, 0.4f), Mat("Paper", new Color(0.78f, 0.73f, 0.60f)));
            Interactable(paper, controller, PathAction.Map, "Recoger mapa y lista de tareas", true);
            var radio = Prop("Radio_Recoger", "Assets/Prefabs/World/Electronic/Radio.prefab", Ground(position + new Vector3(1.6f, 0, -1.5f)), 0.65f, area.transform);
            Interactable(radio, controller, PathAction.Radio, "Recoger radio", true);
            Sign("EQUIPO", Ground(position + new Vector3(-2.6f, 0, 0)), 180, area.transform);
        }
        private static void Battlefield(Transform root, GameplayPathController controller, GameplayPathBattle battle, GameplayPathDefinition definition, Level1WeaponTrial trial, Scene scene)
        {
            var field = Create("Battlefield_ExistingExplosionZone", root);
            Vector3 center = Ground(new Vector3(53, 57, 527));
            Vector3[] blockers = { new(-10,0,13), new(7,0,16), new(0,0,5), new(-12,0,-4), new(12,0,-7), new(1,0,-15) };
            for (int i = 0; i < blockers.Length; i++)
            {
                var obstacle = Prop("Cobertura_" + i, "Assets/Art/Outsourced/Props/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab", Ground(center + blockers[i]), 5, field.transform);
                obstacle.transform.rotation = Quaternion.Euler(0, i % 2 == 0 ? 20 : -25, 0);
            }
            for (int i = 0; i < 2; i++)
            {
                var supply = Prop("Municion_Reserva_" + i, "Assets/Art/Outsourced/Props/Defensive_props/Prefabs/defence/ammo_box.prefab", Ground(center + new Vector3(i == 0 ? -10 : 10, 0, -18)), 0.8f, field.transform);
                var component = Undo.AddComponent<GameplayPathSupply>(supply); SetupInteraction(component, "Recoger cargadores de reserva");
                Set(component, "controller", controller); Set(component, "trial", trial); Set(component, "magazines", 12); SetLayer(supply, 6);
            }
            var gate = Create("BattleEntrance_AfterDelivery", field.transform); gate.transform.position = Ground(center + new Vector3(3, 0, -26)) + Vector3.up * 2;
            var gateCollider = Undo.AddComponent<BoxCollider>(gate); gateCollider.isTrigger = true; gateCollider.size = new Vector3(34, 10, 10);
            Set(Undo.AddComponent<GameplayPathBattleGate>(gate), "controller", controller);
            var trialSerialized = new SerializedObject(trial); var useZone = (BoxCollider)trialSerialized.FindProperty("useZone").objectReferenceValue;
            Undo.RecordObject(useZone.transform, "Move weapon use zone to battle"); useZone.transform.position = center + Vector3.up * 4;
            Undo.RecordObject(useZone, "Size battle weapon zone"); useZone.size = new Vector3(100, 45, 100);
            var routeHost = Create("Routes_BehindCover", field.transform);
            var routes = new BattleRoute[3];
            Vector3[][] offsets = {
                new[] {new Vector3(-20,0,30),new Vector3(-16,0,16),new Vector3(-7,0,11),new Vector3(-7,0,1),new Vector3(-17,0,-4)},
                new[] {new Vector3(1,0,33),new Vector3(3,0,23),new Vector3(-3,0,14),new Vector3(4,0,7),new Vector3(5,0,-6)},
                new[] {new Vector3(22,0,29),new Vector3(16,0,19),new Vector3(3,0,19),new Vector3(16,0,6),new Vector3(17,0,-10)} };
            for (int lane = 0; lane < 3; lane++)
            {
                var routeObject = Create(new[] {"Left", "Center", "Right"}[lane], routeHost.transform); var points = new Transform[offsets[lane].Length];
                for (int point = 0; point < points.Length; point++) { points[point] = Create("Point_" + point, routeObject.transform).transform; points[point].position = Ground(center + offsets[lane][point]); }
                routes[lane] = new BattleRoute {name = routeObject.name, points = points};
            }
            var navObject = Create("Navigation_GameplayPath", root); navObject.transform.position = new Vector3(85, 45, 410);
            var surface = Undo.AddComponent<NavMeshSurface>(navObject); surface.collectObjects = CollectObjects.Volume; surface.center = Vector3.zero; surface.size = new Vector3(180, 160, 370);
            surface.layerMask = (1 << 0) | (1 << 3); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = 0.2f;
            Physics.SyncTransforms(); surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Could not bake GameplayPath navigation.");
            surface.navMeshData.name = "NavMesh_GameplayPath_Level1";
            AssetDatabase.CreateAsset(surface.navMeshData, AssetRoot + "/Data/NavMesh_GameplayPath_Level1.asset");
            var pool = new GameplayPathEnemy[8];
            for (int i = 0; i < pool.Length; i++)
            {
                var enemy = Create("Enemy_Pool_" + i, battle.transform);
                var body = Primitive("Silueta_Cuerpo", PrimitiveType.Cylinder, enemy.transform, Vector3.zero, new Vector3(0.65f, 0.75f, 0.65f), Mat("EnemySilhouette", new Color(0.08f,0.09f,0.10f)));
                body.transform.localPosition = Vector3.up * 0.85f; Undo.DestroyObjectImmediate(body.GetComponent<Collider>());
                var head = Primitive("Silueta_Cabeza", PrimitiveType.Sphere, enemy.transform, Vector3.zero, Vector3.one * 0.36f, Mat("EnemySilhouette", Color.black)); head.transform.localPosition = Vector3.up * 1.7f; Undo.DestroyObjectImmediate(head.GetComponent<Collider>());
                var collider = Undo.AddComponent<CapsuleCollider>(enemy); collider.center = Vector3.up * 0.9f; collider.height = 1.9f; collider.radius = 0.34f;
                var agent = Undo.AddComponent<NavMeshAgent>(enemy); agent.radius = 0.35f; agent.height = 1.9f; agent.stoppingDistance = 0.25f; agent.acceleration = 12; agent.angularSpeed = 360;
                pool[i] = Undo.AddComponent<GameplayPathEnemy>(enemy); enemy.SetActive(false);
            }
            Set(battle, "controller", controller); Set(battle, "definition", definition); SetArray(battle, "pool", pool);
            var serialized = new SerializedObject(battle); var routeProperty = serialized.FindProperty("routes"); routeProperty.arraySize = routes.Length;
            for (int i = 0; i < routes.Length; i++) { var element = routeProperty.GetArrayElementAtIndex(i); element.FindPropertyRelative("name").stringValue = routes[i].name; var points = element.FindPropertyRelative("points"); points.arraySize = routes[i].points.Length; for (int j = 0; j < points.arraySize; j++) points.GetArrayElementAtIndex(j).objectReferenceValue = routes[i].points[j]; }
            serialized.ApplyModifiedProperties();
            var vfx = InScene<Transform>(scene).First(t => t.name == "VFX" && t.parent != null && t.parent.name == "====GAMEPLAY====");
            SetArray(battle, "explosions", vfx.GetComponentsInChildren<ParticleSystem>(true).Where(ps => ps.name.IndexOf("fog", StringComparison.OrdinalIgnoreCase) < 0).ToArray());
            // Existing trigger assets are preserved; the path gate owns combat progression.
        }
        public static void Polish()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before changing routes.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("Open Level1.");
            var root=InScene<GameplayPathController>(scene).Single().transform;
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Validate and polish GameplayPath route");
            foreach(var oldNpc in InScene<MonoBehaviour>(scene).Where(x=>x!=null&&!x.transform.IsChildOf(root)&&x.GetType().Name=="NPCInteractableScript")){Undo.RecordObject(oldNpc,"Disable legacy mission interaction");oldNpc.enabled=false;}
            foreach(var oldEnd in InScene<Transform>(scene).Where(x=>x.name=="LevelEnd")){Undo.RecordObject(oldEnd.gameObject,"Retire legacy ending");oldEnd.gameObject.SetActive(false);}
            foreach(var text in root.GetComponentsInChildren<TextMeshPro>(true))
            {Undo.RecordObject(text,"Make environmental signs legible");text.fontSize=text.name=="Rotulo"?2.5f:1.2f;}
            var gate=InScene<GameplayPathBattleGate>(scene).Single();
            var vfx=InScene<Transform>(scene).First(t=>t.name=="VFX"&&t.parent!=null&&t.parent.name=="====GAMEPLAY====");
            var triggers=vfx.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b!=null&&b.GetType().Name=="VFXColliderTrigger").Select(b=>b.GetComponent<Collider>()).Where(c=>c!=null).ToArray();
            foreach(var trigger in triggers){Undo.RecordObject(trigger,"Keep explosion trigger out of collision navigation");trigger.isTrigger=true;}
            var vfxGate=vfx.GetComponent<GameplayPathVfxGate>();if(vfxGate==null)vfxGate=Undo.AddComponent<GameplayPathVfxGate>(vfx.gameObject);
            Set(vfxGate,"controller",root.GetComponent<GameplayPathController>());SetArray(vfxGate,"explosionTriggers",triggers);
            var field=root.Find("Battlefield_ExistingExplosionZone");
            if(field.position.z<1)
            {
                Undo.RecordObject(field,"Place combat after existing explosions");field.position+=Vector3.forward*36;
                foreach(var child in field.Cast<Transform>())if(child.name.StartsWith("Cobertura_")||child.name.StartsWith("Municion_Reserva_")){Undo.RecordObject(child,"Seat combat props on terrain");child.position=Ground(child.position);}
                foreach(var point in field.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Point_"))){Undo.RecordObject(point,"Ground relocated route");point.position=Ground(point.position);}
                Undo.RecordObject(gate.transform,"Put combat entrance beyond bombs");gate.transform.position=Ground(new Vector3(56,0,543))+Vector3.up*2;
                var box=gate.GetComponent<BoxCollider>();Undo.RecordObject(box,"Size exit of bomb zone");box.size=new Vector3(34,10,6);
                var trial=InScene<Level1WeaponTrial>(scene).Single();var trialData=new SerializedObject(trial);var zone=(BoxCollider)trialData.FindProperty("useZone").objectReferenceValue;
                Undo.RecordObject(zone.transform,"Follow combat use zone");zone.transform.position=Ground(new Vector3(53,0,563))+Vector3.up*4;
                var surface=InScene<NavMeshSurface>(scene).Single();Undo.RecordObject(surface,"Expand connected battle navigation");surface.transform.position=new Vector3(85,45,430);surface.size=new Vector3(180,160,450);
                Physics.SyncTransforms();surface.BuildNavMesh();var meshPath=AssetRoot+"/Data/NavMesh_GameplayPath_Level1.asset";
                var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(meshPath);EditorUtility.CopySerialized(surface.navMeshData,previous);surface.RemoveData();Set(surface,"m_NavMeshData",previous);surface.AddData();EditorUtility.SetDirty(previous);
            }
            if(!NavMesh.SamplePosition(gate.transform.position,out var entrance,10,NavMesh.AllAreas))throw new InvalidOperationException("Battle entrance is not navigable.");
            foreach(var route in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Point_")))
            {
                Vector3 candidate=ReachableNear(route.position,entrance.position);Undo.RecordObject(route,"Keep battle route on connected ground");route.position=candidate;
            }
            foreach(var component in root.GetComponentsInChildren<Component>(true))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Report("polished","Battle route points connected, legacy mission interactions disabled and signs enlarged.");Capture();
        }
        private static Vector3 ReachableNear(Vector3 desired,Vector3 target)
        {
            var path=new NavMeshPath();
            for(float radius=0;radius<=12;radius+=1)
            for(int sample=0;sample<(radius==0?1:24);sample++)
            {
                float a=sample*Mathf.PI*2/24;Vector3 p=desired+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                if(!NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas))continue;
                if(NavMesh.CalculatePath(hit.position,target,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return hit.position;
            }
            throw new InvalidOperationException("No connected navigation near "+desired);
        }
        private static void UI(Transform parent, GameplayPathController controller)
        {
            var host = Create("GameplayPath_UI", parent, typeof(RectTransform)); var canvas = Undo.AddComponent<Canvas>(host); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 29000;
            var scaler = Undo.AddComponent<CanvasScaler>(host); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 0.5f;
            var notice = UIText("ContextAndObjective", host.transform, 26); notice.rectTransform.anchorMin = new Vector2(0.15f, 0); notice.rectTransform.anchorMax = new Vector2(0.85f, 0); notice.rectTransform.pivot = new Vector2(0.5f,0); notice.rectTransform.anchoredPosition = new Vector2(0, 105); notice.rectTransform.sizeDelta = new Vector2(0,90); notice.alignment = TextAlignmentOptions.Center; Set(controller,"noticeText",notice);
            var journal = Panel("Tareas_JuntoAlMapa", host.transform); var rect = (RectTransform)journal.transform; rect.anchorMin = new Vector2(0.71f,0.15f); rect.anchorMax = new Vector2(0.98f,0.88f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var journalText = UIText("Lista",journal.transform,24); Stretch(journalText.rectTransform,24); journalText.alignment = TextAlignmentOptions.TopLeft; Set(controller,"journalText",journalText); Set(controller,"journalPanel",journal); journal.SetActive(false);
            var reading = Panel("Lectura_Opcional", host.transform); rect=(RectTransform)reading.transform; rect.anchorMin = new Vector2(0.18f,0.15f); rect.anchorMax = new Vector2(0.82f,0.85f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            var readingText = UIText("Texto",reading.transform,29); Stretch(readingText.rectTransform,38); readingText.alignment = TextAlignmentOptions.TopLeft; Set(controller,"readingText",readingText); Set(controller,"readingPanel",reading); reading.SetActive(false);
        }
        private static void OptionalReadings(Transform root, GameplayPathController controller, Vector3 kitchen)
        {
            string[] texts = {
                "Carta sin enviar — ficción provisional\n\nEn casa: hoy me senté a escribir y no sabía por dónde empezar. Extraño las conversaciones de la mesa, incluso cuando todos hablaban al mismo tiempo. Si esta carta llega antes que las otras, no se preocupen. Voy a seguir escribiendo.\n\nContexto documentado: los archivos de la Fuerza Aérea Argentina conservan correspondencia y diarios de guerra entre sus documentos sobre Malvinas. Esta carta es inventada; no reproduce un testimonio real.\n\nFuente: Ministerio de Defensa, Colección temática Malvinas – Fuerza Aérea Argentina. Referencia completa en Docs/GameplayPath-Level1.md.",
                "Un paquete de casa — ficción provisional\n\nRubén: guardé un lugar en el cajón para todas las cartas que nos mandes. Te escribo mientras los demás duermen. Te mando un abrazo de todos. No hace falta que encuentres las palabras perfectas: contanos cualquier cosa de tu día.\n\nEste texto es ficción y podrá reemplazarse por el material del equipo. No describe una entrega real ni garantiza que un paquete haya llegado a las islas." };
            for(int i=0;i<texts.Length;i++)
            {
                var table=Prop("Lectura_Apoyo_"+i,"Assets/Prefabs/World/Crates/SmallBox.prefab",Ground(kitchen+new Vector3(4+i*2,0,-4)),0.8f,root);
                var b=Bounds(table); var paper=Primitive("Carta_Opcional_"+i,PrimitiveType.Cube,root,new Vector3(b.center.x,b.max.y+0.025f,b.center.z),new Vector3(0.5f,0.035f,0.35f),Mat("Paper",Color.white));
                var interactable=Interactable(paper,controller,PathAction.Read,"Leer carta (opcional)",false);Set(interactable,"readingText",texts[i]);
            }
        }
        private static GameplayPathDefinition CreateDefinition()
        {
            var d=ScriptableObject.CreateInstance<GameplayPathDefinition>(); d.name="SO_GameplayPath_Level1";
            d.introduction=Conversation("01_Introduccion",new[]{("Rubén","¿Recién llegás? Soy Rubén. Antes de moverte por el campamento, revisá el equipo."),("Martín","Martín. Decime qué necesito."),("Rubén","Casco, FAL, munición, mapa y radio. Están en la mesa de suministros, al lado del cartel EQUIPO. Volvé cuando tengas todo."),("Martín","¿Y después?"),("Rubén","Primero eso. El mapa te va a servir para ubicarte. No hace falta que aprendas todo de golpe.")});
            d.equipmentReady=Conversation("02_EquipoCompleto",new[]{("Martín","Ya tengo todo."),("Rubén","Bien. La cocina está pasando las carpas, junto a la olla. Acercale el jarro a Gómez."),("Martín","¿Venís?"),("Rubén","Sí. Nos vemos ahí. Un rato con algo caliente nos va a venir bien.")});
            d.kitchen=Conversation("03_Cocina",new[]{("Gómez","Acercá el jarro. Todavía queda algo caliente."),("Martín","Gracias. Se me estaban helando las manos."),("Gómez","Quedate un momento. Después te van a hacer caminar otra vez."),("Martín","¿Siempre hay tanto viento?"),("Gómez","Hoy por lo menos podemos escucharnos. Rubén está por ahí; parecía preocupado por algo.")});
            d.letterRequest=Conversation("04_PedidoCarta",new[]{("Rubén","¿Me das una mano? Se me rompió la carta de mi vieja y el viento se llevó los pedazos."),("Martín","¿Por acá cerca?"),("Rubén","Sí. Vi uno sobre una caja, al costado de la cocina. Deben haber quedado cuatro por este sector, entre las cajas y la lona."),("Martín","Voy a mirar."),("Rubén","Gracias. No hace falta que la leas; con recuperarla me alcanza. Es lo único que tengo escrito de ella acá.")});
            d.letterReturn=Conversation("05_DevolverCarta",new[]{("Martín","Encontré los cuatro pedazos. Acá tenés."),("Rubén","Gracias, Martín. Pensé que la había perdido."),("Martín","Guardala bien esta vez."),("Rubén","Sí. Escuchá: te buscaba el sargento Acosta. Está en la carpa principal, junto al cartel."),("Rubén","A mí también me mandaron al puesto avanzado. Nos vemos allá.")});
            d.sergeant=Conversation("06_OrdenSargento",new[]{("Sargento Acosta","Martín, necesito que lleves esta caja al puesto avanzado. Tiene herramientas y unas mantas."),("Martín","¿A quién se la entrego?"),("Sargento Acosta","Al oficial del puesto. Necesitan reparar la cubierta antes de que empeore el tiempo."),("Martín","¿Por dónde voy?"),("Sargento Acosta","Seguí el paso que sale del campamento y los carteles al puesto. Está pasando la subida. Consultá el mapa si perdés la referencia."),("Sargento Acosta","Cuando llegues, dejala con el oficial antes de seguir hacia el frente.")});
            d.delivery=Conversation("07_EntregaCaja",new[]{("Martín","Traigo herramientas y mantas del sargento Acosta."),("Oficial del puesto","Dejala acá. Las herramientas las estaban esperando. Gracias."),("Rubén","Llegaste, Martín. Seguí por el paso hacia el frente. Quedate cerca de las coberturas."),("Oficial del puesto","Hay cargadores de reserva junto a la posición. Si empieza el ataque, mantené la distancia y no te quedes encerrado.")});
            d.battle=Conversation("08_Ataque",new[]{("Rubén","¡A cubierto! ¡Se mueven detrás de las cajas!")});
            d.motherLetter="Rubén querido:\nTe escribo desde la mesa de la cocina. Dejé tu taza donde estaba, aunque todos me dicen que la guarde. Tu hermana quiso agregar un dibujo y terminó ocupando medio sobre.\n\nAcá preguntan por vos. Cuando no sabemos qué decir, contamos alguna de tus historias y por un rato parece que estás sentado con nosotros. Si podés, mandanos unas líneas. No tienen que ser largas.\n\nOjalá estas palabras te acompañen un rato. Te abrazo fuerte.\nMamá.\n\nTexto ficticio provisional; no es una carta histórica real.";
            AssetDatabase.CreateAsset(d,AssetRoot+"/Data/SO_GameplayPath_Level1.asset");return d;
        }
        private static ConversationSO Conversation(string name,(string speaker,string text)[] lines)
        {
            var c=ScriptableObject.CreateInstance<ConversationSO>();c.name=name;AssetDatabase.CreateAsset(c,AssetRoot+"/Data/"+name+".asset");
            var array=new DialogueSO[lines.Length];
            for(int i=0;i<lines.Length;i++)
            {
                var line=ScriptableObject.CreateInstance<DialogueSO>();line.name=name+"_"+(i+1);AssetDatabase.CreateAsset(line,AssetRoot+"/Data/"+line.name+".asset");
                Set(line,"speakerDisplayName",lines[i].speaker);Set(line,"_dialogueText",lines[i].text);Set(line,"_textSpeed",4);Set(line,"_extraReadTime",Mathf.Clamp(lines[i].text.Length*0.035f,2.5f,7));array[i]=line;
            }
            SetArray(c,"Dialogues",array);return c;
        }
        private static InputActionAsset CreateInput()
        {
            var input=ScriptableObject.CreateInstance<InputActionAsset>();var map=input.AddActionMap("GameplayPath");map.AddAction("CloseReading",InputActionType.Button,"<Keyboard>/e");map.AddAction("ReadMother",InputActionType.Button,"<Keyboard>/l");
            string path=AssetRoot+"/Data/GameplayPath.inputactions";File.WriteAllText(path,input.ToJson());UnityEngine.Object.DestroyImmediate(input);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        private static InputActionReference Ref(InputActionAsset input,string name){var r=InputActionReference.Create(input.FindAction("GameplayPath/"+name,true));AssetDatabase.CreateAsset(r,AssetRoot+"/Data/Input_"+name+".asset");return r;}
        private static GameObject Actor(string name,Vector3 point,Vector3 face,Transform parent,GameplayPathController controller,PathAction action,string label)
        {
            var wrapper=Create(name,parent);wrapper.transform.position=point;
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/NPC.prefab"),wrapper.transform);Undo.RegisterCreatedObjectUndo(model,"Create mission character");
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            foreach(var component in model.GetComponentsInChildren<MonoBehaviour>(true)) if(component is SpeakerScript || component is InteractableScript) component.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.tag="Untagged";
            var box=Undo.AddComponent<CapsuleCollider>(wrapper);box.center=Vector3.up*0.9f;box.height=1.8f;box.radius=0.4f;
            Vector3 direction=Vector3.ProjectOnPlane(face-point,Vector3.up);if(direction.sqrMagnitude>0.01f)wrapper.transform.rotation=Quaternion.LookRotation(direction);
            Interactable(wrapper,controller,action,label,false);return wrapper;
        }
        private static GameplayPathInteractable Interactable(GameObject obj,GameplayPathController controller,PathAction action,string label,bool hide)
        {var c=Undo.AddComponent<GameplayPathInteractable>(obj);SetupInteraction(c,label);Set(c,"controller",controller);Set(c,"action",(int)action);Set(c,"hideAfterUse",hide);SetLayer(obj,6);return c;}
        private static void SetupInteraction(InteractableScript c,string label)
        {c.interactableName=label;c.UIInteraction=new UIInteraction{label=label,interactionType=EInteractions.Interact};foreach(string f in new[]{"stateDescription","focusInteraction","inspectInteraction","subInteractions"})Set(c,f,false);}
        private static GameObject Prop(string name,string path,Vector3 ground,float width,Transform parent)
        {
            var wrapper=Create(name,parent);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing prop: "+path);
            var model=PrefabUtility.InstantiatePrefab(prefab,wrapper.transform) as GameObject;Undo.RegisterCreatedObjectUndo(model,"Place "+name);
            foreach(var light in model.GetComponentsInChildren<Light>(true))light.enabled=false;foreach(var camera in model.GetComponentsInChildren<Camera>(true))camera.enabled=false;
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))if(behaviour!=null)behaviour.enabled=false;
            foreach(var rb in model.GetComponentsInChildren<Rigidbody>(true)){rb.isKinematic=true;rb.useGravity=false;}
            var b=Bounds(model);float extent=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(extent>0.001f)model.transform.localScale*=width/extent;
            b=Bounds(model);model.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);wrapper.transform.position=ground;model.transform.position-=ground;
            // The wrapper owns one predictable collision volume; decorative prefab colliders are retired locally.
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            b=Bounds(model);var box=Undo.AddComponent<BoxCollider>(wrapper);box.center=wrapper.transform.InverseTransformPoint(b.center);box.size=b.size;
            return wrapper;
        }
        private static Mesh HelmetMesh()
        {
            const int sides=20,rings=6;var verts=new Vector3[(rings+1)*sides];var tris=new int[rings*sides*6];
            for(int r=0;r<=rings;r++)for(int s=0;s<sides;s++){float a=s*Mathf.PI*2/sides,t=r*Mathf.PI*0.5f/rings;verts[r*sides+s]=new Vector3(Mathf.Cos(a)*Mathf.Sin(t)*0.2f,Mathf.Cos(t)*0.22f,Mathf.Sin(a)*Mathf.Sin(t)*0.24f);}
            int k=0;for(int r=0;r<rings;r++)for(int s=0;s<sides;s++){int a=r*sides+s,b=r*sides+(s+1)%sides,c=(r+1)*sides+s,d=(r+1)*sides+(s+1)%sides;tris[k++]=a;tris[k++]=b;tris[k++]=c;tris[k++]=b;tris[k++]=d;tris[k++]=c;}
            var mesh=new Mesh{name="Helmet_Provisional"};mesh.vertices=verts;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,AssetRoot+"/Data/Helmet_Provisional.asset");return mesh;
        }
        private static void Sign(string text,Vector3 ground,float yaw,Transform parent)
        {
            var root=Create("Cartel_"+text,parent);root.transform.position=ground;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            var post=Primitive("Poste",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(0.1f,1.8f,0.1f),Mat("Wood",new Color(0.22f,0.16f,0.10f)));post.transform.localPosition=Vector3.up*0.9f;
            var board=Primitive("Tabla",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(2.5f,0.5f,0.1f),Mat("Wood",Color.gray));board.transform.localPosition=Vector3.up*1.6f;
            var label=WorldText("Rotulo",text,root.transform,0.22f);label.transform.localPosition=new Vector3(0,1.6f,-0.065f);label.rectTransform.sizeDelta=new Vector2(2.4f,0.45f);
        }
        private static TextMeshPro WorldText(string name,string value,Transform parent,float size){var obj=Create(name,parent);var t=Undo.AddComponent<TextMeshPro>(obj);t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(0.92f,0.88f,0.75f);t.rectTransform.sizeDelta=new Vector2(0.4f,0.25f);return t;}
        private static GameObject Panel(string name,Transform parent){var p=Create(name,parent,typeof(RectTransform));var image=Undo.AddComponent<Image>(p);image.color=new Color(0.04f,0.05f,0.05f,0.95f);image.raycastTarget=false;return p;}
        private static TextMeshProUGUI UIText(string name,Transform parent,float size){var obj=Create(name,parent,typeof(RectTransform));var text=Undo.AddComponent<TextMeshProUGUI>(obj);text.font=TMP_Settings.defaultFontAsset;text.fontSize=size;text.color=new Color(0.94f,0.91f,0.82f);text.raycastTarget=false;return text;}
        private static void Stretch(RectTransform r,float margin=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*margin;r.offsetMax=-Vector2.one*margin;}
        private static GameObject Create(string name,Transform parent,params Type[] types){var g=new GameObject(name,types);Undo.RegisterCreatedObjectUndo(g,"Create "+name);if(parent!=null)g.transform.SetParent(parent,false);return g;}
        private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 point,Vector3 scale,Material material){var obj=GameObject.CreatePrimitive(type);Undo.RegisterCreatedObjectUndo(obj,"Create "+name);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.position=point;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;}
        private static Material Mat(string name,Color color){string path=AssetRoot+"/Data/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(mat,path);return mat;}
        private static Bounds Bounds(GameObject obj){var rs=obj.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(obj.transform.position,Vector3.one);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static Vector3 Ground(Vector3 p){var terrain=Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.z>=t.transform.position.z&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z<=t.transform.position.z+t.terrainData.size.z);if(terrain==null)throw new InvalidOperationException("Outside terrain: "+p);return new Vector3(p.x,terrain.SampleHeight(p)+terrain.transform.position.y+0.03f,p.z);}
        private static void SetLayer(GameObject obj,int layer){foreach(var t in obj.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=layer;if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}}
        private static void SetArray(UnityEngine.Object target,string field,UnityEngine.Object[] values){var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(target.GetType().Name+"."+field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();}
        private static void Set(UnityEngine.Object target,string field,object value){Undo.RecordObject(target,"Configure "+field);var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(target.GetType().Name+"."+field);if(value is UnityEngine.Object o)p.objectReferenceValue=o;else if(value is bool b)p.boolValue=b;else if(value is int i)p.intValue=i;else if(value is float f)p.floatValue=f;else if(value is string s)p.stringValue=s;so.ApplyModifiedProperties();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
        private static T[] InScene<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        internal static void Capture()
        {
            var host=new GameObject("GameplayPath_CaptureCamera");var cam=host.AddComponent<Camera>();var rt=new RenderTexture(1600,1000,24);var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;bool fog=RenderSettings.fog;
            try
            {
                RenderSettings.fog=false;host.transform.position=new Vector3(89,340,390);host.transform.rotation=Quaternion.Euler(90,0,0);cam.orthographic=true;cam.orthographicSize=155;cam.nearClipPlane=0.3f;cam.farClipPlane=800;cam.cullingMask=~(1<<5|1<<9|1<<10);rt.Create();
                RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes("Library/GameplayPath-overview.png",tex.EncodeToPNG());
            }
            finally{RenderSettings.fog=fog;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(host);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
    }
}
#endif
