#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using StoryTell.PhysicalMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace StoryTell.GameplayPath.Editor
{
    public static class Level1AtmosphereAuthoring
    {
        const string ScenePath="Assets/Scenes/Level1.unity";
        const string AssetsPath="Assets/Systems/GameplayPath/Data/Atmosphere";
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        public static void RefineShadowReadability()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var atmosphere=All<Level1Atmosphere>(scene).Single();
            AddSkyFill(atmosphere.transform);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CaptureViews("after");
        }
        static void AddSkyFill(Transform root)
        {
            if(root.Find("Cloud_Bounce_Fill"))return;
            var obj=new GameObject("Cloud_Bounce_Fill");Undo.RegisterCreatedObjectUndo(obj,"Keep objects readable in exterior shadows");obj.transform.SetParent(root,false);obj.transform.rotation=Quaternion.Euler(60,145,0);
            var light=Undo.AddComponent<Light>(obj);light.type=LightType.Directional;light.color=new Color(0.72f,0.80f,0.91f);light.intensity=0.45f;light.shadows=LightShadows.None;
        }
        public static void ApplyBatch()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var controller=All<GameplayPathController>(scene).Single();
            if(controller.GetComponentInChildren<Level1Atmosphere>(true))throw new InvalidOperationException("Atmosphere already installed.");
            Directory.CreateDirectory("Library/GameplayPathBackups");File.Copy(scene.path,"Library/GameplayPathBackups/Level1-before-atmosphere-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            Directory.CreateDirectory(AssetsPath);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Readable cold exterior and spatial soundscape");
            var root=new GameObject("Atmosphere_Level1");Undo.RegisterCreatedObjectUndo(root,"Create scene-local atmosphere");root.transform.SetParent(controller.transform,false);
            AddSkyFill(root.transform);
            var sun=All<Light>(scene).Single(l=>l.isActiveAndEnabled&&l.type==LightType.Directional);
            Undo.RecordObject(sun,"Balance the exterior light");Undo.RecordObject(sun.transform,"Set sun direction");sun.transform.rotation=Quaternion.Euler(38,-35,0);sun.intensity=1.4f;sun.color=Color.white;sun.useColorTemperature=true;sun.colorTemperature=6500;sun.shadows=LightShadows.Soft;sun.shadowStrength=0.8f;sun.shadowBias=0.035f;sun.shadowNormalBias=0.25f;RenderSettings.sun=sun;
            foreach(var settings in Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings") is Object setting?new[]{setting}:Array.Empty<Object>())Undo.RecordObject(settings,"Balance ambient and fog");
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(0.48f,0.55f,0.63f);RenderSettings.ambientEquatorColor=new Color(0.32f,0.36f,0.40f);RenderSettings.ambientGroundColor=new Color(0.19f,0.20f,0.21f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(0.52f,0.58f,0.63f);RenderSettings.fogDensity=0.0032f;
            var sky=new Material(RenderSettings.skybox);sky.name="Sky_Level1_ColdDay";sky.SetColor("_SkyTint",new Color(0.47f,0.51f,0.55f));sky.SetColor("_GroundColor",new Color(0.25f,0.27f,0.29f));sky.SetFloat("_AtmosphereThickness",1.3f);sky.SetFloat("_Exposure",1);sky.SetFloat("_SunSize",0.035f);AssetDatabase.CreateAsset(sky,AssetsPath+"/Sky_Level1.mat");RenderSettings.skybox=sky;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Level1_Atmosphere";AssetDatabase.CreateAsset(profile,AssetsPath+"/Level1_Atmosphere.asset");
            var grading=profile.Add<ColorAdjustments>(true);grading.postExposure.value=0.15f;grading.contrast.value=5;grading.saturation.value=-15;grading.colorFilter.value=new Color(0.96f,0.985f,1);
            var balance=profile.Add<WhiteBalance>(true);balance.temperature.value=-5;balance.tint.value=0;
            var bloom=profile.Add<Bloom>(true);bloom.intensity.value=0.10f;bloom.threshold.value=1.2f;bloom.scatter.value=0.5f;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=0.08f;vignette.smoothness.value=0.4f;
            profile.Add<MotionBlur>(true).intensity.value=0;profile.Add<DepthOfField>(true).mode.value=DepthOfFieldMode.Off;
            profile.Add<Tonemapping>(true).mode.value=TonemappingMode.Neutral;
            foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(profile);
            var volume=Undo.AddComponent<Volume>(root);volume.isGlobal=true;volume.priority=15;volume.sharedProfile=profile;
            var camera=Camera.main;var cameraData=camera.GetUniversalAdditionalCameraData();Undo.RecordObject(cameraData,"Enable the dedicated Level1 grading");cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;cameraData.renderShadows=true;
            var glow=new Material(Shader.Find("Universal Render Pipeline/Lit"));glow.name="LanternWarmGlow";glow.SetColor("_BaseColor",new Color(1,0.72f,0.34f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,0.48f,0.12f)*2);AssetDatabase.CreateAsset(glow,AssetsPath+"/LanternGlow.mat");
            var frame=new Material(Shader.Find("Universal Render Pipeline/Lit"));frame.name="LanternDarkMetal";frame.SetColor("_BaseColor",new Color(0.11f,0.12f,0.11f));frame.SetFloat("_Metallic",0.5f);frame.SetFloat("_Smoothness",0.3f);AssetDatabase.CreateAsset(frame,AssetsPath+"/LanternFrame.mat");
            var kitchen=controller.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Kitchen_Table");
            var bounds=GameplayPathScaleAuthoring.VisualBounds(kitchen);Lantern("Lantern_Kitchen",new Vector3(bounds.center.x+0.6f,bounds.max.y+0.02f,bounds.center.z),root.transform,glow,frame);
            var pole=Ground(new Vector3(99,0,270));var poleObject=Primitive("Carpa_LanternPole",PrimitiveType.Cylinder,root.transform,pole+Vector3.up*0.85f,new Vector3(0.045f,0.85f,0.045f),frame);Lantern("Lantern_Carpa",pole+Vector3.up*1.7f,root.transform,glow,frame);
            var ambience=Undo.AddComponent<Level1Atmosphere>(root);Set(ambience,"controller",controller);Set(ambience,"map",All<PhysicalMapController>(scene).Single());Set(ambience,"trial",All<StoryTell.Weapons.Level1WeaponTrial>(scene).Single());Set(ambience,"camp",kitchen);Set(ambience,"frontline",controller.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Zona_Frente"));
            var wind=Sound("Wind_Bed",root.transform,0,0,true,Clip("Ambient/Nature/Wind-Blowing.mp3"));var rustle=Sound("Camp_Grass",root.transform,1,42,true,Clip("Ambient/Nature/Grass-rustling.mp3"));rustle.transform.position=kitchen.position;
            var war=Sound("Front_DistantArtillery",root.transform,0.75f,650,false,null);war.minDistance=80;
            var bird=Sound("Coastal_Seagull",root.transform,1,110,false,null);bird.minDistance=10;
            Set(ambience,"wind",wind);Set(ambience,"campRustle",rustle);Set(ambience,"distantWar",war);Set(ambience,"coastalBird",bird);Set(ambience,"seagull",Clip("Ambient/Nature/Seagull.mp3"));
            var values=new SerializedObject(ambience);var clips=values.FindProperty("distantClips");clips.arraySize=2;clips.GetArrayElementAtIndex(0).objectReferenceValue=Clip("Ambient/War/Explosions/Distant-Explosion01.mp3");clips.GetArrayElementAtIndex(1).objectReferenceValue=Clip("Ambient/War/Explosions/Distant-Explosion02.mp3");values.ApplyModifiedProperties();
            foreach(var item in All<Component>(scene))if(item&&PrefabUtility.IsPartOfPrefabInstance(item))PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Library/GameplayPath-atmosphere-applied.txt","Scene-local lighting, sky, volume, two lanterns and four audio voices installed.");
        }
        static AudioClip Clip(string relative)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/SoundSystem/Assets/"+relative);
        static AudioSource Sound(string name,Transform parent,float spatial,float range,bool loop,AudioClip clip)
        {
            var obj=new GameObject(name);Undo.RegisterCreatedObjectUndo(obj,"Create spatial sound layer");obj.transform.SetParent(parent,false);var source=Undo.AddComponent<AudioSource>(obj);source.playOnAwake=false;source.loop=loop;source.clip=clip;source.volume=0;source.spatialBlend=spatial;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=5;source.maxDistance=Mathf.Max(range,6);source.dopplerLevel=0;source.priority=180;return source;
        }
        static void Lantern(string name,Vector3 basePosition,Transform parent,Material glow,Material metal)
        {
            var obj=new GameObject(name);Undo.RegisterCreatedObjectUndo(obj,"Place practical warm lamp");obj.transform.SetParent(parent,false);obj.transform.position=basePosition;
            Primitive("Glass",PrimitiveType.Cylinder,obj.transform,basePosition+Vector3.up*0.14f,new Vector3(0.12f,0.10f,0.12f),glow);
            Primitive("Base",PrimitiveType.Cylinder,obj.transform,basePosition+Vector3.up*0.025f,new Vector3(0.18f,0.025f,0.18f),metal);
            Primitive("Cap",PrimitiveType.Cylinder,obj.transform,basePosition+Vector3.up*0.26f,new Vector3(0.18f,0.025f,0.18f),metal);
            var lightObject=new GameObject("WarmFill");Undo.RegisterCreatedObjectUndo(lightObject,"Add local warm light");lightObject.transform.SetParent(obj.transform,false);lightObject.transform.localPosition=Vector3.up*0.2f;var light=Undo.AddComponent<Light>(lightObject);light.type=LightType.Point;light.color=new Color(1,0.68f,0.36f);light.intensity=1.8f;light.range=6;light.shadows=LightShadows.None;
        }
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type);Undo.RegisterCreatedObjectUndo(obj,"Build simple field lantern");obj.name=name;obj.transform.SetParent(parent,false);obj.transform.position=position;obj.transform.localScale=scale;Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        static void Set(Object target,string field,Object value){var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedProperties();}
        public static void Baseline()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var report=new StringBuilder();
            report.AppendLine("Pipeline: "+GraphicsSettings.currentRenderPipeline+" / "+QualitySettings.names[QualitySettings.GetQualityLevel()]+" / "+QualitySettings.activeColorSpace);
            foreach(var light in All<Light>(scene))report.AppendLine($"LIGHT {light.name} active={light.isActiveAndEnabled} pos={light.transform.position} rotation={light.transform.eulerAngles} type={light.type} intensity={light.intensity} temp={light.colorTemperature}");
            foreach(var audio in All<AudioSource>(scene))report.AppendLine($"AUDIO {PathOf(audio.transform)} active={audio.isActiveAndEnabled} clip={audio.clip} loop={audio.loop} spatial={audio.spatialBlend} vol={audio.volume} play={audio.playOnAwake} components={string.Join(",",audio.GetComponents<MonoBehaviour>().Where(b=>b).Select(b=>b.GetType().Name))}");
            foreach(var volume in All<Volume>(scene))report.AppendLine($"VOLUME {volume.name} priority={volume.priority} profile={AssetDatabase.GetAssetPath(volume.sharedProfile)}");
            foreach(var camera in All<Camera>(scene))report.AppendLine($"CAMERA {camera.name} post={camera.GetUniversalAdditionalCameraData().renderPostProcessing} mask={camera.cullingMask}");
            File.WriteAllText("Library/GameplayPath-atmosphere-audit.txt",report.ToString());CaptureViews("before");
        }
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        public static void CaptureAfter(){EditorSceneManager.OpenScene(ScenePath);CaptureViews("after");}
        public static void CaptureComparison()
        {
            var backup=Directory.GetFiles("Library/GameplayPathBackups","Level1-before-atmosphere-*.unity").OrderBy(f=>f).Last();
            var preview=AssetDatabase.GenerateUniqueAssetPath(AssetsPath+"/BeforeLightingPreview.unity");
            File.Copy(backup,preview);AssetDatabase.ImportAsset(preview);
            try{EditorSceneManager.OpenScene(preview);CaptureViews("before");}
            finally{EditorSceneManager.OpenScene(ScenePath);AssetDatabase.DeleteAsset(preview);}
            CaptureViews("after");
        }
        public static void CaptureViews(string suffix)
        {
            var original=Camera.main;var host=new GameObject("AtmosphereVerificationCamera");var camera=host.AddComponent<Camera>();camera.CopyFrom(original);camera.enabled=false;camera.cullingMask&=~((1<<5)|(1<<9)|(1<<10));
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=original.GetUniversalAdditionalCameraData().renderPostProcessing;data.antialiasing=original.GetUniversalAdditionalCameraData().antialiasing;data.renderShadows=true;camera.fieldOfView=65;camera.allowHDR=true;
            // SDR output must be sRGB; directly encoding a linear HDR render target makes PNGs too dark.
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                Vector3[] positions={new(80,0,282),new(82,0,282),new(100,0,285),new(56,0,540)};
                Vector3[] targets={new(89,0,294),new(77.7f,0,277.4f),new(84,0,284),new(54,0,577)};
                string[] names={"kitchen","equipment","camp","front"};
                for(int i=0;i<positions.Length;i++)
                {
                    camera.transform.position=Ground(positions[i])+Vector3.up*1.7f;camera.transform.LookAt(Ground(targets[i])+Vector3.up*1.1f);camera.targetTexture=rt;
                    VolumeManager.instance.Update(camera.transform,data.volumeLayerMask);
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                    File.WriteAllBytes($"Library/Atmosphere-{names[i]}-{suffix}.png",pixels.EncodeToPNG());
                }
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);Object.DestroyImmediate(host);}
        }
        static Vector3 Ground(Vector3 point)
        {
            var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.z>=t.transform.position.z&&point.x<=t.transform.position.x+t.terrainData.size.x&&point.z<=t.transform.position.z+t.terrainData.size.z);
            point.y=terrain.SampleHeight(point)+terrain.transform.position.y;return point;
        }
    }
}
#endif
