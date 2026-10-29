#if UNITY_EDITOR
using System.IO;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class ColdStyleTests
{
    [UnityTest] public IEnumerator ColdMaterialsRenderInThePlayerCamera()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        var camera=Camera.main;var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/ColdStyle-player.png",image.EncodeToPNG());Object.Destroy(image);Assert.That(ShaderUtil.ShaderHasError(Shader.Find("StoryTell/ColdWeatherSurface")),Is.False);}
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();Object.Destroy(target);}
        yield return new ExitPlayMode();
    }
    [Test] public void VariantsPreserveTexturesAndOriginalsAndProvideLightingPasses()
    {
        var manifest=JsonUtility.FromJson<ColdStyleAuthoring.Manifest>(File.ReadAllText(ColdStyleAuthoring.Data+"/Manifest.json"));
        foreach(var role in new[]{"Canvas","Wood","Stone"})Assert.That(manifest.materials.Any(m=>m.role==role),Is.True,role);
        foreach(var entry in manifest.materials)
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>(entry.original);var variant=AssetDatabase.LoadAssetAtPath<Material>(entry.variant);
            Assert.That(variant,Is.Not.Null);Assert.That(variant,Is.Not.SameAs(source));Assert.That(source.shader.name,Is.Not.EqualTo("StoryTell/ColdWeatherSurface"));
            Assert.That(variant.GetTexture("_BaseMap"),Is.EqualTo(source.GetTexture("_BaseMap")),entry.variant);
            if(source.HasProperty("_BumpMap"))Assert.That(variant.GetTexture("_BumpMap"),Is.EqualTo(source.GetTexture("_BumpMap")),entry.variant);
            Assert.That(ShaderUtil.ShaderHasError(variant.shader),Is.False,entry.variant);
            foreach(var pass in new[]{"ForwardLit","ShadowCaster","DepthOnly","DepthNormals"})Assert.That(variant.FindPass(pass),Is.GreaterThanOrEqualTo(0),pass);
            Assert.That(variant.GetFloat("_Saturation"),Is.LessThan(1));
        }
    }
    [Test] public void MaterialAssignmentsPersistAndApprovedLightingRemainsIdentical()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var manifest=JsonUtility.FromJson<ColdStyleAuthoring.Manifest>(File.ReadAllText(ColdStyleAuthoring.Data+"/Manifest.json"));
        Assert.That(ExplorationPolishAuthoring.CaptureLighting(scene),Is.EqualTo(manifest.lighting));
        var materials=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m).ToArray();
        foreach(var entry in manifest.materials)Assert.That(materials.Count(m=>AssetDatabase.GetAssetPath(m)==entry.variant),Is.EqualTo(entry.users),entry.variant);
        Assert.That(materials.Where(m=>m.name.Contains("SoftFire")).All(m=>m.shader.name=="StoryTell/SoftEmberParticle"),Is.True);
    }
}
#endif
