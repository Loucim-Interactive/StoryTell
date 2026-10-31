#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using StoryTell.MainIntegration;
using Object=UnityEngine.Object;

public sealed class CloudSunlightTests
{
    [UnityTest]public IEnumerator CloudsProjectMovingSunlitAndCoveredAreas()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        foreach(string name in new[]{"StoryTell/ColdWeatherSurface","StoryTell/MudSurface","StoryTell/WeatherLightShaft"})Assert.That(ShaderUtil.ShaderHasError(Shader.Find(name)),Is.False,name);
        yield return new EnterPlayMode();yield return null;yield return null;
        var movement=Object.FindAnyObjectByType<MovingCloudSunlight>();Assert.That(movement,Is.Not.Null);var sun=movement.GetComponent<Light>();Assert.That(sun.cookie,Is.Not.Null);
        var lightData=sun.GetComponent<UniversalAdditionalLightData>();Assert.That(lightData.lightCookieSize,Is.EqualTo(new Vector2(110,110)));
        var originalOffset=lightData.lightCookieOffset;yield return null;yield return null;Assert.That(lightData.lightCookieOffset,Is.Not.EqualTo(originalOffset));
        movement.enabled=false;float intensity=sun.intensity;var ambient=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
        var terrain=Object.FindAnyObjectByType<Terrain>();float ground=terrain.SampleHeight(new Vector3(85,0,279))+terrain.transform.position.y;
        var go=new GameObject("Cloud coverage visual verification",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(96,ground+15,298);camera.transform.LookAt(new Vector3(83,ground,277));camera.fieldOfView=58;camera.nearClipPlane=.06f;camera.farClipPlane=1500;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;var first=new Texture2D(1280,720,TextureFormat.RGB24,false);var second=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            movement.Evaluate(0);yield return null;yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;first.ReadPixels(new Rect(0,0,1280,720),0,0);first.Apply();File.WriteAllBytes("Library/CloudSunlight-camp-0.png",first.EncodeToPNG());
            movement.Evaluate(50);yield return null;yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;second.ReadPixels(new Rect(0,0,1280,720),0,0);second.Apply();File.WriteAllBytes("Library/CloudSunlight-camp-50.png",second.EncodeToPNG());
            var a=first.GetPixels32();var b=second.GetPixels32();long difference=0;for(int y=0;y<360;y++)for(int x=0;x<1280;x++){int i=y*1280+x;difference+=Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b);}
            float mean=difference/(1280f*360*3*255);File.WriteAllText("Library/CloudSunlight-render-check.txt","Mean lower-frame change: "+mean);Assert.That(mean,Is.GreaterThan(.005f),"Cloud drift must visibly change actual surfaces");
            Assert.That(sun.intensity,Is.EqualTo(intensity));Assert.That(RenderSettings.ambientLight,Is.EqualTo(ambient));Assert.That(RenderSettings.fog,Is.EqualTo(fog));
        }
        finally{RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(first);Object.Destroy(second);Object.Destroy(go);}
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
