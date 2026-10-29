#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class ColdStyleAuthoring
    {
        public const string Data="Assets/Systems/MainIntegration/Data/ColdStyle";
        [Serializable] public sealed class Record {public string role,original,variant;public int users;}
        [Serializable] public sealed class Manifest {public Record[] materials;public string lighting;}
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        static string Role(Renderer r,Material m)
        {
            string path=PathOf(r.transform).ToLowerInvariant(),asset=AssetDatabase.GetAssetPath(m).ToLowerInvariant();
            if(path.Contains("physicalmap")||path.Contains("fire")||path.Contains("carta")||path.Contains("diario")||path.Contains("puzzle")||path.Contains("vfx")||r is ParticleSystemRenderer)return null;
            if(m.HasProperty("_Surface")&&m.GetFloat("_Surface")>.1f)return null;
            if(m.IsKeywordEnabled("_EMISSION"))return null;
            if(path.Contains("tent_"))return "Canvas";
            if(path.Contains("rockcl")||path.Contains("sm_rocks"))return "Stone";
            if(path.Contains("wooden_table")||path.Contains("workbench")||path.Contains("pallet")||path.Contains("lettersupport")||path.Contains("box3")||path.Contains("supplycache"))return "Wood";
            if(path.Contains("sandbag"))return "Sandbags";
            if(asset.Contains("npcs/soldier")&&(m.name.ToLowerInvariant().Contains("campera")||m.name.ToLowerInvariant().Contains("pantalon")||m.name.ToLowerInvariant().Contains("casco")))return "Uniform";
            if(path.Contains("jerrycan")||path.Contains("fuel canister")||path.Contains("barrel_"))return "Metal";
            return null;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            if(AssetDatabase.LoadAssetAtPath<TextAsset>(Data+"/Manifest.json"))throw new InvalidOperationException("Cold style already applied.");
            Directory.CreateDirectory("Library/LevelPolishBackups");File.Copy("Assets/Scenes/Level1.unity","Library/LevelPolishBackups/BeforeColdStyle-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            string lighting=ExplorationPolishAuthoring.CaptureLighting(scene);CaptureViews(scene,"before");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();var shader=Shader.Find("StoryTell/ColdWeatherSurface");if(!shader)throw new InvalidOperationException("Missing weather shader.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Cold damp materials for Level1");
            var cache=new Dictionary<string,Material>();var records=new Dictionary<string,Record>();
            foreach(var renderer in All<Renderer>(scene))
            {
                if(renderer is ParticleSystemRenderer)continue;
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(!source||!source.HasProperty("_BaseMap"))continue;var role=Role(renderer,source);if(role==null)continue;
                    string path=AssetDatabase.GetAssetPath(source),key=role+"_"+AssetDatabase.AssetPathToGUID(path);
                    if(!cache.TryGetValue(key,out var variant))
                    {
                        variant=new Material(source){name="Cold_"+role+"_"+source.name,shader=shader};variant.shaderKeywords=System.Array.Empty<string>();
                        variant.SetFloat("_HasMetallicMap",source.HasProperty("_MetallicGlossMap")&&source.GetTexture("_MetallicGlossMap")?1:0);
                        Color tint;float saturation,wetness,mud,smoothness;
                        switch(role)
                        {
                            case "Canvas":tint=new Color(.62f,.98f,.83f);saturation=.08f;wetness=.2f;mud=.25f;smoothness=.12f;break;
                            case "Wood":tint=new Color(.82f,.88f,.84f);saturation=.55f;wetness=.26f;mud=.14f;smoothness=.15f;break;
                            case "Stone":tint=new Color(.85f,.94f,.98f);saturation=.65f;wetness=.35f;mud=.14f;smoothness=.2f;break;
                            case "Sandbags":tint=new Color(.85f,.9f,.86f);saturation=.5f;wetness=.17f;mud=.28f;smoothness=.08f;break;
                            case "Uniform":tint=new Color(.88f,.94f,.9f);saturation=.65f;wetness=.08f;mud=0;smoothness=.12f;break;
                            default:tint=new Color(.83f,.92f,.96f);saturation=.6f;wetness=.18f;mud=.16f;smoothness=.24f;break;
                        }
                        variant.SetColor("_ColdTint",tint);variant.SetFloat("_Saturation",saturation);variant.SetFloat("_Wetness",wetness);variant.SetFloat("_MudAmount",mud);variant.SetFloat("_MudHeight",.4f);variant.SetFloat("_Smoothness",smoothness);
                        string variantPath=Data+"/"+key+".mat";AssetDatabase.CreateAsset(variant,variantPath);cache.Add(key,variant);records.Add(key,new Record{role=role,original=path,variant=variantPath});
                    }
                    materials[i]=variant;records[key].users++;changed=true;
                }
                if(!changed)continue;Undo.RecordObject(renderer,"Assign a cold variant without modifying the source material");renderer.sharedMaterials=materials;
                if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            File.WriteAllText(Data+"/Manifest.json",JsonUtility.ToJson(new Manifest{materials=records.Values.OrderBy(r=>r.variant).ToArray(),lighting=lighting},true));
            if(lighting!=ExplorationPolishAuthoring.CaptureLighting(scene))throw new InvalidOperationException("Lighting changed.");
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CaptureViews(scene,"after");
        }
        public static void CaptureViews(Scene scene,string suffix)
        {
            var terrain=All<Terrain>(scene).Single();float Y(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            Capture(new Vector3(85,Y(85,276)+1.7f,276),new Vector3(96.5f,Y(96.5f,265)+1.3f,265),"camp",suffix);
            Capture(new Vector3(90,Y(90,257)+1.8f,257),new Vector3(91.4f,28.8f,260.4f),"equipment",suffix);
            var rock=All<MeshRenderer>(scene).Where(r=>r.name.StartsWith("RockCl")).OrderBy(r=>Vector3.Distance(r.bounds.center,new Vector3(85,28,279))).First();
            Capture(rock.bounds.center+new Vector3(1,.35f,-1).normalized*Mathf.Max(5,rock.bounds.extents.magnitude*2),rock.bounds.center,"stone",suffix);
        }
        public static void RefineCanvas()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Data+"/Manifest.json"));
            foreach(var entry in manifest.materials.Where(m=>m.role=="Canvas"))
            {var material=AssetDatabase.LoadAssetAtPath<Material>(entry.variant);Undo.RecordObject(material,"Quiet grey-green canvas");material.SetColor("_ColdTint",new Color(.62f,.98f,.83f));material.SetFloat("_Saturation",.08f);EditorUtility.SetDirty(material);}
            AssetDatabase.SaveAssets();CaptureViews(scene,"final");
        }
        static void Capture(Vector3 position,Vector3 target,string label,string suffix)
        {
            var go=new GameObject("Temporary cold-style preview");var camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=58;camera.nearClipPlane=.06f;camera.farClipPlane=1500;camera.cullingMask=~(1<<LayerMask.NameToLayer("MainPhysicalMap"));
            var texture=new RenderTexture(1280,720,24);var active=RenderTexture.active;
            try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/ColdStyle-"+label+"-"+suffix+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}
            finally{RenderTexture.active=active;camera.targetTexture=null;texture.Release();Object.DestroyImmediate(texture);Object.DestroyImmediate(go);}
        }
    }
}
#endif
