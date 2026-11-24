#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RoadsideCratersTests
{
    [Test]public void RoadsideImpactsHaveDepthWithoutMovingSheltersOrChangingTheMainPath()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=Object.FindAnyObjectByType<Terrain>();var before=AssetDatabase.LoadAssetAtPath<TerrainData>(MapFrontCratersAuthoring.Data+"/FrontTerrain.asset");var origin=terrain.transform.position;
        float Previous(float x,float z)=>before.GetInterpolatedHeight((x-origin.x)/before.size.x,(z-origin.z)/before.size.z)+origin.y;
        Assert.That(terrain.terrainData,Is.Not.SameAs(before));var collider=terrain.GetComponent<TerrainCollider>();Assert.That(collider.terrainData,Is.SameAs(terrain.terrainData));
        var shelters=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Collapsed shelter")||r.name.StartsWith("Refugio dañado")).ToArray();Assert.That(shelters.Length,Is.EqualTo(4));
        foreach(var impact in MapFrontCratersAuthoring.RoadsideImpacts)
        {
            float actual=terrain.SampleHeight(new Vector3(impact.x,0,impact.y))+origin.y;Assert.That(Previous(impact.x,impact.y)-actual,Is.InRange(impact.w*.45f,impact.w*1.1f));Assert.That(collider.Raycast(new Ray(new Vector3(impact.x,200,impact.y),Vector3.down),out var hit,400),Is.True);Assert.That(hit.point.y,Is.EqualTo(actual).Within(.15f));
            float nearest=shelters.Min(s=>Vector2.Distance(new Vector2(s.bounds.center.x,s.bounds.center.z),new Vector2(impact.x,impact.y)));Assert.That(nearest,Is.LessThan(20),"Keep each impact near damaged canvas");
        }
        foreach(var s in shelters)for(int z=0;z<5;z++)for(int x=0;x<5;x++){float wx=Mathf.Lerp(s.bounds.min.x,s.bounds.max.x,x/4f),wz=Mathf.Lerp(s.bounds.min.z,s.bounds.max.z,z/4f);Assert.That(terrain.SampleHeight(new Vector3(wx,0,wz))+origin.y,Is.EqualTo(Previous(wx,wz)).Within(.001f),s.name);}
        foreach(var p in new[]{new Vector2(72,410),new Vector2(72,420),new Vector2(73,435),new Vector2(70,450),new Vector2(65,465),new Vector2(55,480),new Vector2(47,490),new Vector2(45,500),new Vector2(83.7f,253.5f),new Vector2(56,531),new Vector2(57,571)})Assert.That(terrain.SampleHeight(new Vector3(p.x,0,p.y))+origin.y,Is.EqualTo(Previous(p.x,p.y)).Within(.001f),p.ToString());
        foreach(var p in MapFrontCratersAuthoring.Craters)Assert.That(terrain.SampleHeight(new Vector3(p.x,0,p.y))+origin.y,Is.EqualTo(Previous(p.x,p.y)).Within(.001f),"Keep earlier combat craters unchanged");
        Assert.That(Object.FindObjectsByType<MeshRenderer>().Count(r=>r.name.StartsWith("Cráter junto al camino ")),Is.EqualTo(4));
    }
    [UnityTest]public IEnumerator ReviewCraterPlacementBesideDamagedCanvas()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<8;i++)yield return null;var terrain=Object.FindAnyObjectByType<Terrain>();var go=new GameObject("Roadside crater review camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=3200;camera.cullingMask=~(1<<5);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        Vector3 At(float x,float z,float height=0)=>new(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+height,z);
        camera.transform.position=At(66,402,1.75f);camera.transform.LookAt(At(56,410,.1f));yield return null;yield return Capture(camera,"Library/RoadsideCraters-first-shelter.png");camera.transform.position=At(89,437,1.75f);camera.transform.LookAt(At(90,446));yield return null;yield return Capture(camera,"Library/RoadsideCraters-middle-shelter.png");camera.transform.position=At(48,485,1.75f);camera.transform.LookAt(At(38,490));yield return null;yield return Capture(camera,"Library/RoadsideCraters-last-shelters.png");Object.Destroy(go);yield return new ExitPlayMode();
    }
    static IEnumerator Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);target.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;target.Release();Object.Destroy(target);}yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
