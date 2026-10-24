#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.Input;
using NUnit.Framework;
using StoryTell.PhysicalMap;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Level1PhysicalMapIntegrationTests
{
    [SerializeField] private int background = -1, editorInput = -1, keyboardId = -1, mouseId = -1;

    [Test]
    public void Level1_HasMapBoundToPlayerAndToggleAction()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        PhysicalMapController[] maps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PhysicalMapController>(true)).ToArray();
        Assert.That(maps.Length, Is.EqualTo(1));
        var map = maps[0];
        Assert.That(map.IsReady, Is.True);
        Assert.That(map.ToggleMapAction.action.bindings.Any(b => b.path == "<Keyboard>/m"), Is.True);
        Assert.That(map.GetComponent<MapWorldReference>().IsConfigured, Is.True);
        Assert.That(map.GetComponentInChildren<MapMarkerController>().TrackedTarget, Is.Not.Null);
        Assert.That(map.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
        Assert.That(map.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("PhysicalMap")));
        Assert.That(map.GetComponent<PhysicalMapViewCamera>().MapCamera, Is.Not.Null);
        Assert.That(map.GetComponentsInChildren<Collider>().All(c => c.isTrigger), Is.True);
    }

    [UnityTest]
    public IEnumerator M_OpensClosesAndMarkerTracksPlayer()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        yield return new EnterPlayMode();
        yield return null;
        var questPath = Object.FindAnyObjectByType<StoryTell.GameplayPath.GameplayPathController>();
        if (questPath != null) {
            questPath.Progress.Advance(StoryTell.GameplayPath.PathAction.Ruben);
            questPath.Interact(StoryTell.GameplayPath.PathAction.Map);
        }
        background = (int)InputSystem.settings.backgroundBehavior;
        editorInput = (int)InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        keyboardId = keyboard.deviceId;
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        mouseId = mouse.deviceId;
        var playerInput = FirstPersonControllerSystem.Scripts.Input.InputScript.Instance;
        var actions = (PlayerLocomotionInput)typeof(FirstPersonControllerSystem.Scripts.Input.InputScript)
            .GetField("_playerLocomotionInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(playerInput);
        actions.devices = new InputDevice[] { keyboard, mouse };
        yield return new WaitForSeconds(0.5f);
        PhysicalMapController map = Object.FindAnyObjectByType<PhysicalMapController>();
        Assert.That(map.IsOpen, Is.False);
        Assert.That(map.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(map.ToggleMapAction.action.enabled, Is.True);

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M));
        yield return new WaitForSeconds(0.3f);
        Assert.That(map.IsOpen, Is.True, "The real Input System binding must open the map.");
        Assert.That(map.GetComponent<Renderer>().enabled, Is.True);
        Assert.That(map.GetComponent<PhysicalMapViewCamera>().MapCamera.enabled, Is.True);
        Assert.That(map.ViewAnchor, Is.EqualTo(Camera.main.transform));
        var marker = map.GetComponentInChildren<MapMarkerController>();
        var reference = map.GetComponent<MapWorldReference>();
        reference.TryWorldToMapPosition(marker.TrackedTarget.position, out Vector3 expected, out bool inside);
        Assert.That(inside, Is.True);
        Assert.That(Vector3.Distance(marker.transform.position, expected), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(map.transform.position, map.ViewAnchor.TransformPoint(map.Settings.localHoldPosition)), Is.LessThan(0.15f));
        Capture(Camera.main);

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M));
        yield return new WaitForSeconds(0.3f);
        Assert.That(map.IsOpen, Is.False);
        float deadline = Time.realtimeSinceStartup + 2f;
        while (map.GetComponent<Renderer>().enabled && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(map.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(map.GetComponent<Rigidbody>().isKinematic, Is.True);
        Assert.That(marker.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(map.GetComponent<PhysicalMapViewCamera>().MapCamera.enabled, Is.False);
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (keyboardId >= 0)
        {
            InputDevice keyboard = InputSystem.GetDeviceById(keyboardId);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            keyboardId = -1;
        }
        if (mouseId >= 0)
        {
            InputDevice mouse = InputSystem.GetDeviceById(mouseId);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            mouseId = -1;
        }
        if (background >= 0)
        {
            InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)background;
            InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)editorInput;
            background = editorInput = -1;
        }
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    private static void Capture(Camera camera)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        string path = "Library/Level1PhysicalMap-open.png";
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
        }
    }
}
#endif
