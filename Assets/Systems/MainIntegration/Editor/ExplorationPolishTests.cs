#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.MainIntegration.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class ExplorationPolishTests
{
    [Test] public void ApprovedLightingAndFogAreUnchanged()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        Assert.That(ExplorationPolishAuthoring.CaptureLighting(scene),Is.EqualTo(File.ReadAllText(ExplorationPolishAuthoring.LightingBaseline)));
    }
    [Test] public void ExplorationAllowsCampFlanksAndOutpostWhileDistantEdgesRemainClosed()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var route=Object.FindAnyObjectByType<RouteBoundary>();
        foreach(var point in new[]{new Vector3(30,0,280),new Vector3(130,0,280),new Vector3(20,0,355),new Vector3(130,0,355),new Vector3(95,0,380),new Vector3(100,0,430)})
            Assert.That(route.Contains(point),Is.True,point.ToString());
        Assert.That(route.Contains(new Vector3(500,0,500)),Is.False);Assert.That(route.Contains(new Vector3(-5,0,280)),Is.False);
        var settings=new SerializedObject(route);Assert.That(settings.FindProperty("entrances").arraySize,Is.Zero);
    }
    [Test] public void VisibleWallsBlockFlanksButLeaveMissionAndCombatEntrancesClear()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var route=Object.FindAnyObjectByType<RouteBoundary>();var terrain=Object.FindAnyObjectByType<Terrain>();
        var gate=(GameObject)new SerializedObject(route).FindProperty("frontBarrier").objectReferenceValue;bool active=gate.activeSelf;gate.SetActive(false);Physics.SyncTransforms();
        bool HasWall(float x,float z)
        {
            float ground=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            return Physics.OverlapBox(new Vector3(x,ground+1,z),new Vector3(.2f,.95f,.3f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                .Any(c=>c.GetComponentsInParent<Transform>().Any(t=>t.name=="Access_SandbagWall"));
        }
        try{Assert.That(HasWall(50,398),Is.True);Assert.That(HasWall(90,398),Is.True);Assert.That(HasWall(72,398),Is.False);Assert.That(HasWall(56,519),Is.False);}
        finally{gate.SetActive(active);}
    }
    [UnityTest] public IEnumerator CloudsCompileAndRenderWithExistingCampLighting()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");Assert.That(ShaderUtil.ShaderHasError(Shader.Find("StoryTell/ColdOvercastSky")),Is.False);
        yield return new EnterPlayMode();yield return null;yield return null;
        var camera=Camera.main;var look=Object.FindAnyObjectByType<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>();float pitch=look.CameraPitch;
        yield return Capture(camera,"Library/Exploration-camp.png");look.SetCameraPitch(-35);
        yield return Capture(camera,"Library/Exploration-clouds.png");look.SetCameraPitch(pitch);
        yield return new ExitPlayMode();
    }
    static IEnumerator Capture(Camera camera,string path)
    {
        yield return null;var target=new RenderTexture(1280,720,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();Object.Destroy(target);}
    }
}
#endif
