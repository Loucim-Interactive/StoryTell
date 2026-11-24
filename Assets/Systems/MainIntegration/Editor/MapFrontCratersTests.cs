#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class MapFrontCratersTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    [Test]public void CratersAreRealDepressionsWithCollisionAndUnchangedGroundOutsideThem()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=One<Terrain>();var data=terrain.terrainData;var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Art/Maximo/Terrain/TerrainSet1.asset");Assert.That(data,Is.Not.SameAs(original));Assert.That(terrain.GetComponent<TerrainCollider>().terrainData,Is.SameAs(data));var origin=terrain.transform.position;
        foreach(var c in MapFrontCratersAuthoring.Craters){float before=original.GetInterpolatedHeight((c.x-origin.x)/original.size.x,(c.y-origin.z)/original.size.z)+origin.y,after=terrain.SampleHeight(new Vector3(c.x,0,c.y))+origin.y;Assert.That(before-after,Is.InRange(c.w*.55f,c.w*1.1f),c.ToString());Assert.That(terrain.GetComponent<TerrainCollider>().Raycast(new Ray(new Vector3(c.x,200,c.y),Vector3.down),out var hit,400),Is.True);Assert.That(hit.point.y,Is.EqualTo(after).Within(.15f));}
        foreach(var p in new[]{new Vector2(83.7f,253.5f),new Vector2(93,278),new Vector2(56,531),new Vector2(57,571),new Vector2(45,500)})Assert.That(terrain.SampleHeight(new Vector3(p.x,0,p.y)),Is.EqualTo(original.GetInterpolatedHeight((p.x-origin.x)/original.size.x,(p.y-origin.z)/original.size.z)).Within(.001f));
        Assert.That(Object.FindObjectsByType<MeshRenderer>().Count(r=>r.name.StartsWith("Cráter de impacto ")),Is.EqualTo(5));
    }
    [Test]public void EnemyRoutesStillReachThePlayerAcrossTheCraterField()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var data=(NavMeshData)new SerializedObject(One<CombatNavigation>()).FindProperty("data").objectReferenceValue;var instance=NavMesh.AddNavMeshData(data);
        try
        {
            var terrain=One<Terrain>();Vector3 Ground(float x,float z)=>new(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
            Assert.That(NavMesh.SamplePosition(Ground(56,531),out var goal,4,NavMesh.AllAreas),Is.True);
            foreach(var p in new[]{new Vector2(57,571),new Vector2(25,580),new Vector2(96,590),new Vector2(70,620)}){Assert.That(NavMesh.SamplePosition(Ground(p.x,p.y),out var start,5,NavMesh.AllAreas),Is.True,p.ToString());var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(start.position,goal.position,NavMesh.AllAreas,path),Is.True,p.ToString());Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),p.ToString());}
        }
        finally{instance.Remove();}
    }
    [UnityTest]public IEnumerator ReopeningMapShowsOnlyCleanPaperAndRoundNumberedDestinations()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;One<MainPresentation>().UnlockMap();var map=One<PhysicalMapController>();var view=One<PhysicalMapViewCamera>();var image=(RawImage)new SerializedObject(view).FindProperty("surface").objectReferenceValue;
        for(int opening=0;opening<3;opening++)
        {
            map.SetOpen(true,true);Assert.That(image.enabled,Is.False,"Do not expose a stale or uninitialized map frame");yield return null;yield return null;Assert.That(view.Target,Is.Not.Null);RenderPipeline.SubmitRenderRequest(view.MapCamera,new RenderPipeline.StandardRequest{destination=view.Target});Assert.That(image.enabled,Is.True,"Show paper only after its camera has rendered");
            var overlay=One<MapZoneOverlay>();overlay.Refresh();Assert.That(overlay.Zones.Length,Is.EqualTo(8));foreach(var zone in overlay.Zones){var icon=zone.marker.GetComponent<Image>();Assert.That(icon.sprite,Is.Not.Null);Assert.That(icon.sprite.name,Does.Contain("Knob"));Assert.That(zone.marker.gameObject.activeInHierarchy,Is.True);}
            if(opening==1){Camera.main.transform.rotation*=Quaternion.Euler(0,25,0);yield return null;RenderPipeline.SubmitRenderRequest(view.MapCamera,new RenderPipeline.StandardRequest{destination=view.Target});yield return MainIntegrationTests.Capture("Library/MapFrontCraters-map.png",1280,720);}
            map.SetOpen(false,true);Assert.That(image.enabled,Is.False);Assert.That(view.MapCamera.enabled,Is.False);yield return null;
        }
        var clean=new Texture2D(2,2);clean.LoadImage(File.ReadAllBytes(MapFrontCratersAuthoring.Data+"/CleanSurvey.png"));int squares=clean.GetPixels32().Count(p=>p.r==84&&p.g==87&&p.b==64);Assert.That(squares,Is.EqualTo(0),"Do not bake the old rectangular tent footprints onto the paper");Object.Destroy(clean);yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator CaptureFrontCratersAtPlayerHeight()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();for(int i=0;i<8;i++)yield return null;var terrain=One<Terrain>();var go=new GameObject("Crater field review camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=3200;camera.cullingMask=~(1<<5);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        var center=new Vector3(94,terrain.SampleHeight(new Vector3(94,0,546)),546);camera.transform.position=new Vector3(94,terrain.SampleHeight(new Vector3(94,0,535))+1.75f,535);camera.transform.LookAt(center);yield return null;yield return Capture(camera,"Library/MapFrontCraters-crater-near.png");camera.transform.position=new Vector3(57,terrain.SampleHeight(new Vector3(57,0,542))+4,542);camera.transform.LookAt(new Vector3(70,terrain.SampleHeight(new Vector3(70,0,570)),570));yield return null;yield return Capture(camera,"Library/MapFrontCraters-front.png");Object.Destroy(go);yield return new ExitPlayMode();
    }
    static IEnumerator Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);target.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;target.Release();Object.Destroy(target);}yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
