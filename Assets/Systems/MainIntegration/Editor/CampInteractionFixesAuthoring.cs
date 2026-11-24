#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using StoryTell.MainIntegration;
using Systems.InteractionSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using InteractionSystem.Scripts.Utils;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;

public static class CampInteractionFixesAuthoring
{
    const string ScenePath="Assets/Scenes/Level1.unity";
    public const string Data="Assets/Systems/MainIntegration/Data/CampInteractionFixes";
    static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
        var s=EditorSceneManager.OpenScene(ScenePath);if(All<Transform>(s).Any(t=>t.name=="Coastal grass continuation"))throw new InvalidOperationException("Already applied");
        Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fix camp inputs, kitchen focus and landscape continuity");
        int inputs=0;
        foreach(var item in All<InteractableScript>(s).Where(i=>i.UIInteraction!=null&&!i.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()&&i.InspectInteractable))
        {Undo.RecordObject(item,"Use E consistently for world objects");item.UIInteraction.interactionType=EInteractions.EKeypress;EditorUtility.SetDirty(item);inputs++;}
        var table=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");var scenic=new SerializedObject(All<ScenicFocus>(s).Single());var views=scenic.FindProperty("views");
        for(int i=0;i<views.arraySize;i++)
        {
            var v=views.GetArrayElementAtIndex(i);string caption=v.FindPropertyRelative("caption").stringValue.ToLowerInvariant();if(!caption.Contains("cocina")&&!caption.Contains("comedor"))continue;
            var target=(Transform)v.FindPropertyRelative("target").objectReferenceValue;var entrance=(Transform)v.FindPropertyRelative("entrance").objectReferenceValue;
            Undo.RecordObject(target,"Focus on the current dining area");target.position=table.bounds.center+Vector3.up*.5f;Undo.SetTransformParent(target,table.transform,"Keep kitchen focus attached to its table");
            Undo.RecordObject(entrance,"Show kitchen invitation on its approach");var t=All<Terrain>(s).Single();var p=table.bounds.center+Vector3.forward*10;entrance.position=new Vector3(p.x,t.SampleHeight(p)+t.transform.position.y,p.z);v.FindPropertyRelative("radius").floatValue=7;v.FindPropertyRelative("caption").stringValue="El comedor del campamento";
        }
        scenic.ApplyModifiedProperties();
        // The reticle is the small centered Image named Dot in the native interaction canvas.
        int dots=0;foreach(var dot in All<Graphic>(s).Where(i=>i.name=="Dot"&&i.rectTransform.sizeDelta.x<=20&&i.rectTransform.sizeDelta.y<=20)){Undo.RecordObject(dot,"Hide the central aiming dot");Undo.RecordObject(dot.gameObject,"Hide the central aiming dot");dot.enabled=false;dot.raycastTarget=false;dot.gameObject.SetActive(false);if(PrefabUtility.IsPartOfPrefabInstance(dot)){PrefabUtility.RecordPrefabInstancePropertyModifications(dot);PrefabUtility.RecordPrefabInstancePropertyModifications(dot.gameObject);}dots++;}
        RefineCoast(s);
        foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
        File.WriteAllText("Library/CampInteractionFixes-applied.txt","World object inputs standardized: "+inputs+"\nCentral dots hidden: "+dots+"\nCoastal sampling, signed texture scale and grass continuation applied.");
    }
    static void RefineCoast(Scene s)
    {
        var terrain=All<Terrain>(s).Single();var data=terrain.terrainData;var size=data.size;var origin=terrain.transform.position;var skirt=All<MeshFilter>(s).Single(f=>f.name=="HorizonTerrainSkirt");var layer=data.terrainLayers.First();
        var corners=new[]{Vector3.zero,new Vector3(size.x,0,0),new Vector3(size.x,0,size.z),new Vector3(0,0,size.z)};float[] bands={0,20,55,110,200,330,500,850};int samples=data.heightmapResolution-1;
        var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        float Tile(float value)=>Mathf.Abs(value)<.001f?1:value;
        Vector2 UV(Vector3 world)=>new Vector2((world.x-origin.x+layer.tileOffset.x)/Tile(layer.tileSize.x),(world.z-origin.z+layer.tileOffset.y)/Tile(layer.tileSize.y));
        float Height(float edge,float d,float x,float z)=>Mathf.Lerp(edge,-1.5f,Mathf.SmoothStep(0,1,Mathf.Clamp01((d-45)/500)))+(Mathf.PerlinNoise(x*.007f,z*.007f)-.5f)*3*Mathf.Sin(Mathf.PI*Mathf.Clamp01(d/545));
        for(int side=0;side<4;side++)
        {
            int offset=vertices.Count;for(int row=0;row<bands.Length;row++)for(int i=0;i<=samples;i++)
            {float t=i/(float)samples,d=bands[row];var a=Vector3.Lerp(corners[side],corners[(side+1)%4],t);var direction=(corners[(side+1)%4]-corners[side]).normalized;var outward=new Vector3(direction.z,0,-direction.x);var world=origin+a+outward*d+direction*(d*(2*t-1));world.y=Height(terrain.SampleHeight(origin+a)+origin.y,d,world.x,world.z);vertices.Add(skirt.transform.InverseTransformPoint(world));uv.Add(UV(world));}
            for(int row=0;row<bands.Length-1;row++)for(int i=0;i<samples;i++){int a=offset+row*(samples+1)+i,b=a+1,c=a+samples+1,e=c+1;triangles.AddRange(new[]{a,b,c,b,e,c});}
        }
        var mesh=new Mesh{name="Coast matched to terrain boundary",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/ContinuousCoast.asset");Undo.RecordObject(skirt,"Sample each terrain boundary vertex");skirt.sharedMesh=mesh;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Coast aligned with terrain texture"};material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",layer.smoothness);AssetDatabase.CreateAsset(material,Data+"/ContinuousCoast.mat");var renderer=skirt.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Match ground texture and tiling");renderer.sharedMaterial=material;
        var grassRoot=new GameObject("Coastal grass continuation");Undo.RegisterCreatedObjectUndo(grassRoot,"Grass along the extended coast");grassRoot.transform.SetParent(skirt.transform.parent,false);
        // Scatter the existing optimized grasses on the visible western extension only.
        // Combine into a few meshes rather than thousands of scene objects.
        var random=new System.Random(7319);int total=0;
        var surfaceObject=new GameObject("Temporary coastal surface sampler");surfaceObject.transform.SetParent(skirt.transform,false);var surface=surfaceObject.AddComponent<MeshCollider>();surface.sharedMesh=mesh;Physics.SyncTransforms();
        foreach(var prototype in data.detailPrototypes.Where(p=>p.prototype&&p.prototype.name.Contains("Optimized")))
        {
            var sources=prototype.prototype.GetComponentsInChildren<MeshFilter>(true);if(sources.Length==0)continue;var source=sources[0];var sourceRenderer=source.GetComponent<MeshRenderer>();if(!source.sharedMesh||!sourceRenderer||source.sharedMesh.subMeshCount!=1)continue;
            var patches=new System.Collections.Generic.List<CombineInstance>();
            for(float z=170;z<680;z+=5)for(float d=2;d<82;d+=4)
            {
                if(random.NextDouble()>.62*(1-d/100f))continue;float x=origin.x-d-(float)random.NextDouble()*2,wz=z+origin.z+(float)random.NextDouble()*4;
                if(!surface.Raycast(new Ray(new Vector3(x,200,wz),Vector3.down),out var hit,400))continue;float h=hit.point.y;
                float width=Mathf.Lerp(prototype.minWidth,prototype.maxWidth,(float)random.NextDouble()),height=Mathf.Lerp(prototype.minHeight,prototype.maxHeight,(float)random.NextDouble());
                var relative=prototype.prototype.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
                patches.Add(new CombineInstance{mesh=source.sharedMesh,transform=grassRoot.transform.worldToLocalMatrix*Matrix4x4.TRS(new Vector3(x,h-.03f,wz),Quaternion.Euler(0,(float)random.NextDouble()*360,0),new Vector3(width,height,width))*relative});
            }
            if(patches.Count==0)continue;var grass=new Mesh{name=prototype.prototype.name+" coastal patches",indexFormat=IndexFormat.UInt32};grass.CombineMeshes(patches.ToArray(),true,true);grass.RecalculateBounds();AssetDatabase.CreateAsset(grass,Data+"/"+prototype.prototype.name+".asset");var go=new GameObject(prototype.prototype.name,typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Combined coastal grasses");go.transform.SetParent(grassRoot.transform,false);go.GetComponent<MeshFilter>().sharedMesh=grass;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=sourceRenderer.sharedMaterials;r.shadowCastingMode=ShadowCastingMode.Off;total+=patches.Count;
        }
        UnityEngine.Object.DestroyImmediate(surfaceObject);
        File.WriteAllText("Library/CampInteractionFixes-coast.txt","Boundary samples per side: "+samples+"\nSigned texture tile: "+layer.tileSize+"\nCombined grass clusters: "+total);
    }
    public static void RefineGrassMaterials()
    {
        var s=EditorSceneManager.OpenScene(ScenePath);var shader=Shader.Find("StoryTell/Optimized Terrain Grass");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Coastal grass shader failed");
        foreach(var renderer in All<MeshRenderer>(s).Where(r=>r.transform.parent&&r.transform.parent.name=="Coastal grass continuation"))
        {
            string path=Data+"/"+renderer.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(material,path);}Undo.RecordObject(material,"Keep the coastal grass palette cold");material.shader=shader;material.SetColor("_Color",new Color(.26f,.28f,.26f));material.SetColor("_BottomColor",new Color(.18f,.20f,.17f));material.SetFloat("_TerrainBlend",.30f);material.SetFloat("_LightColorInfluence",.08f);material.SetFloat("_FadeStart",100);material.SetFloat("_FadeEnd",170);EditorUtility.SetDirty(material);Undo.RecordObject(renderer,"Use matching grass materials");renderer.sharedMaterial=material;
        }
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
    }
    public static void Survey()
    {
        var s=EditorSceneManager.OpenScene(ScenePath);var b=new StringBuilder();
        foreach(var item in All<InteractableScript>(s).Where(i=>!i.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()))
        {var renderers=item.GetComponentsInChildren<Renderer>(true);b.AppendLine("ITEM "+PathOf(item.transform)+" type="+item.GetType().Name+" input="+item.UIInteraction?.interactionType+" pos="+item.transform.position+" scale="+item.transform.lossyScale+" inspect="+item.InspectInteractable+" bounds="+(renderers.Length>0?renderers[0].bounds.ToString():"none"));}
        var inspector=All<InspectionScript>(s).Single();b.AppendLine("INSPECTION "+PathOf(inspector.inspectAnchor)+" local="+inspector.inspectAnchor.localPosition+" world="+inspector.inspectAnchor.position);
        var views=new SerializedObject(All<ScenicFocus>(s).Single()).FindProperty("views");
        for(int i=0;i<views.arraySize;i++){var v=views.GetArrayElementAtIndex(i);var entrance=(Transform)v.FindPropertyRelative("entrance").objectReferenceValue;var target=(Transform)v.FindPropertyRelative("target").objectReferenceValue;b.AppendLine("VIEW "+v.FindPropertyRelative("caption").stringValue+" entrance="+entrance.position+" target="+PathOf(target)+" "+target.position);}
        foreach(var image in All<Image>(s).Where(i=>i.name=="Cross"))b.AppendLine("CROSS "+PathOf(image.transform)+" size="+image.rectTransform.sizeDelta+" active="+image.gameObject.activeInHierarchy);
        foreach(var t in All<Terrain>(s)){var d=t.terrainData;b.AppendLine("TERRAIN "+t.name+" origin="+t.transform.position+" size="+d.size+" res="+d.heightmapResolution+" detail="+d.detailResolution+" material="+t.materialTemplate?.name);foreach(var l in d.terrainLayers)b.AppendLine("LAYER "+AssetDatabase.GetAssetPath(l)+" tile="+l.tileSize+" offset="+l.tileOffset+" tex="+AssetDatabase.GetAssetPath(l.diffuseTexture));foreach(var p in d.detailPrototypes)b.AppendLine("GRASS "+(p.prototypeTexture?p.prototypeTexture.name:p.prototype?.name)+" mode="+p.renderMode+" width="+p.minWidth+".."+p.maxWidth);}
        File.WriteAllText("Library/CampInteractionFixes-survey.txt",b.ToString());
    }
}
#endif
