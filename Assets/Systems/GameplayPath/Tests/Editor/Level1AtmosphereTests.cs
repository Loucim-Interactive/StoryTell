#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.GameplayPath;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class Level1AtmosphereTests
{
    [Test]public void SavedAtmosphere_UsesLocalAssets_AndKeepsMapAndWeaponCamerasClear()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var all=scene.GetRootGameObjects();
        var atmosphere=all.SelectMany(g=>g.GetComponentsInChildren<Level1Atmosphere>(true)).Single();
        var sources=atmosphere.GetComponentsInChildren<AudioSource>(true);Assert.That(sources.Length,Is.EqualTo(4));
        Assert.That(atmosphere.Wind.clip,Is.Not.Null);Assert.That(atmosphere.Wind.loop,Is.True);
        foreach(var source in sources){Assert.That(source.playOnAwake,Is.False);Assert.That(source.dopplerLevel,Is.EqualTo(0));}
        var volume=atmosphere.GetComponent<Volume>();Assert.That(volume.priority,Is.GreaterThan(0));
        Assert.That(volume.sharedProfile.TryGet<MotionBlur>(out var blur),Is.True);Assert.That(blur.intensity.value,Is.Zero);
        Assert.That(volume.sharedProfile.TryGet<ColorAdjustments>(out var grading),Is.True);Assert.That(grading.saturation.value,Is.LessThan(0));
        Assert.That(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing,Is.True);
        foreach(var camera in all.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))if(camera.name=="PhysicalMapCamera"||camera.name=="WeaponViewCamera")Assert.That(camera.GetUniversalAdditionalCameraData().renderPostProcessing,Is.False);
        Assert.That(RenderSettings.sun.colorTemperature,Is.InRange(6000,7000));Assert.That(RenderSettings.sun.shadows,Is.EqualTo(LightShadows.Soft));Assert.That(RenderSettings.ambientMode,Is.EqualTo(AmbientMode.Trilight));
        Assert.That(atmosphere.GetComponentsInChildren<Light>(true).Count(l=>l.type==LightType.Point),Is.EqualTo(2));
        Assert.That(atmosphere.GetComponentsInChildren<Light>(true).Single(l=>l.name=="Cloud_Bounce_Fill").shadows,Is.EqualTo(LightShadows.None));
        Assert.That(atmosphere.GetComponentsInChildren<Collider>(true).Length,Is.Zero,"Decorative lanterns must not obstruct the quest route.");
    }
    [UnityTest]public IEnumerator SpatialMix_TransitionsToFront_DucksForReading_AndFadesAtEnding()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var atmosphere=Object.FindAnyObjectByType<Level1Atmosphere>();var c=Object.FindAnyObjectByType<GameplayPathController>();
        c.Player.GetComponent<PlayerControllerScript>().enabled=false;
        float until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
        Assert.That(atmosphere.FrontProximity,Is.LessThan(0.1f));Assert.That(atmosphere.Wind.volume,Is.GreaterThan(0));
        var target=GameObject.Find("Zona_Frente").transform.position;var cc=c.Player.GetComponent<CharacterController>();cc.enabled=false;c.Player.position=target+Vector3.up*0.2f;cc.enabled=true;
        until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
        Assert.That(atmosphere.FrontProximity,Is.GreaterThan(0.95f));Assert.That(RenderSettings.fogDensity,Is.EqualTo(0.0045f).Within(0.0001f));float outsideVolume=atmosphere.Wind.volume;
        c.OpenReading("Prueba de mezcla durante lectura");until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
        Assert.That(atmosphere.Wind.volume,Is.LessThan(outsideVolume*0.55f));
        var progress=c.Progress;progress.Advance(PathAction.Ruben);progress.Collect(PathEquipment.All);progress.Advance(PathAction.Ruben);progress.Advance(PathAction.Eat);progress.Advance(PathAction.Ruben);
        for(int i=0;i<4;i++)progress.CollectFragment(i);progress.AssembleLetter();progress.Advance(PathAction.Ruben);progress.Advance(PathAction.Sergeant);progress.Advance(PathAction.Crate);progress.Advance(PathAction.Officer);progress.Advance(PathAction.BattleEntrance);
        Assert.That(c.FinishBattle(true),Is.True);until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;
        Assert.That(atmosphere.Wind.volume,Is.EqualTo(0).Within(0.001f));Assert.That(atmosphere.DistantWar.isPlaying,Is.False);
        // Closing a disabled atmosphere restores the scene fog rather than leaving runtime globals behind.
        atmosphere.enabled=false;Assert.That(RenderSettings.fogDensity,Is.EqualTo(0.0032f).Within(0.0001f));Assert.That(atmosphere.Wind.isPlaying,Is.False);
        yield return new ExitPlayMode();
    }
    [Test]public void CandidateWorldCaptures_HaveReadableMidtones()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        StoryTell.GameplayPath.Editor.Level1AtmosphereAuthoring.CaptureViews("after");
        foreach(string name in new[]{"kitchen","equipment","camp","front"})
        {
            string file="Library/Atmosphere-"+name+"-after.png";Assert.That(File.Exists(file),Is.True,file);
            var pixels=new Texture2D(2,2);try{pixels.LoadImage(File.ReadAllBytes(file));var samples=pixels.GetPixels32();int readable=0;foreach(var p in samples)if(Mathf.Max(p.r,Mathf.Max(p.g,p.b))>45&&Mathf.Max(p.r,Mathf.Max(p.g,p.b))<248)readable++;Assert.That((float)readable/samples.Length,Is.GreaterThan(0.4f),name+" must contain readable midtones; this supplements visual review.");}finally{Object.DestroyImmediate(pixels);}
        }
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
