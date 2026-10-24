#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.Input;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using StoryTell.Weapons;
using StoryTell.PhysicalMap;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class Level1WeaponIntegrationTests
{
    [SerializeField] private int background = -1, editorInput = -1, keyboardId = -1, mouseId = -1;
    [Test]
    public void Spray_RecoversAfterPauseAndCompletesOnlyFromSustainedFire()
    {
        var stress = new WeaponSprayStress();
        for (int i = 0; i < 15; i++) stress.RecordShot(i * 0.11f);
        Assert.That(stress.Darkness, Is.EqualTo(0.85f).Within(0.001f));
        Assert.That(stress.Completed, Is.False);
        stress.Tick(4, 1); Assert.That(stress.Heat, Is.EqualTo(11));
        stress.Tick(20, 5); Assert.That(stress.Darkness, Is.Zero);
        for (int i = 0; i < 20; i++) stress.RecordShot(21 + i * 0.11f);
        Assert.That(stress.Completed, Is.True); stress.Tick(100, 50);
        Assert.That(stress.Darkness, Is.EqualTo(1));
    }
    [Test]
    public void Level1_HasSeparatePickupsAndRealInputActions()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        var all = scene.GetRootGameObjects();
        Assert.That(all.SelectMany(r => r.GetComponentsInChildren<Level1WeaponTrial>(true)).Count(), Is.EqualTo(1));
        var pickups = all.SelectMany(r => r.GetComponentsInChildren<WeaponTrialPickup>(true)).ToArray();
        Assert.That(pickups.Length, Is.EqualTo(2));
        Assert.That(pickups.All(p => !p.InspectInteractable && !p.FocusInteraction && !p.HasSubInteractions), Is.True);
        Assert.That(pickups.SelectMany(p => p.GetComponentsInChildren<Camera>(true)).Count(), Is.Zero);
        Assert.That(pickups.SelectMany(p => p.GetComponentsInChildren<Light>(true)).Count(), Is.Zero);
        var weaponInput = all.SelectMany(r => r.GetComponentsInChildren<WeaponInput>(true)).Single();
        Assert.That(weaponInput.FireAction.action.bindings.Any(b => b.path == "<Mouse>/leftButton"), Is.True);
        Assert.That(all.SelectMany(r => r.GetComponentsInChildren<WeaponCylinderTarget>(true)).Count(), Is.EqualTo(3));
        foreach (var g in all.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            Assert.That(g.GetComponents<Component>().All(c => c != null), Is.True, "Missing script: " + g.name);
    }
    [UnityTest]
    public IEnumerator Pickup_Ammo_Reload_Jam_Map_Zone_SprayAndEnding()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        yield return new EnterPlayMode();
        StoryTell.GameplayPath.Editor.GameplayPathEditorLegacyFixture.RestoreTrialForLegacyTests();
        background = (int)InputSystem.settings.backgroundBehavior;
        editorInput = (int)InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); keyboardId = keyboard.deviceId;
        Mouse mouse = InputSystem.AddDevice<Mouse>(); mouseId = mouse.deviceId;
        Assert.That(InputScript.Instance, Is.Not.Null, "Level1 player input must exist in Play Mode.");
        var generated = (PlayerLocomotionInput)typeof(InputScript).GetField("_playerLocomotionInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(InputScript.Instance);
        generated.devices = new InputDevice[] { keyboard, mouse };
        yield return WaitGameTime(0.4f);
        Cursor.lockState = CursorLockMode.Locked;
        var trial = Object.FindAnyObjectByType<Level1WeaponTrial>();
        Debug.Log("Weapon trial runtime checkpoint: found trial " + (trial != null));
        Assert.That(trial, Is.Not.Null, "Level1 weapon trial must exist in Play Mode.");
        if (Application.isBatchMode) { var context = new UnityEditor.SerializedObject(trial); context.FindProperty("requireLockedPointer").boolValue = false; context.ApplyModifiedPropertiesWithoutUndo(); }
        var weapon = trial.Weapon;
        Debug.Log("Weapon trial runtime checkpoint: weapon=" + (weapon != null));
        Assert.That(weapon, Is.Not.Null, "Trial must retain its serialized weapon reference.");
        var player = Object.FindAnyObjectByType<PlayerControllerScript>();
        Debug.Log("Weapon trial runtime checkpoint: player=" + (player != null));
        Assert.That(player, Is.Not.Null, "Level1 player must exist in Play Mode.");
        var look = player.GetComponent<CameraControllerScript>();
        Debug.Log("Weapon trial runtime checkpoint: look=" + (look != null));
        var camera = Camera.main;
        Debug.Log("Weapon trial runtime checkpoint: player=" + (player != null) + " camera=" + (camera != null) + " look=" + (look != null));
        var pickups = Object.FindObjectsByType<WeaponTrialPickup>(FindObjectsSortMode.None);
        var ammo = pickups.Single(p => p.IsAmmunition);
        Debug.Log("Weapon trial runtime checkpoint: collecting ammunition");
        Assert.That(ammo.TryCollect(), Is.True);
        Assert.That(weapon.MagazineInventory.MagazineCount, Is.EqualTo(3));
        var rifle = pickups.Single(p => !p.IsAmmunition);
        Debug.Log("Weapon trial runtime checkpoint: looking toward rifle");
        LookAt(player.transform, look, camera, rifle.transform.position);
        yield return WaitGameTime(0.2f);
        Debug.Log("Pickup ray: camera=" + camera.transform.position + " forward=" + camera.transform.forward + " rifle=" + rifle.transform.position + " in zone=" + trial.IsInsideZone);
        foreach (var hit in Physics.RaycastAll(camera.transform.position, camera.transform.forward, 5, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            Debug.Log("Pickup ray hit: " + hit.collider.name + " layer=" + hit.collider.gameObject.layer + " distance=" + hit.distance);
        yield return Capture("Library/Level1Weapons-pickup.png");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
        yield return WaitGameTime(0.2f);
        Assert.That(trial.HasRifle, Is.True, "E interaction must collect the floor rifle.");
        Assert.That(weapon.MagazineInventory.MagazineCount, Is.EqualTo(3), "Picking up rifle must preserve prior ammo.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        look.SetCameraPitch(0);
        yield return WaitGameTime(0.2f);
        foreach (Renderer r in weapon.GetComponentsInChildren<Renderer>(true)) Debug.Log("Weapon child renderer " + r.name);
        foreach (Renderer r in camera.GetComponentsInChildren<Renderer>(true))
            if (r.gameObject.layer == LayerMask.NameToLayer("WeaponView")) Debug.Log("Held FAL renderer " + r.name + " active=" + r.gameObject.activeInHierarchy + " local center=" + camera.transform.InverseTransformPoint(r.bounds.center) + " size=" + r.bounds.size);
        yield return Capture("Library/Level1Weapons-held.png");
        var target = Object.FindObjectsByType<WeaponCylinderTarget>(FindObjectsSortMode.None).OrderBy(t => t.name).First();
        LookAt(player.transform, look, camera, target.transform.position);
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
        yield return WaitGameTime(0.15f);
        Assert.That(weapon.IsAiming, Is.True);
        float pitchBeforeFire = look.CameraPitch;
        Vector3 positionBeforeFire = player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        // Hold the trigger to exercise the configured InputAction, not direct fire calls.
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));
        float deadline = Time.time + 4;
        while (trial.ShotsFired < 3 && Time.time < deadline) yield return null;
        Debug.Log("Burst: shots=" + trial.ShotsFired + " state=" + weapon.State + " allowed=" + trial.CanUseWeapon + " cursor=" + Cursor.lockState);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        Assert.That(trial.ShotsFired, Is.GreaterThanOrEqualTo(3));
        Assert.That(Vector3.Distance(player.transform.position, positionBeforeFire), Is.GreaterThan(0.1f), "Player can walk while firing.");
        Assert.That(target.Health, Is.LessThan(100), "Hitscan must damage cylinder targets.");
        Assert.That(weapon.GetComponent<WeaponImpactFeedback>().ImpactCount, Is.GreaterThan(0), "Successful hits must generate impact feedback.");
        Assert.That(look.CameraPitch, Is.LessThan(pitchBeforeFire), "Each shot must kick the player's view upward.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
        yield return null;
        deadline = Time.time + 6;
        while (weapon.State == WeaponState.Reloading && Time.time < deadline) yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Assert.That(weapon.RoundsInMagazine, Is.EqualTo(20));
        Assert.That(weapon.MagazineInventory.MagazineCount, Is.EqualTo(2));
        var map = Object.FindAnyObjectByType<PhysicalMapController>();
        map.SetOpen(true, true);
        Assert.That(weapon.TryFire(), Is.EqualTo(WeaponFireResult.NotReady));
        map.SetOpen(false, true);
        var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
        Vector3 oldPosition = player.transform.position; player.transform.position += Vector3.right * 80;
        Assert.That(weapon.TryFire(), Is.EqualTo(WeaponFireResult.NotReady));
        player.transform.position = oldPosition; cc.enabled = true;
        yield return WaitGameTime(0.2f);
        weapon.ForceNextJam(); Assert.That(weapon.TryFire(), Is.EqualTo(WeaponFireResult.Jammed));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
        yield return null;
        deadline = Time.time + 6;
        while (weapon.State == WeaponState.ClearingJam && Time.time < deadline) yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Assert.That(weapon.State, Is.EqualTo(WeaponState.Ready));
        // Keep the clean interval beyond this last magazine; jam behavior was verified above.
        weapon.Condition.ConfigureExactIntervalsForTests(100, 100, 100, 100);
        look.SetCameraPitch(0);
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        deadline = Time.time + 6;
        while (trial.Darkness < 0.8f && !trial.Completed && Time.time < deadline) yield return null;
        Assert.That(trial.Darkness, Is.GreaterThan(0.65f));
        yield return Capture("Library/Level1Weapons-spray.png");
        deadline = Time.time + 4;
        while (!trial.Completed && Time.time < deadline) yield return null;
        Assert.That(trial.Completed, Is.True);
        Assert.That(trial.Darkness, Is.EqualTo(1));
        Assert.That(player.enabled, Is.False);
        yield return Capture("Library/Level1Weapons-complete.png");
        yield return new ExitPlayMode();
    }
    private static IEnumerator WaitGameTime(float duration) { float until = Time.time + duration; while (Time.time < until) yield return null; }
    private static void LookAt(Transform player, CameraControllerScript look, Camera camera, Vector3 point)
    {
        Vector3 direction = point - camera.transform.position;
        player.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(direction, Vector3.up));
        look.SetCameraPitch(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (int id in new[] { keyboardId, mouseId }) { var device = InputSystem.GetDeviceById(id); if (device != null) InputSystem.RemoveDevice(device); }
        keyboardId = mouseId = -1;
        if (background >= 0) InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)background;
        if (editorInput >= 0) InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)editorInput;
        background = editorInput = -1;
        if (Application.isPlaying) yield return new ExitPlayMode();
    }
    private static IEnumerator Capture(string path)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
        if (Application.isBatchMode) yield return null; else yield return new WaitForEndOfFrame();
        var rt = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(c => c.name == "WeaponHUD");
        var mode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        var previousTarget = Camera.main.targetTexture;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 0.3f;
            rt.Create();
            Camera.main.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = rt });
            RenderTexture.active = rt; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
            if (path.Contains("-complete"))
                foreach (Vector2 point in new[] { new Vector2(5, 5), new Vector2(1274, 5), new Vector2(5, 714), new Vector2(1274, 714) })
                    Assert.That(pixels.GetPixel((int)point.x, (int)point.y).maxColorComponent, Is.LessThan(0.03f), "The ending must cover every corner of the screen.");
        }
        finally { Camera.main.targetTexture = previousTarget; canvas.renderMode = mode; canvas.worldCamera = previousCamera; RenderTexture.active = previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(pixels); }
    }
}
#endif
