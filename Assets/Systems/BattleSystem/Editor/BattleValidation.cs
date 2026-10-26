using System;
using System.Collections.Generic;
using System.Reflection;
using Systems.EventSystem.Scripts;
using Systems.WeaponSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Systems.BattleSystem.Editor {
    public static class BattleValidation {
        private const string Pending = "StoryTell.BattleValidation.Pending";
        private static GameObject root;
        private static List<GameObject> spawned;
        private static double deadline;

        [MenuItem("Tools/Battle/Validate in Play Mode")]
        private static void RunInPlayMode() {
            if (root) return;
            OnPlayModeChanged(PlayModeStateChange.EnteredPlayMode);
        }

        [MenuItem("Tools/Battle/Validate in Play Mode", true)]
        private static bool CanRunInPlayMode() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        public static void Run() {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this validation with Unity -batchmode.");
            SessionState.SetBool(Pending, true);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume() {
            if (SessionState.GetBool(Pending, false)) EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state) {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SessionState.SetBool(Pending, false);
            try {
                root = new GameObject("Battle validation");
                root.SetActive(false);
                var playerObject = Child("Player");
                playerObject.tag = "Player";
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                var recoil = playerObject.GetComponent<WeaponRecoil>();
                var colliderObject = new GameObject("Player collider");
                colliderObject.transform.SetParent(playerObject.transform);
                var playerCollider = colliderObject.AddComponent<BoxCollider>();
                var stranger = Child("Not a player").AddComponent<BoxCollider>();
                var area = Child("Area").AddComponent<SpawnArea>();
                Set(area, "sampleNavMesh", false);
                var spawner = Child("Spawner").AddComponent<ShadowSpawner>();
                Set(spawner, "spawnArea", area);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/BattleSystem/Materials/BattleShadow.mat");
                Require(material && material.shader.name == "Universal Render Pipeline/Unlit" && material.GetColor("_BaseColor") == Color.black, "Black Unlit material");
                Set(spawner, "blackUnlitMaterial", material);
                Set(spawner, "lifetimeRange", new Vector2(.15f, .15f));
                var manager = Child("Manager").AddComponent<BattleManager>();
                Set(manager, "player", weapon); Set(manager, "shadowSpawner", spawner); Set(manager, "spawnCount", 3);
                var zoneObject = Child("Zone");
                zoneObject.AddComponent<BoxCollider>();
                var zone = zoneObject.AddComponent<TriggerZone>();
                root.SetActive(true);
                Require(!weapon.CanShoot && !weapon.TryShoot(), "Cannot shoot before equipping");
                Require(!weapon.EquipWeapon("Unknown") && !weapon.CanShoot, "Unknown weapon rejected");
                manager.enabled = false;
                GameEventBus.Raise(GameplayEvents.OnBattleTriggered);
                Require(!weapon.CanShoot, "Disabled manager unsubscribes");
                manager.enabled = true;
                Call(zone, "OnTriggerEnter", stranger);
                Require(!weapon.CanShoot, "Non-player ignored");
                Call(zone, "OnTriggerEnter", playerCollider);
                Require(weapon.CanShoot && weapon.EquippedWeapon == "FAL", "Player child collider triggers equip");
                Require(zone.GetComponent<Collider>().isTrigger, "Trigger collider configured");
                spawned = (List<GameObject>)Get(spawner, "shadows");
                Require(spawned.Count == 3, "Requested spawn count");
                foreach (var shadow in spawned) {
                    var renderer = shadow.GetComponent<MeshRenderer>();
                    Require(renderer.sharedMaterial == material && renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows, "Shared material and shadows disabled");
                }
                Call(zone, "OnTriggerEnter", playerCollider);
                GameEventBus.Raise(GameplayEvents.OnBattleTriggered);
                Require(spawned.Count == 3, "Duplicate battle guarded");
                int shots = 0;
                weapon.ShotFired += () => shots++;
                Require(weapon.TryShoot() && shots == 1 && recoil.PitchOffset < 0, "Shot applies upward recoil");
                Require(!weapon.TryShoot() && shots == 1, "Fire interval enforced");
                weapon.HolsterWeapon();
                Require(!weapon.CanShoot && recoil.PitchOffset == 0, "Holstering clears recoil");
                deadline = EditorApplication.timeSinceStartup + 1;
                EditorApplication.update += CheckLifetime;
            } catch (Exception exception) { Finish(exception); }
        }

        private static void CheckLifetime() {
            if (EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= CheckLifetime;
            try {
                foreach (var shadow in spawned) Require(!shadow, "Capsule lifetime expires");
                Finish(null);
            } catch (Exception exception) { Finish(exception); }
        }

        private static void Finish(Exception exception) {
            if (root) Object.Destroy(root);
            if (exception != null) Debug.LogException(exception);
            else Debug.Log("[BattleValidation] PASS: event lifecycle, player filtering, FAL equip, spawn count, shared Unlit material, disabled shadows, duplicate prevention, shot cooldown, recoil cleanup, capsule lifetime.");
            if (Application.isBatchMode) EditorApplication.Exit(exception == null ? 0 : 1);
        }

        private static GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform); return go; }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Call(object target, string name, object argument) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, new[] { argument });
    }
}
