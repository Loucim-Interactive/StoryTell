#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SceneSystem.Scripts;
using StoryTell.MainIntegration;
using StoryTell.MainIntegration.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

public sealed class WeatherMenuTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    static IEnumerator Loaded(string scene)
    {
        float until=Time.unscaledTime+20;
        while(SceneManager.GetActiveScene().name!=scene||SceneTransitionManager.Instance&&SceneTransitionManager.Instance.IsTransitioning){Assert.That(Time.unscaledTime,Is.LessThan(until));yield return null;}
        yield return null;yield return null;
        Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
    }
    [Test]public void WeatherAssetsHaveValidShadersRaisedTentsAndNonBlockingMud()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        foreach(string name in new[]{"StoryTell/ColdOvercastSky","StoryTell/ColdWeatherSurface","StoryTell/MudSurface","StoryTell/WeatherParticle","StoryTell/WeatherLightShaft","StoryTell/WeatherFlame"}){var shader=Shader.Find(name);Assert.That(shader,Is.Not.Null,name);Assert.That(ShaderUtil.ShaderHasError(shader),Is.False,name);}
        Assert.That(RenderSettings.skybox.GetFloat("_WindSpeed"),Is.GreaterThan(.01));
        var report=JsonUtility.FromJson<WeatherMenuAuthoring.Report>(File.ReadAllText(WeatherMenuAuthoring.Data+"/PlacementReport.json"));Assert.That(report.fires,Is.EqualTo(5));Assert.That(report.tents.Length,Is.GreaterThan(0));
        foreach(var tent in report.tents){Assert.That(tent.after.x,Is.EqualTo(tent.before.x));Assert.That(tent.after.z,Is.EqualTo(tent.before.z));Assert.That(tent.after.y-tent.before.y,Is.InRange(.05f,.19f));}
        var mesh=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Single(m=>m.name=="Mud_VisualPaths");Assert.That(mesh.GetComponent<Collider>(),Is.Null);Assert.That(mesh.sharedMesh.vertexCount,Is.EqualTo(report.mudVertices));Assert.That(mesh.sharedMesh.colors.Any(c=>c.a==0),Is.True);Assert.That(mesh.sharedMesh.colors.Any(c=>c.a>.95f),Is.True);
        Assert.That(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.name=="Cloud_LightShaft"),Is.EqualTo(3));
    }
    [UnityTest]public IEnumerator MenuRoundTripAndCampaignLoadUseOneTransitionService()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");yield return new EnterPlayMode();yield return null;yield return null;
        var start=One<MenuScreenController>().Root.Q<Button>("new-game-button");start.Focus();using(var submit=NavigationSubmitEvent.GetPooled()){submit.target=start;start.SendEvent(submit);}yield return Loaded("GameSelect");
        Assert.That(One<MenuScreenController>().Root.Q<Button>("part-2-button").enabledSelf,Is.False);Assert.That(One<MenuScreenController>().Root.Q<Button>("part-3-button").enabledSelf,Is.False);
        yield return CaptureMenu("Library/WeatherMenu-select-1280x720.png",1280,720);yield return CaptureMenu("Library/WeatherMenu-select-1280x1024.png",1280,1024);
        One<MenuScreenController>().Back();yield return Loaded("MainMenu");yield return CaptureMenu("Library/WeatherMenu-main-1280x720.png",1280,720);
        One<MenuScreenController>().OpenSelection();yield return Loaded("GameSelect");One<MenuScreenController>().Play(true);yield return Loaded("Level1");
        Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));Assert.That(One<PauseMenu>().IsOpen,Is.False);
        Assert.That(Object.FindObjectsByType<SceneTransitionManager>(FindObjectsSortMode.None).Length,Is.EqualTo(1));SceneTransitionManager.Instance.TransitionToScene("MainMenu");yield return Loaded("MainMenu");
        Assert.That(One<MenuScreenController>().Root.Q<Button>("new-game-button").enabledInHierarchy,Is.True);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator MenuContinueRestoresTheStoryTellCheckpointWithoutAnotherPrompt()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");yield return new EnterPlayMode();yield return null;
        Assert.That(new CheckpointStore(MenuScreenController.SavePath).TryWrite(new CheckpointData{equipmentStarted=true,equipmentMask=7,letterStarted=true,letterMask=15,letterCompleted=true,mapUnlocked=true,puzzleMask=15,stage=RouteStage.Front}),Is.True);
        One<MenuScreenController>().ContinueGame();yield return Loaded("Level1");yield return null;yield return null;
        Assert.That(One<LevelProgress>().Stage,Is.EqualTo(RouteStage.Front));Assert.That(One<MainPresentation>().HasMap,Is.True);Assert.That(One<PauseMenu>().IsOpen,Is.False);Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));yield return new ExitPlayMode();
    }
    static IEnumerator CaptureMenu(string path,int width,int height)
    {
        var doc=One<UIDocument>();var panel=doc.panelSettings;var old=panel.targetTexture;var target=new RenderTexture(width,height,24);target.Create();var active=RenderTexture.active;
        try
        {
            panel.targetTexture=target;yield return null;yield return null;yield return null;yield return null;
            RenderTexture.active=target;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);
            string[] names=SceneManager.GetActiveScene().name=="GameSelect"?new[]{"continue-button","part-1-button","part-2-button","part-3-button","back-button"}:new[]{"continue-button","new-game-button","options-button","exit-button"};
            Rect previous=default;
            foreach(string name in names){var button=doc.rootVisualElement.Q<Button>(name);var rect=button.worldBound;Assert.That(rect.width,Is.GreaterThan(70),name);Assert.That(rect.height,Is.GreaterThan(20),name);Assert.That(rect.Overlaps(previous),Is.False,name);previous=rect;Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(-1));Assert.That(rect.xMax,Is.LessThanOrEqualTo(doc.rootVisualElement.worldBound.width+1));}
        }
        finally{panel.targetTexture=old;RenderTexture.active=active;target.Release();Object.Destroy(target);}
    }
    [UnityTest]public IEnumerator CloudDriftAndCampfireEffectsRemainActive()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return Wait(.2f);
        var fires=Object.FindObjectsByType<CampfireMotion>(FindObjectsSortMode.None);Assert.That(fires.Length,Is.EqualTo(5));var light=fires[0].GetComponentInChildren<Light>();float initial=light.intensity;yield return Wait(.7f);Assert.That(Mathf.Abs(light.intensity-initial),Is.GreaterThan(.0001f));
        foreach(var fire in fires){var smoke=fire.transform.Find("WindCarriedSmoke").GetComponent<ParticleSystem>();Assert.That(smoke.velocityOverLifetime.enabled,Is.True);Assert.That(smoke.main.maxParticles,Is.LessThanOrEqualTo(24));}
        var go=new GameObject("Sky motion verification",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.rotation=Quaternion.Euler(-40,20,0);camera.clearFlags=CameraClearFlags.Skybox;camera.cullingMask=0;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        var rt=new RenderTexture(256,128,24);var previous=RenderTexture.active;var a=new Texture2D(256,128,TextureFormat.RGB24,false);var b=new Texture2D(256,128,TextureFormat.RGB24,false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;a.ReadPixels(new Rect(0,0,256,128),0,0);a.Apply();yield return Wait(2);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;b.ReadPixels(new Rect(0,0,256,128),0,0);b.Apply();var pa=a.GetPixels32();var pb=b.GetPixels32();long difference=0;for(int i=0;i<pa.Length;i++)difference+=Mathf.Abs(pa[i].r-pb[i].r)+Mathf.Abs(pa[i].g-pb[i].g)+Mathf.Abs(pa[i].b-pb[i].b);Assert.That(difference,Is.GreaterThan(50));
            File.WriteAllBytes("Library/WeatherMenu-sky.png",b.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(a);Object.Destroy(b);Object.Destroy(go);}
        yield return CaptureWorldDetails();yield return new ExitPlayMode();
    }
    static IEnumerator CaptureWorldDetails()
    {
        var fire=Object.FindObjectsByType<CampfireMotion>(FindObjectsSortMode.None).OrderBy(f=>Vector3.Distance(f.transform.position,new Vector3(85,28,279))).First();
        var go=new GameObject("Weather runtime preview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=58;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
        try
        {
            camera.transform.position=fire.transform.position+new Vector3(1.6f,1.2f,-2);camera.transform.LookAt(fire.transform.position+Vector3.up*.3f);yield return null;yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes("Library/WeatherMenu-fire-runtime.png",pixels.EncodeToPNG());Object.Destroy(pixels);
        }
        finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(go);}
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying){string path=MenuScreenController.SavePath;foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);yield return new ExitPlayMode();}}
}
#endif
