#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.Weapons;
using Systems.InteractionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class WeaponPolishTests
{
    [UnityTest]
    public IEnumerator Vignette_RendersDarkEdgesAndReadableCenter()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        StoryTell.GameplayPath.Editor.GameplayPathEditorLegacyFixture.RestoreTrialForLegacyTests();
        var cameraHost = new GameObject("VignetteVerificationCamera");
        var camera = cameraHost.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
        camera.cullingMask = 1 << 5;
        var canvasHost = new GameObject("VignetteVerificationCanvas", typeof(RectTransform)); canvasHost.layer = 5;
        var canvas = canvasHost.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var graphicHost = new GameObject("Vignette", typeof(RectTransform)); graphicHost.layer = 5; graphicHost.transform.SetParent(canvas.transform, false);
        var graphic = graphicHost.AddComponent<WeaponVignetteGraphic>(); graphic.raycastTarget = false; graphic.Intensity = 0.85f;
        var rect = graphic.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases(); yield return null;
        var target = new RenderTexture(256, 256, 24); var pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            target.Create(); camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes("Library/WeaponPolish-vignette.png", pixels.EncodeToPNG());
            Assert.That(pixels.GetPixel(128, 128).r, Is.GreaterThan(0.7f));
            var mesh = graphic.canvasRenderer.GetMesh();
            string diagnostic = $"Peripheral stress must be visibly darker than the center. Mesh={(mesh != null ? mesh.vertexCount : 0)}, rect={rect.rect}, culled={graphic.canvasRenderer.cull}, materials={graphic.canvasRenderer.materialCount}";
            Assert.That(pixels.GetPixel(12, 128).r + 0.2f, Is.LessThan(pixels.GetPixel(128, 128).r), diagnostic);
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        yield return new ExitPlayMode();
    }
    [Test]
    public void SavedLevel1_HasScaledCompactHUD_Vignette_EAction_AndImpactPool()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        var trial = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Level1WeaponTrial>(true)).Single();
        Assert.That(trial.GetComponentInChildren<WeaponVignetteGraphic>(true), Is.Not.Null);
        var canvas = trial.GetComponentInChildren<CanvasScaler>(true);
        Assert.That(canvas.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
        var interaction = Object.FindAnyObjectByType<InteractionScript>();
        var action = (InputActionReference)new SerializedObject(interaction).FindProperty("interactAction").objectReferenceValue;
        Assert.That(action.action.bindings.Any(b => b.path == "<Keyboard>/e"), Is.True);
        var feedback = trial.Weapon.GetComponent<WeaponImpactFeedback>();
        Assert.That(new SerializedObject(feedback).FindProperty("impactPool").arraySize, Is.EqualTo(4));
        var recoil = new SerializedObject(trial.Weapon.GetComponent<WeaponRecoil>());
        Assert.That(recoil.FindProperty("shakeIntensity").floatValue, Is.LessThan(1));
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            Assert.That(t.GetComponents<Component>().All(c => c != null), Is.True, "Missing component on " + t.name);
    }
    [UnityTest]
    public IEnumerator SlowShots_StillFinish_WithDelayedBlackAndTitle()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        yield return new EnterPlayMode();
        StoryTell.GameplayPath.Editor.GameplayPathEditorLegacyFixture.RestoreTrialForLegacyTests();
        float readyAt = Time.time + 0.4f;
        while (Time.time < readyAt) yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        var trial = Object.FindAnyObjectByType<Level1WeaponTrial>();
        if (Application.isBatchMode)
        {
            var serialized = new SerializedObject(trial); serialized.FindProperty("requireLockedPointer").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        Assert.That(trial.CollectRifle(), Is.True);
        var weapon = trial.Weapon;
        weapon.Condition.ConfigureExactIntervalsForTests(100, 100, 100, 100);
        var movement = Object.FindAnyObjectByType<PlayerControllerScript>();
        for (int shot = 0; shot < trial.FinalizationShots; shot++)
        {
            Assert.That(weapon.TryFire(), Is.EqualTo(WeaponFireResult.Fired), $"Shot {shot}: allowed={trial.CanUseWeapon}, state={weapon.State}, input={weapon.InputEnabled}, zone={trial.IsInsideZone}, cursor={Cursor.lockState}");
            if (shot + 1 < trial.FinalizationShots)
            {
                float nextShotAt = Time.time + 1.1f;
                while (Time.time < nextShotAt) yield return null;
                Assert.That(trial.Darkness, Is.LessThan(0.25f), "Slow shots should let visual stress recover.");
                Assert.That(trial.Completed, Is.False);
            }
        }
        Assert.That(trial.IsFinishing, Is.True, "Persistent shot count must finish even though spray stress recovered.");
        Assert.That(trial.Completed, Is.False);
        Assert.That(trial.CentralDarkness, Is.LessThan(0.1f), "Last shot must not cut directly to black.");
        Assert.That(movement.enabled, Is.True, "Movement is only locked after reaching black.");
        float deadline = Time.realtimeSinceStartup + 8;
        while (!trial.Completed && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(trial.Completed, Is.True);
        Assert.That(trial.CentralDarkness, Is.EqualTo(1));
        Assert.That(movement.enabled, Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator PartiallyCollectedAmmo_RemainsAvailable()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        yield return new EnterPlayMode();
        StoryTell.GameplayPath.Editor.GameplayPathEditorLegacyFixture.RestoreTrialForLegacyTests();
        yield return null;
        var trial = Object.FindAnyObjectByType<Level1WeaponTrial>();
        Assert.That(trial.CollectRifle(), Is.True);
        var inventory = trial.Weapon.MagazineInventory;
        inventory.AddFullMagazines(3);
        var pickup = Object.FindObjectsByType<WeaponTrialPickup>(FindObjectsSortMode.None).Single(p => p.IsAmmunition);
        Assert.That(pickup.TryCollect(), Is.True);
        Assert.That(inventory.MagazineCount, Is.EqualTo(4));
        Assert.That(pickup.gameObject.activeSelf, Is.True, "Two uncollected magazines must remain in the world.");
        inventory.TryTakeNextMagazine(out _); inventory.TryTakeNextMagazine(out _);
        Assert.That(pickup.TryCollect(), Is.True);
        Assert.That(inventory.MagazineCount, Is.EqualTo(4));
        Assert.That(pickup.gameObject.activeSelf, Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTearDown]
    public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
}
#endif
