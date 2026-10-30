#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class WeatherMenuAuthoring
    {
        public const string Data="Assets/Systems/MainIntegration/Data/WeatherMenu";
        public const string Menus="Assets/Systems/MainIntegration/Menu/Resources/StoryMenus";
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static void Set(Object o,string field,Object value){var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
        static GameObject New(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Weather polish");g.transform.SetParent(parent,false);return g;}
        static Material Mat(string name,string shader,Color color){var m=new Material(Shader.Find(shader));if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);AssetDatabase.CreateAsset(m,Data+"/"+name+".mat");return m;}
        [Serializable]public sealed class TentPlacement{public string name;public Vector3 before,after;}
        [Serializable]public sealed class Report{public TentPlacement[] tents;public int fires,mudVertices;}
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(scene.GetRootGameObjects().Any(g=>g.name=="Weather_MudAndLight"))throw new InvalidOperationException("Already applied");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            var root=New("Weather_MudAndLight",null);var terrain=All<Terrain>(scene).Single();
            float Ground(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            var sky=new Material(RenderSettings.skybox);sky.SetFloat("_WindSpeed",.018f);AssetDatabase.CreateAsset(sky,Data+"/MovingSky.mat");RenderSettings.skybox=sky;
            var camera=Camera.main?Camera.main:All<Camera>(scene).First(c=>c.CompareTag("MainCamera"));camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            var sun=All<Light>(scene).First(l=>l.type==LightType.Directional);var direction=sun.transform.forward;if(direction.y>-.2f)direction=new Vector3(.35f,-.85f,.3f).normalized;sky.SetVector("_SunDirection",-direction);
            var shaft=Mat("CloudLightShafts","StoryTell/WeatherLightShaft",new Color(.65f,.74f,.85f,.045f));
            foreach(var point in new[]{new Vector2(91,280),new Vector2(101,350),new Vector2(64,405)})
            {
                var basePoint=new Vector3(point.x,Ground(point.x,point.y),point.y);var g=New("Cloud_LightShaft",root.transform);var mesh=new Mesh{name="Soft cloud opening ray"};var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int plane=0;plane<2;plane++){var side=(plane==0?Vector3.right:Vector3.forward)*9;int n=v.Count;v.Add(basePoint-side);v.Add(basePoint+side);v.Add(basePoint-direction*90+side*.35f);v.Add(basePoint-direction*90-side*.35f);uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
                mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/LightRay_"+point.y+".asset");g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=shaft;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            var tents=new List<TentPlacement>();var roots=All<MeshRenderer>(scene).Where(r=>r.name.StartsWith("Tent_")).Select(r=>{var p=PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);return p&&(p.name.ToLowerInvariant().Contains("tent")||p.name.StartsWith("LetterCamp"))?p:r.gameObject;}).Distinct().ToArray();
            foreach(var tent in roots)
            {
                var renderers=tent.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float surface=Ground(bounds.center.x,bounds.center.z);float lift=Mathf.Clamp(surface+.09f-bounds.min.y,.06f,.18f);var before=tent.transform.position;Undo.RecordObject(tent.transform,"Raise canvas clear of terrain");tent.transform.position+=Vector3.up*lift;tents.Add(new TentPlacement{name=tent.name,before=before,after=tent.transform.position});
            }
            var mud=Mat("ColdPeatMud","StoryTell/MudSurface",new Color(.9f,.95f,1));var importer=(TextureImporter)AssetImporter.GetAtPath(Data+"/ColdPeatMud.png");importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.SaveAndReimport();
            mud.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Data+"/ColdPeatMud.png"));mud.SetFloat("_Smoothness",.24f);mud.SetFloat("_Wetness",.7f);mud.SetFloat("_MudAmount",0);mud.SetFloat("_Saturation",.7f);mud.SetColor("_ColdTint",new Color(.85f,.93f,.95f));
            string normalPath=Data+"/ColdPeatMudNormal.png";File.Copy(Data+"/ColdPeatMud.png",normalPath);AssetDatabase.ImportAsset(normalPath);var ni=(TextureImporter)AssetImporter.GetAtPath(normalPath);ni.textureType=TextureImporterType.NormalMap;ni.convertToNormalmap=true;ni.heightmapScale=.025f;ni.wrapMode=TextureWrapMode.Repeat;ni.SaveAndReimport();mud.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mud.SetFloat("_BumpScale",.45f);
            var vertices=new List<Vector3>();var uvs=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
            void Patch(Vector2 center,Vector2 size)
            {
                const int steps=10;int start=vertices.Count;
                for(int z=0;z<=steps;z++)for(int x=0;x<=steps;x++){float u=x/(float)steps,w=z/(float)steps;float px=center.x+(u-.5f)*size.x,pz=center.y+(w-.5f)*size.y;vertices.Add(new Vector3(px,Ground(px,pz)+.025f,pz));uvs.Add(new Vector2(px/3.5f,pz/3.5f));float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(w,1-w));float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge*5-Mathf.PerlinNoise(px*.7f,pz*.7f)*.15f));colors.Add(new Color(1,1,1,fade));if(x<steps&&z<steps){int a=start+z*(steps+1)+x;indices.AddRange(new[]{a,a+steps+1,a+1,a+1,a+steps+1,a+steps+2});}}
            }
            foreach(var p in new[]{new Vector2(85,279),new Vector2(92,267),new Vector2(76,282),new Vector2(64,278),new Vector2(69,292),new Vector2(67,266),new Vector2(90,297),new Vector2(100,320),new Vector2(109,339),new Vector2(92,352),new Vector2(45,364),new Vector2(62,390),new Vector2(72,404),new Vector2(65,435),new Vector2(40,466),new Vector2(39,496),new Vector2(56,514)})Patch(p,new Vector2(7,11));
            var groundMesh=new Mesh{name="Terrain following damp paths"};groundMesh.SetVertices(vertices);groundMesh.SetUVs(0,uvs);groundMesh.SetColors(colors);groundMesh.SetTriangles(indices,0);groundMesh.RecalculateNormals();groundMesh.RecalculateTangents();groundMesh.RecalculateBounds();AssetDatabase.CreateAsset(groundMesh,Data+"/MudPaths.asset");var mudGo=New("Mud_VisualPaths",root.transform);mudGo.AddComponent<MeshFilter>().sharedMesh=groundMesh;var mr=mudGo.AddComponent<MeshRenderer>();mr.sharedMaterial=mud;mr.shadowCastingMode=ShadowCastingMode.Off;
            var wood=Mat("CharredLogs","Universal Render Pipeline/Lit",new Color(.075f,.055f,.035f));var ember=Mat("EmberBed","Universal Render Pipeline/Lit",new Color(.18f,.055f,.012f));ember.EnableKeyword("_EMISSION");ember.SetColor("_EmissionColor",new Color(.3f,.05f,.005f));var smokeMat=Mat("FireSmoke","StoryTell/WeatherParticle",new Color(.32f,.34f,.35f,.25f));var fires=All<Light>(scene).Where(l=>l.name=="WarmLandmarkLight").ToArray();
            foreach(var fire in fires)
            {
                var camp=fire.transform.parent;Undo.RecordObject(fire,"Warm moving fire light");fire.intensity=2.4f;fire.range=6.5f;var flicker=Undo.AddComponent<CampfireMotion>(camp.gameObject);Set(flicker,"glow",fire);
                for(int i=0;i<3;i++){var log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Undo.RegisterCreatedObjectUndo(log,"Firewood");log.name="CharredLog";log.transform.SetParent(camp,false);log.transform.localPosition=new Vector3((i-1)*.08f,.12f,0);log.transform.localRotation=Quaternion.Euler(90,i*60,0);log.transform.localScale=new Vector3(.1f,.4f,.1f);log.GetComponent<Renderer>().sharedMaterial=wood;log.GetComponent<Collider>().enabled=false;}
                var bed=GameObject.CreatePrimitive(PrimitiveType.Sphere);Undo.RegisterCreatedObjectUndo(bed,"Ember bed");bed.name="EmberBed";bed.transform.SetParent(camp,false);bed.transform.localPosition=Vector3.up*.06f;bed.transform.localScale=new Vector3(.65f,.05f,.65f);bed.GetComponent<Renderer>().sharedMaterial=ember;bed.GetComponent<Collider>().enabled=false;
                var smoke=New("WindCarriedSmoke",camp);smoke.transform.localPosition=Vector3.up*.35f;smoke.transform.localRotation=Quaternion.Euler(-90,0,0);var ps=smoke.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.startLifetime=3;main.startSpeed=.55f;main.startSize=.35f;main.maxParticles=24;main.startColor=new Color(.65f,.65f,.65f,.35f);main.simulationSpace=ParticleSystemSimulationSpace.World;var em=ps.emission;em.rateOverTime=5;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.17f;var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.35f;velocity.y=.4f;velocity.z=.16f;var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.7f));var opacity=ps.colorOverLifetime;opacity.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.2f,0),new GradientAlphaKey(.55f,.2f),new GradientAlphaKey(0,1)});opacity.color=gradient;smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=smokeMat;
            }
            foreach(var material in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Systems/MainIntegration/Data/ColdStyle"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))))
            {if(material.HasProperty("_Wetness")){Undo.RecordObject(material,"Subtle damp surface variation");material.SetFloat("_Wetness",Mathf.Min(.45f,material.GetFloat("_Wetness")+.06f));material.SetFloat("_DetailStrength",material.name.Contains("Uniform")?.015f:material.name.Contains("Stone")?.1f:.065f);EditorUtility.SetDirty(material);}}
            File.WriteAllText(Data+"/PlacementReport.json",JsonUtility.ToJson(new Report{tents=tents.ToArray(),fires=fires.Length,mudVertices=vertices.Count},true));
            foreach(var c in All<Component>(scene))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ColdStyleAuthoring.CaptureViews(scene,"weather");Undo.CollapseUndoOperations(group);
            BuildMenus();
        }
        static void BuildMenus()
        {
            File.WriteAllText(Menus+"/RuntimeTheme.tss","@import url(\"unity-theme://default\");");AssetDatabase.Refresh();var panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1920,1080);panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=.5f;panel.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Menus+"/RuntimeTheme.tss");AssetDatabase.CreateAsset(panel,Data+"/MenuPanel.asset");
            var profile=ScriptableObject.CreateInstance<SceneTransitionProfile>();var data=new SerializedObject(profile);data.FindProperty("message").stringValue="";data.FindProperty("font").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");foreach(string f in new[]{"overlayFadeInDuration","overlayFadeOutDuration"})data.FindProperty(f).floatValue=.25f;foreach(string f in new[]{"textFadeInDuration","textFadeOutDuration","holdDuration"})data.FindProperty(f).floatValue=0;data.ApplyModifiedProperties();AssetDatabase.CreateAsset(profile,Data+"/MenuTransition.asset");
            foreach(string name in new[]{"MainMenu","GameSelect"})
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("MenuCamera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=Color.black;camera.GetComponent<Camera>().cullingMask=0;
                ConfigureInput(new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>());var go=new GameObject("StoryTell_"+name,typeof(UIDocument));var doc=go.GetComponent<UIDocument>();doc.panelSettings=AssetDatabase.LoadAssetAtPath<PanelSettings>(Data+"/MenuPanel.asset");doc.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Menus+"/"+name+".uxml");var controller=go.AddComponent<MenuScreenController>();Set(controller,"theme",AssetDatabase.LoadAssetAtPath<StyleSheet>(Menus+"/Theme.uss"));Set(controller,"transition",AssetDatabase.LoadAssetAtPath<SceneTransitionProfile>(Data+"/MenuTransition.asset"));var so=new SerializedObject(controller);so.FindProperty("campaignSelection").boolValue=name=="GameSelect";so.ApplyModifiedProperties();EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+name+".unity");
            }
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path=="Assets/Scenes/GameSelect.unity"))scenes.Insert(1,new EditorBuildSettingsScene("Assets/Scenes/GameSelect.unity",true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        }
        public static void FinishMenus()
        {
            var level=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            foreach(var flame in All<ParticleSystem>(level).Where(p=>p.name=="SmallFlames")){Undo.RecordObject(flame,"Shaped flame licks");var main=flame.main;main.startSize=.17f;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.7f);var size=flame.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.25f),new Keyframe(.2f,1),new Keyframe(1,0)));var renderer=flame.GetComponent<ParticleSystemRenderer>();Undo.RecordObject(renderer,"Stretched flames");renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.7f;renderer.velocityScale=.15f;}
            EditorSceneManager.MarkSceneDirty(level);EditorSceneManager.SaveScene(level);
            foreach(string name in new[]{"MainMenu","GameSelect"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");foreach(var module in All<InputSystemUIInputModule>(scene))ConfigureInput(module);
                foreach(var doc in All<UIDocument>(scene))doc.panelSettings=AssetDatabase.LoadAssetAtPath<PanelSettings>(Data+"/MenuPanel.asset");
                foreach(var controller in All<MenuScreenController>(scene))Set(controller,"transition",AssetDatabase.LoadAssetAtPath<SceneTransitionProfile>(Data+"/MenuTransition.asset"));
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }AssetDatabase.SaveAssets();
        }
        static void ConfigureInput(InputSystemUIInputModule module)
        {
            var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Systems/FirstPersonControllerSystem/Scripts/Input/InputSystem_Actions.inputactions");module.actionsAsset=asset;
            InputActionReference Ref(string name){string path=Data+"/UI_"+name+".asset";var value=AssetDatabase.LoadAssetAtPath<InputActionReference>(path);if(!value){value=InputActionReference.Create(asset.FindAction("UI/"+name,true));AssetDatabase.CreateAsset(value,path);}return value;}
            module.point=Ref("Point");module.move=Ref("Navigate");module.submit=Ref("Submit");module.cancel=Ref("Cancel");module.leftClick=Ref("Click");module.rightClick=Ref("RightClick");module.middleClick=Ref("MiddleClick");module.scrollWheel=Ref("ScrollWheel");
        }
        public static void CaptureDetails()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            foreach(var ps in All<ParticleSystem>(scene))if(ps.transform.GetComponentInParent<CampfireMotion>())ps.Simulate(2,true,true);
            var fire=All<CampfireMotion>(scene).OrderBy(f=>Vector3.Distance(f.transform.position,new Vector3(85,28,279))).First();
            var terrain=All<Terrain>(scene).Single();var ground=new Vector3(85,terrain.SampleHeight(new Vector3(85,0,279))+terrain.transform.position.y,279);
            CaptureDetail(fire.transform.position+new Vector3(1.6f,1.2f,-2),fire.transform.position+Vector3.up*.3f,"fire");
            CaptureDetail(ground+new Vector3(2,3,-3),ground,"mud");
        }
        public static void RefineFireVisibility()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"/VisibleFlames.mat");if(!material){material=new Material(Shader.Find("StoryTell/WeatherFlame"));AssetDatabase.CreateAsset(material,Data+"/VisibleFlames.mat");}
            foreach(var flame in All<ParticleSystem>(scene).Where(p=>p.name=="SmallFlames"))
            {
                Undo.RecordObject(flame,"Visible warm flame silhouettes");flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=flame.main;main.loop=true;main.playOnAwake=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.7f);main.startSpeed=.4f;main.startColor=Color.white;main.startSize3D=true;main.startSizeX=.24f;main.startSizeY=.55f;main.startSizeZ=.24f;main.maxParticles=32;var emission=flame.emission;emission.enabled=true;emission.rateOverTime=28;var shape=flame.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=8;shape.radius=.15f;
                var renderer=flame.GetComponent<ParticleSystemRenderer>();Undo.RecordObject(renderer,"Flame material");renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sharedMaterial=material;
                Undo.RecordObject(flame.transform,"Raise flame base above embers");flame.transform.localPosition=Vector3.up*.18f;
            }
            var logs=AssetDatabase.LoadAssetAtPath<Material>(Data+"/CharredLogs.mat");logs.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(logs);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void CaptureDetail(Vector3 position,Vector3 target,string label)
        {
            var go=new GameObject("Temporary weather preview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=58;camera.nearClipPlane=.05f;camera.farClipPlane=1500;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/WeatherMenu-"+label+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        }
    }
}
#endif
