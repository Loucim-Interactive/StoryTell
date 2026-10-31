#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StoryTell.MainIntegration.Editor
{
    public static class CloudSunlightAuthoring
    {
        public const string Data="Assets/Systems/MainIntegration/Data/WeatherMenu";
        static float TileNoise(float x,float y,float frequency,float seed)
        {
            float a=Mathf.PerlinNoise(x*frequency+seed,y*frequency+seed);
            float b=Mathf.PerlinNoise((x-1)*frequency+seed,y*frequency+seed);
            float c=Mathf.PerlinNoise(x*frequency+seed,(y-1)*frequency+seed);
            float d=Mathf.PerlinNoise((x-1)*frequency+seed,(y-1)*frequency+seed);
            return Mathf.Lerp(Mathf.Lerp(a,b,x),Mathf.Lerp(c,d,x),y);
        }
        static Texture2D WriteCookie()
        {
            const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1);
                float noise=TileNoise(u,v,4,17.3f)*.8f+TileNoise(u,v,9,31.7f)*.2f;
                float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.57f,noise));
                float value=Mathf.Lerp(.14f,1,opening);pixels[y*size+x]=new Color(value,value,value,1);
            }
            texture.SetPixels(pixels);texture.Apply();string path=Data+"/MovingCloudCookie.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var sun=RenderSettings.sun?RenderSettings.sun:scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional);
            if(sun.GetComponent<MovingCloudSunlight>())throw new InvalidOperationException("Cloud sunlight already applied");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(!pipeline||!pipeline.supportsLightCookies)throw new InvalidOperationException("Active pipeline must support light cookies");
            Directory.CreateDirectory("Library/CloudSunlight-backup");File.Copy("Assets/Scenes/Level1.unity","Library/CloudSunlight-backup/Level1.unity",false);
            var cookie=WriteCookie();
            Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Moving patches of sunlight beneath clouds");Undo.RecordObject(sun,"Cloud-filtered sun");float previous=sun.intensity;sun.cookie=cookie;sun.intensity=previous*3;
            var additional=sun.GetComponent<UniversalAdditionalLightData>();if(!additional)additional=Undo.AddComponent<UniversalAdditionalLightData>(sun.gameObject);Undo.RecordObject(additional,"World-sized cloud coverage");additional.lightCookieSize=new Vector2(110,110);additional.lightCookieOffset=new Vector2(12,7);Undo.AddComponent<MovingCloudSunlight>(sun.gameObject);
            if(PrefabUtility.IsPartOfPrefabInstance(sun)){PrefabUtility.RecordPrefabInstancePropertyModifications(sun);PrefabUtility.RecordPrefabInstancePropertyModifications(additional);}
            var shaft=AssetDatabase.LoadAssetAtPath<Material>(Data+"/CloudLightShafts.mat");Undo.RecordObject(shaft,"Soft rays only under openings");var color=shaft.GetColor("_Color");color.a=.085f;shaft.SetColor("_Color",color);EditorUtility.SetDirty(shaft);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Library/CloudSunlight-applied.txt",$"Sun intensity {previous} -> {sun.intensity}. Cookie size 110 m; transmission 14%-100%. Ambient, fog, exposure and sun angle preserved.");
        }
        public static void RefineContrast()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var sun=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MovingCloudSunlight>(true)).Single().GetComponent<Light>();
            Undo.RecordObject(sun,"Readable sunlight between clouds");sun.cookie=WriteCookie();sun.intensity=2.7f;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
#endif
