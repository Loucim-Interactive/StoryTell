#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StoryTell.MainIntegration.Editor
{
    public static class ExplorationPolishAuthoring
    {
        public const string LightingBaseline="Assets/Systems/MainIntegration/Data/Exploration/ApprovedLighting.json";
        const string Data="Assets/Systems/MainIntegration/Data/Exploration";
        [Serializable] public sealed class LightingState
        {
            public bool fog;public int mode;public float density;
            public Color fogColor,sky,equator,ground;public LightState[] lights;
        }
        [Serializable] public sealed class LightState
        { public string path;public Color color;public float intensity,range,bias,normalBias;public int type,shadows; }
        static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        public static string CaptureLighting(Scene scene)=>JsonUtility.ToJson(new LightingState
        {
            fog=RenderSettings.fog,mode=(int)RenderSettings.fogMode,density=RenderSettings.fogDensity,fogColor=RenderSettings.fogColor,
            sky=RenderSettings.ambientSkyColor,equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,
            lights=All<Light>(scene).OrderBy(l=>PathOf(l.transform)).Select(l=>new LightState{path=PathOf(l.transform),color=l.color,intensity=l.intensity,range=l.range,bias=l.shadowBias,normalBias=l.shadowNormalBias,type=(int)l.type,shadows=(int)l.shadows}).ToArray()
        },true);
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            if(scene.GetRootGameObjects().Any(r=>r.name=="Exploration_AccessLandmarks"))throw new InvalidOperationException("Exploration pass already applied.");
            Directory.CreateDirectory("Library/LevelPolishBackups");File.Copy("Assets/Scenes/Level1.unity","Library/LevelPolishBackups/BeforeExploration-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            Directory.CreateDirectory(Data);File.WriteAllText(LightingBaseline,CaptureLighting(scene));AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Broaden exploration and make critical approaches visible");
            var route=All<RouteBoundary>(scene).Single();var settings=new SerializedObject(route);var sections=settings.FindProperty("sections");
            float[] widths={65,30,35,55,55,36,38,42,40,42,36};
            if(sections.arraySize!=widths.Length)throw new InvalidOperationException("Unexpected route layout.");
            for(int i=0;i<widths.Length;i++)sections.GetArrayElementAtIndex(i).FindPropertyRelative("radius").floatValue=widths[i];
            settings.FindProperty("minimumWorldXZ").vector2Value=new Vector2(8,205);settings.FindProperty("maximumWorldXZ").vector2Value=new Vector2(170,610);
            settings.FindProperty("entrances").arraySize=0;settings.ApplyModifiedProperties();
            var sky=RenderSettings.skybox;Undo.RecordObject(sky,"Detailed cloud texture without changing the lighting");sky.SetFloat("_CloudScale",3.8f);sky.SetColor("_CloudLight",new Color(.59f,.62f,.64f));sky.SetColor("_CloudShadow",new Color(.31f,.35f,.39f));EditorUtility.SetDirty(sky);
            RebuildPerimeter(route,scene);
            var root=new GameObject("Exploration_AccessLandmarks");Undo.RegisterCreatedObjectUndo(root,"Visible approaches");
            var terrain=All<Terrain>(scene).Single();
            // Fortifications close only the two critical transitions. The surrounding
            // camp and outpost remain broad spaces instead of a prescribed corridor.
            Row(8,64,398,root.transform,terrain);Row(80,136,398,root.transform,terrain);
            Row(8,44,519,root.transform,terrain);Row(68,112,519,root.transform,terrain);
            var gate=(GameObject)settings.FindProperty("frontBarrier").objectReferenceValue;var collider=gate.GetComponent<BoxCollider>();
            Undo.RecordObject(gate.transform,"Align mission gate with visible opening");gate.transform.position=Ground(terrain,72,398)+Vector3.up*1.2f;
            Undo.RecordObject(collider,"Gate only the access opening");collider.size=new Vector3(16,2.4f,1);collider.center=Vector3.zero;
            Row(64,80,398,gate.transform,terrain);
            foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            if(CaptureLighting(scene)!=File.ReadAllText(LightingBaseline))throw new InvalidOperationException("Approved lighting changed.");
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static Vector3 Ground(Terrain terrain,float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
        static void Row(float start,float end,float z,Transform parent,Terrain terrain)
        {
            const string prefab="Assets/Art/Outsourced/Props/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab";
            float cursor=start;int count=0;
            while(cursor<end-.05f)
            {
                if(++count>80)throw new InvalidOperationException("Unexpectedly small barricade mesh.");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),parent);Undo.RegisterCreatedObjectUndo(instance,"Place visible boundary");instance.name="Access_SandbagWall";
                var renderers=instance.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                float scale=3.6f/bounds.size.x;instance.transform.localScale*=scale;
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                // Fit the last segment to preserve the full entrance width.
                if(cursor+bounds.size.x>end){var size=instance.transform.localScale;size.x*=(end-cursor)/bounds.size.x;instance.transform.localScale=size;bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);}
                float width=bounds.size.x;var ground=Ground(terrain,cursor+width*.5f,z);
                instance.transform.position+=ground-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                foreach(var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh&&!filter.GetComponent<Collider>()){var mesh=Undo.AddComponent<MeshCollider>(filter.gameObject);mesh.sharedMesh=filter.sharedMesh;}
                cursor+=width*.97f;
            }
        }
        static void RebuildPerimeter(RouteBoundary route,Scene scene)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var terrain=All<Terrain>(scene).Single();const float cell=2;
            for(float x=8;x<=170;x+=cell)for(float z=204;z<=612;z+=cell)
            {
                if(!route.Contains(new Vector3(x,0,z)))continue;
                for(int side=0;side<4;side++)
                {
                    Vector2 direction=side==0?Vector2.left:side==1?Vector2.right:side==2?Vector2.down:Vector2.up;
                    if(route.Contains(new Vector3(x+direction.x*cell,0,z+direction.y*cell)))continue;
                    var middle=new Vector3(x+direction.x*cell*.5f,0,z+direction.y*cell*.5f);var tangent=new Vector3(-direction.y,0,direction.x)*cell*.5f;
                    var a=middle-tangent;var b=middle+tangent;a.y=Ground(terrain,a.x,a.z).y-2;b.y=Ground(terrain,b.x,b.z).y-2;int n=vertices.Count;
                    vertices.AddRange(new[]{a,b,b+Vector3.up*14,a+Vector3.up*14});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});
                }
            }
            var mesh=new Mesh{name="DistantExplorationPerimeter"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/WidePerimeter.asset");
            var wall=All<MeshCollider>(scene).Single(c=>c.name=="Route_OuterInvisibleWall");Undo.RecordObject(wall,"Move the outer limit away from exploration");wall.sharedMesh=mesh;
        }
    }
}
#endif
