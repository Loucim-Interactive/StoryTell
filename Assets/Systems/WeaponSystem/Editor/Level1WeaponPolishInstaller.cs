#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using InteractionSystem.Scripts.Utils;
using StoryTell.Weapons;
using Systems.InteractionSystem.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StoryTell.Weapons.Editor
{
    public static class Level1WeaponPolishInstaller
    {
        private const string Root = "Assets/Systems/WeaponSystem";
        [MenuItem("StoryTell/Weapons/Polish Level1 Presentation")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before polishing the scene.");
            const string path = "Assets/Scenes/Level1.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty)) throw new InvalidOperationException("Save open scenes first.");
                scene = EditorSceneManager.OpenScene(path);
            }
            string backup = "Library/WeaponPolishBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup); File.Copy(path, backup + "/Level1.unity");
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene, backup + "/Level1-unsaved.unity", true)) throw new IOException("Cannot back up unsaved edits.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Polish Level1 weapon presentation");
            var trial = InScene<Level1WeaponTrial>(scene).Single();
            var weapon = trial.Weapon;
            var serialized = new SerializedObject(trial);
            var status = (TMP_Text)serialized.FindProperty("status").objectReferenceValue;
            var completion = (TMP_Text)serialized.FindProperty("completion").objectReferenceValue;
            var darkness = (Image)serialized.FindProperty("darkness").objectReferenceValue;
            var interaction = (InteractionScript)serialized.FindProperty("interaction").objectReferenceValue;
            Transform canvasRoot = status.GetComponentInParent<Canvas>().transform;
            var hud = Child("CombatHUD", canvasRoot, typeof(RectTransform)); Stretch((RectTransform)hud.transform);
            var hudGroup = Ensure<CanvasGroup>(hud); hudGroup.interactable = false; hudGroup.blocksRaycasts = false;
            var panel = Child("AmmunitionPanel", hud.transform, typeof(RectTransform));
            var panelImage = Ensure<Image>(panel); panelImage.color = new Color(0.025f, 0.035f, 0.04f, 0.7f); panelImage.raycastTarget = false;
            RectTransform rect = (RectTransform)panel.transform; Undo.RecordObject(rect, "Place ammo HUD");
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-36, 34); rect.sizeDelta = new Vector2(240, 105);
            Undo.SetTransformParent(status.transform, panel.transform, "Place ammo text");
            Undo.RecordObject(status, "Style ammunition display"); Stretch(status.rectTransform);
            status.fontSize = 27; status.alignment = TextAlignmentOptions.BottomRight; status.margin = new Vector4(18, 10, 20, 12); status.raycastTarget = false;
            var hintObject = Child("ContextHint", hud.transform, typeof(RectTransform));
            var hint = Ensure<TextMeshProUGUI>(hintObject); hint.font = status.font; hint.fontSize = 27; hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.94f, 0.94f, 0.90f); hint.raycastTarget = false;
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0);
            hint.rectTransform.pivot = new Vector2(0.5f, 0); hint.rectTransform.anchoredPosition = new Vector2(0, 45); hint.rectTransform.sizeDelta = new Vector2(1180, 75);
            var vignetteObject = Child("PeripheralStress", canvasRoot, typeof(RectTransform));
            Ensure<CanvasRenderer>(vignetteObject);
            var vignette = Ensure<WeaponVignetteGraphic>(vignetteObject); vignette.raycastTarget = false; Stretch(vignette.rectTransform);
            vignetteObject.transform.SetAsFirstSibling();
            darkness.transform.SetSiblingIndex(1);
            hud.transform.SetSiblingIndex(2);
            completion.transform.SetAsLastSibling();
            var titleGroup = Ensure<CanvasGroup>(completion.gameObject); titleGroup.alpha = 0; titleGroup.blocksRaycasts = false;
            Undo.RecordObject(completion, "Style scene ending title"); completion.characterSpacing = 3; completion.fontSize = 48;
            Set(trial, "vignette", vignette); Set(trial, "hint", hint); Set(trial, "combatHUD", hudGroup); Set(trial, "completionGroup", titleGroup); Set(trial, "ammoPanel", panel);

            InputActionReference interact = EnsureInteractAction();
            Set(interaction, "interactAction", interact);
            foreach (var pickup in InScene<WeaponTrialPickup>(scene))
            {
                Undo.RecordObject(pickup, "Bind E pickup without jumping"); pickup.UIInteraction.interactionType = EInteractions.Interact;
                EditorUtility.SetDirty(pickup);
            }
            var recoil = weapon.GetComponent<WeaponRecoil>();
            Set(recoil, "trial", trial); Set(recoil, "weaponAudio", weapon.GetComponent<AudioSource>());

            var controllerSerialized = new SerializedObject(weapon);
            AudioClip mechanical = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/SoundSystem/Assets/Radio/MechanicalClick.mp3");
            foreach (string field in new[] { "emptyClip", "reloadClip", "jamClip", "clearJamClip" })
            {
                if (controllerSerialized.FindProperty(field).objectReferenceValue == null) Set(weapon, field, mechanical);
            }
            var muzzle = controllerSerialized.FindProperty("muzzleFlash").objectReferenceValue as ParticleSystem;
            if (muzzle != null)
            {
                Undo.RecordObject(muzzle.transform, "Reduce muzzle flash size"); muzzle.transform.localScale = Vector3.one * 0.10f;
                foreach (var ps in muzzle.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Undo.RecordObject(ps, "Keep muzzle flash brief"); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main; main.duration = 0.08f; main.startLifetime = 0.055f; main.playOnAwake = false; main.loop = false;
                    var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
                    if (PrefabUtility.IsPartOfPrefabInstance(ps)) PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
                }
                if (PrefabUtility.IsPartOfPrefabInstance(muzzle.transform)) PrefabUtility.RecordPrefabInstancePropertyModifications(muzzle.transform);
            }
            var feedback = Ensure<WeaponImpactFeedback>(weapon.gameObject); Set(feedback, "weapon", weapon);
            var pools = new ParticleSystem[4]; Material dust = ParticleMaterial();
            for (int i = 0; i < pools.Length; i++)
            {
                var host = Child("ImpactDust_" + i, trial.transform);
                var ps = Ensure<ParticleSystem>(host); pools[i] = ps; ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.playOnAwake = false; main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 128; main.gravityModifier = 0.5f;
                var emission = ps.emission; emission.enabled = false;
                var shape = ps.shape; shape.enabled = false;
                var color = ps.colorOverLifetime; color.enabled = true;
                var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = gradient;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = dust;
            }
            SetArray(feedback, "impactPool", pools);
            var breathingHost = Child("StressBreathing", weapon.transform);
            var breathing = Ensure<AudioSource>(breathingHost); breathing.playOnAwake = false; breathing.loop = false; breathing.spatialBlend = 0;
            breathing.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/InmersionSystem/Ambient/Assets/ColdbreathSounds/exhale_1.mp3"); breathing.volume = 0;
            var audio = Ensure<WeaponStressAudio>(weapon.gameObject);
            var filter = Ensure<AudioLowPassFilter>(weapon.gameObject); filter.cutoffFrequency = 22000;
            Set(audio, "trial", trial); Set(audio, "breathing", breathing); Set(audio, "weaponFilter", filter);
            SetArray(audio, "endingSources", InScene<AudioSource>(scene).Where(s => s != breathing).ToArray());
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save polished Level1.");
            File.WriteAllText("Library/WeaponPolish-install.txt", "Installed in Level1. Backup: " + backup);
            Debug.Log("Weapon polish installed. Backup: " + backup);
        }
        private static Material ParticleMaterial()
        {
            string texturePath = Root + "/Data/PolishDust.png";
            if (!File.Exists(texturePath))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color[32 * 32];
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float r = new Vector2((x + 0.5f) / 16 - 1, (y + 0.5f) / 16 - 1).magnitude;
                    pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 2));
                }
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(texturePath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            string path = Root + "/Data/PolishDust.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000; material.SetOverrideTag("RenderType", "Transparent");
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
        private static InputActionReference EnsureInteractAction()
        {
            string path = Root + "/Data/Level1WeaponInput.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset.FindAction("Weapons/Interact") == null)
            {
                asset.FindActionMap("Weapons", true).AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
                File.WriteAllText(path, asset.ToJson()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }
            string referencePath = Root + "/Data/Level1_Interact.asset";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
            if (reference == null) { reference = InputActionReference.Create(asset.FindAction("Weapons/Interact", true)); AssetDatabase.CreateAsset(reference, referencePath); }
            return reference;
        }
        private static T[] InScene<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static T Ensure<T>(GameObject g) where T : Component => g.TryGetComponent<T>(out var c) ? c : Undo.AddComponent<T>(g);
        private static GameObject Child(string name, Transform parent, params Type[] components)
        {
            var child = parent.Find(name); if (child != null) return child.gameObject;
            var g = new GameObject(name, components); Undo.RegisterCreatedObjectUndo(g, "Create " + name); g.transform.SetParent(parent, false); return g;
        }
        private static void Stretch(RectTransform r) { Undo.RecordObject(r, "Fit HUD"); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            Undo.RecordObject(target, "Configure weapon polish"); var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedProperties();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            Undo.RecordObject(target, "Configure weapon polish references"); var s = new SerializedObject(target); var p = s.FindProperty(field); p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; s.ApplyModifiedProperties();
        }
    }
}
#endif
