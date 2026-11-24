#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MapFrontCratersAuthoring
{
    public const string Data="Assets/Systems/MainIntegration/Data/MapFrontCraters";
    public static readonly Vector4[] Craters={new(27,535,4.8f,.9f),new(94,546,5.6f,1.1f),new(35,560,4.5f,.9f),new(104,578,6.2f,1.2f),new(70,609,5.2f,1)};
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static Terrain terrain;
    static Vector3 Ground(float x,float z)=>new(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
    public static void SurveyRoadside()
    {
        var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var report=new System.Text.StringBuilder();terrain=All<Terrain>(s).Single();
        foreach(var r in All<MeshRenderer>(s).Where(r=>r.name.StartsWith("Collapsed shelter")||r.name.StartsWith("Refugio dañado")))report.AppendLine(r.name+" "+r.bounds);
        foreach(var actor in All<DialogueSystem.Scripts.SpeakerScript>(s).Where(a=>a.transform.position.z>380))report.AppendLine("NPC "+actor.name+" "+actor.transform.position);
        foreach(var c in All<Collider>(s).Where(c=>c.enabled&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.center.z>390&&c.bounds.center.z<500&&c.bounds.size.magnitude<30))report.AppendLine("SOLID "+c.name+" "+c.bounds);
        File.WriteAllText("Library/RoadsideCraters-survey.txt",report.ToString());
    }
    public const string RoadsideData="Assets/Systems/MainIntegration/Data/RoadsideCraters";
    public static readonly Vector4[] RoadsideImpacts={new(52,405,3.8f,.8f),new(94,449,3.8f,.8f),new(34,465,3.8f,.8f),new(40,490,3.5f,.75f)};
    public static void AddRoadsideCraters()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
        var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<Transform>(s).Any(t=>t.name=="Roadside impact craters"))throw new InvalidOperationException("Already applied");
        terrain=All<Terrain>(s).Single();var original=terrain.terrainData;
        var shelters=All<MeshRenderer>(s).Where(r=>r.name.StartsWith("Collapsed shelter")||r.name.StartsWith("Refugio dañado")).ToArray();
        foreach(var impact in RoadsideImpacts)foreach(var shelter in shelters)
        {
            var p=new Vector3(impact.x,shelter.bounds.center.y,impact.y);if(Vector3.Distance(shelter.bounds.ClosestPoint(p),p)<impact.z*1.65f+.4f)throw new InvalidOperationException("Impact intersects shelter: "+shelter.name);
        }
        Directory.CreateDirectory(RoadsideData);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Roadside shell craters near damaged shelters");
        var copy=UnityEngine.Object.Instantiate(original);copy.name="Level1 with front and roadside craters";AssetDatabase.CreateAsset(copy,RoadsideData+"/RoadsideTerrain.asset");var collider=terrain.GetComponent<TerrainCollider>();Undo.RecordObject(terrain,"Extend local crater terrain");Undo.RecordObject(collider,"Match roadside crater collision");terrain.terrainData=copy;collider.terrainData=copy;
        Sculpt(copy,RoadsideImpacts);ClearCraterGrass(copy,RoadsideImpacts);terrain.Flush();Physics.SyncTransforms();
        var root=new GameObject("Roadside impact craters");Undo.RegisterCreatedObjectUndo(root,"Impacts near the damaged shelters");var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"/DisturbedPeat.mat");if(!material)throw new InvalidOperationException("Existing crater material missing");
        for(int i=0;i<RoadsideImpacts.Length;i++){BuildSurface(RoadsideImpacts[i],i,root.transform,material,collider,RoadsideData);root.transform.GetChild(i).name="Cráter junto al camino "+(i+1);}
        CreateSurvey(s,RoadsideData);
        foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
        File.WriteAllText("Library/RoadsideCraters-applied.txt","Four shallow impacts alongside the four damaged shelters.\nExisting front craters and combat navigation retained. Roadside impacts end before the combat navigation bounds at Z=500.\nPrevious terrain preserved: "+AssetDatabase.GetAssetPath(original));
    }
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
        var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<Transform>(s).Any(t=>t.name=="Front impact craters"))throw new InvalidOperationException("Already applied");
        var circle=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");if(!circle)throw new InvalidOperationException("Missing native round marker sprite");
        Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Clean map symbols and front impact craters");
        terrain=All<Terrain>(s).Single();var original=terrain.terrainData;var copy=UnityEngine.Object.Instantiate(original);copy.name="Level1 with local impact craters";AssetDatabase.CreateAsset(copy,Data+"/FrontTerrain.asset");
        var collider=terrain.GetComponent<TerrainCollider>();Undo.RecordObject(terrain,"Use isolated crater terrain data");Undo.RecordObject(collider,"Match the crater collision surface");terrain.terrainData=copy;collider.terrainData=copy;
        Sculpt(copy);ClearCraterGrass(copy);terrain.Flush();Physics.SyncTransforms();
        var root=new GameObject("Front impact craters");Undo.RegisterCreatedObjectUndo(root,"Front impact dressing");
        var material=new Material(Shader.Find("StoryTell/MudSurface")){name="Cold disturbed peat"};material.SetColor("_BaseColor",new Color(.23f,.19f,.14f));material.SetFloat("_Saturation",.35f);material.SetFloat("_Smoothness",.2f);material.SetFloat("_Wetness",.5f);material.SetFloat("_MudAmount",.25f);material.SetFloat("_MudHeight",100);AssetDatabase.CreateAsset(material,Data+"/DisturbedPeat.mat");
        for(int i=0;i<Craters.Length;i++)BuildSurface(Craters[i],i,root.transform,material,collider);
        foreach(var zone in All<MapZoneOverlay>(s).Single().Zones){var image=zone.marker.GetComponent<Image>();Undo.RecordObject(image,"Readable round destination markers");image.sprite=circle;image.type=Image.Type.Simple;image.color=new Color(.40f,.33f,.21f);image.preserveAspect=true;}
        CreateSurvey(s);BakeNavigation(s);
        foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
        File.WriteAllText("Library/MapFrontCraters-applied.txt","Five shallow terrain craters on a cloned TerrainData; matching collision and rebuilt enemy navigation.\nClean contour map without baked tent squares; round numbered destinations retained.\nOriginal terrain data preserved: "+AssetDatabase.GetAssetPath(original));
    }
    static void Sculpt(TerrainData data,Vector4[] impacts=null)
    {
        int res=data.heightmapResolution;var origin=terrain.transform.position;float cell=data.size.x/(res-1);
        foreach(var c in impacts??Craters)
        {
            float reach=c.z*1.65f;int x0=Mathf.FloorToInt((c.x-reach-origin.x)/cell),z0=Mathf.FloorToInt((c.y-reach-origin.z)/cell);int count=Mathf.CeilToInt(reach*2/cell)+3;var heights=data.GetHeights(x0,z0,count,count);
            for(int z=0;z<count;z++)for(int x=0;x<count;x++)
            {
                float dx=origin.x+(x0+x)*cell-c.x,dz=origin.z+(z0+z)*cell-c.y;float angle=Mathf.Atan2(dz,dx);float radius=c.z*(1+.08f*Mathf.Sin(3*angle)+.05f*Mathf.Cos(5*angle));float r=Mathf.Sqrt(dx*dx+dz*dz)/radius;if(r>=1.45f)continue;
                float bowl=r<1?-c.w*Mathf.Pow(1-r*r,1.5f):0;float rim=.19f*Mathf.Exp(-Mathf.Pow((r-1.03f)/.20f,2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.45f,1.2f,r));heights[z,x]+=(bowl+rim)/data.size.y;
            }
            data.SetHeights(x0,z0,heights);
        }
        EditorUtility.SetDirty(data);
    }
    static void ClearCraterGrass(TerrainData data,Vector4[] impacts=null)
    {
        for(int layer=0;layer<data.detailPrototypes.Length;layer++)
        {
            var p=data.detailPrototypes[layer];if(!p.prototype||!p.prototype.name.ToLowerInvariant().Contains("grass"))continue;var values=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);
            for(int z=0;z<data.detailHeight;z++)for(int x=0;x<data.detailWidth;x++)
            {
                var point=new Vector2(terrain.transform.position.x+(x+.5f)/data.detailWidth*data.size.x,terrain.transform.position.z+(z+.5f)/data.detailHeight*data.size.z);
                foreach(var c in impacts??Craters){float r=Vector2.Distance(point,new Vector2(c.x,c.y))/c.z;if(r<1.6f)values[z,x]=Mathf.RoundToInt(values[z,x]*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1.6f,r)));}
            }
            data.SetDetailLayer(0,0,layer,values);
        }
    }
    static void BuildSurface(Vector4 crater,int index,Transform parent,Material material,TerrainCollider ground,string assetFolder=null)
    {
        const int segments=64,rings=18;var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int i=0;i<=segments;i++)
        {
            float a=i*Mathf.PI*2/segments,r=ring/(float)rings;float radius=crater.z*1.4f*r*(1+.08f*Mathf.Sin(3*a)+.05f*Mathf.Cos(5*a));float x=crater.x+Mathf.Cos(a)*radius,z=crater.y+Mathf.Sin(a)*radius;var point=Ground(x,z);if(ground.Raycast(new Ray(new Vector3(x,200,z),Vector3.down),out var hit,400))point=hit.point;point.y+=.025f;vertices.Add(point);uv.Add(new Vector2(x*.3f,z*.3f));colors.Add(new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,r))));
        }
        for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++){int a=ring*(segments+1)+i,b=a+1,c=a+segments+1,d=c+1;triangles.AddRange(new[]{a,b,c,b,d,c});}
        var mesh=new Mesh{name="Impact crater peat "+index};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,(assetFolder??Data)+"/Crater"+index+".asset");var go=new GameObject("Cráter de impacto "+(index+1),typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Irregular disturbed soil");go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
 static void CreateSurvey(Scene s,string assetFolder=null)
 {
  var reference=All<MapWorldReference>(s).Single();
  const int width=1536,height=1024;var values=new float[width*height];var colors=new Color32[values.Length];var origin=reference.WorldOrigin;for(int y=0;y<height;y++)for(int x=0;x<width;x++){var world=origin.position+origin.right*((x/(float)(width-1)-.5f)*450)+origin.forward*((y/(float)(height-1)-.5f)*300);values[y*width+x]=terrain.SampleHeight(world)+terrain.transform.position.y;}
  for(int y=0;y<height;y++)for(int x=0;x<width;x++){int index=y*width+x;float h=values[index];float dx=values[y*width+Mathf.Min(width-1,x+1)]-values[y*width+Mathf.Max(0,x-1)],dy=values[Mathf.Min(height-1,y+1)*width+x]-values[Mathf.Max(0,y-1)*width+x];float lineWidth=Mathf.Max(.035f,(Mathf.Abs(dx)+Mathf.Abs(dy))*.36f);bool contour=Mathf.Abs(h-Mathf.Round(h/2.5f)*2.5f)<lineWidth;float shade=Mathf.Clamp01(.5f+dx*.12f-dy*.16f);var paper=Color.Lerp(new Color(.67f,.65f,.49f),new Color(.84f,.82f,.66f),shade);float u=x/(float)(width-1),v=y/(float)(height-1);bool grid=Mathf.Min(Mathf.Abs(u*9-Mathf.Round(u*9)),Mathf.Abs(v*6-Mathf.Round(v*6)))<.008f;colors[index]=contour?new Color(.29f,.30f,.21f):grid?Color.Lerp(paper,new Color(.40f,.40f,.29f),.28f):paper;}
  void Pixel(int x,int y,Color color){if(x>=0&&x<width&&y>=0&&y<height)colors[y*width+x]=color;}
  void Line(int x0,int y0,int x1,int y1,int radius){int n=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));for(int i=0;i<=n;i++){int x=Mathf.RoundToInt(Mathf.Lerp(x0,x1,i/(float)Mathf.Max(1,n))),y=Mathf.RoundToInt(Mathf.Lerp(y0,y1,i/(float)Mathf.Max(1,n)));for(int a=-radius;a<=radius;a++)for(int b=-radius;b<=radius;b++)Pixel(x+a,y+b,new Color(.21f,.23f,.16f));}}
  Line(8,8,width-9,8,2);Line(8,height-9,width-9,height-9,2);Line(8,8,8,height-9,2);Line(width-9,8,width-9,height-9,2);Line(42,height-68,120,height-68,2);Line(120,height-68,101,height-53,2);Line(120,height-68,101,height-83,2);Line(43,height-43,43,height-23,1);Line(57,height-43,57,height-23,1);Line(43,height-23,57,height-43,1);
  var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.SetPixels32(colors);texture.Apply();string path=(assetFolder??Data)+"/CleanSurvey.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.SaveAndReimport();var renderer=reference.GetComponent<MeshRenderer>();var material=new Material(renderer.sharedMaterial){name="Readable sector paper"};material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);AssetDatabase.CreateAsset(material,(assetFolder??Data)+"/CleanPaper.mat");renderer.sharedMaterial=material;
 }

    static void BakeNavigation(Scene s)
    {
        var player=All<Systems.InteractionSystem.Scripts.InteractionScript>(s).Single().transform;var bounds=new Bounds(Ground(75,590),new Vector3(160,130,180));var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
        sources.RemoveAll(source=>source.component&&(source.component.transform.IsChildOf(player)||source.component.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()||PathOf(source.component.transform).Contains("/VFX/")||source.component.name.Contains("Route_")||source.component.name.Contains("MissionGate")||source.component is Collider c&&(c.isTrigger||!c.enabled)));
        var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.45f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;settings.minRegionArea=2;var built=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!built)throw new InvalidOperationException("Crater navigation failed");AssetDatabase.CreateAsset(built,Data+"/FrontNavigation.asset");var nav=new SerializedObject(All<CombatNavigation>(s).Single());nav.FindProperty("data").objectReferenceValue=built;nav.ApplyModifiedProperties();
    }
}
#endif
