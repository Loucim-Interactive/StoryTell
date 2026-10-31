#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Systems.Missions.FindObjects;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StoryTell.MainIntegration.Editor
{
    public static class QuietPresentationAuthoring
    {
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        const string Data="Assets/Systems/MainIntegration/Data/QuietPresentation";
        static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
        static GameObject New(string name,Transform parent,bool ui=false){var go=ui?new GameObject(name,typeof(RectTransform)):new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Quiet presentation");go.transform.SetParent(parent,false);return go;}
        static void Box(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=anchor;rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;}
        static void ConfigureDesignerPieces(Scene scene,SerializedObject puzzleData)
        {
            var front=Rect.MinMaxRect(.1625f,.4955f,.8375f,.9685f);var slots=puzzleData.FindProperty("slots");
            foreach(var piece in All<LetterPuzzlePiece>(scene))
            {
                int i=piece.slotIndex;string source="Assets/Art/Maximo/Interactables/Cartas/CartaLanus/Carta3parte"+(i+1)+"/Carta3parte"+(i+1)+".png";var pixels=new Texture2D(2,2);pixels.LoadImage(File.ReadAllBytes(source));var rgba=pixels.GetPixels32();int width=pixels.width,height=pixels.height,minX=width,minY=height,maxX=0,maxY=0;
                for(int y=Mathf.CeilToInt(front.yMin*height);y<front.yMax*height;y++)for(int x=Mathf.CeilToInt(front.xMin*width);x<front.xMax*width;x++)if(rgba[y*width+x].a>25){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                var crop=Rect.MinMaxRect(minX/(float)width,minY/(float)height,(maxX+1f)/width,(maxY+1f)/height);
                string meshPath=Data+"/DesignerLetterPiece_"+i+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}Undo.RecordObject(mesh,"Original designer alpha silhouette");mesh.Clear();mesh.name="Designer alpha-cut letter "+i;mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};mesh.uv=new[]{new Vector2(crop.xMin,crop.yMin),new Vector2(crop.xMin,crop.yMax),new Vector2(crop.xMax,crop.yMax),new Vector2(crop.xMax,crop.yMin)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                string maskPath=Data+"/DesignerLetterHitMask_"+i+".asset";var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);if(!mask){mask=new Texture2D(64,64,TextureFormat.RGBA32,false,true);AssetDatabase.CreateAsset(mask,maskPath);}var samples=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float alpha=pixels.GetPixelBilinear(Mathf.Lerp(crop.xMin,crop.xMax,(x+.5f)/64),Mathf.Lerp(crop.yMin,crop.yMax,(y+.5f)/64)).a;samples[y*64+x]=new Color(alpha,alpha,alpha,1);}mask.SetPixels(samples);mask.Apply();EditorUtility.SetDirty(mask);UnityEngine.Object.DestroyImmediate(pixels);
                Set(piece,"artworkMesh",mesh);Set(piece,"artworkTexture",AssetDatabase.LoadAssetAtPath<Texture2D>(source));Set(piece,"artworkHitMask",mask);
                var size=new Vector2(crop.width/front.width*450,crop.height/front.height*586.8f);var position=new Vector2((crop.center.x-front.center.x)/front.width*450,(crop.center.y-front.center.y)/front.height*586.8f);var slot=(RectTransform)slots.GetArrayElementAtIndex(i).objectReferenceValue;Undo.RecordObject(slot,"Match the original silhouette position");Box(slot,Vector2.one*.5f,position,size);Undo.RecordObject(piece.rectTransform,"Designer paper proportions");piece.rectTransform.sizeDelta=size;piece.rectTransform.anchorMin=piece.rectTransform.anchorMax=new Vector2(i%2==0?.22f:.78f,i/2==0?.7f:.3f);piece.rectTransform.anchoredPosition=Vector2.zero;piece.color=Color.white;
                var ink=piece.GetComponentInChildren<TMP_Text>(true);if(ink){Undo.RecordObject(ink.gameObject,"Original handwriting replaces generated text");ink.gameObject.SetActive(false);}
            }
            File.WriteAllText(Data+"/DesignerLetterSource.txt","The source FBX planes share the complete sheet geometry. The actual four cuts are in the PNG alpha channels. UI uses their original front textures and alpha silhouettes, with crops and matching target positions derived from their opaque bounds. Original FBX, materials and textures remain unchanged.");
        }
        public static void RefineLetterArt()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");ConfigureDesignerPieces(scene,new SerializedObject(All<LetterAssemblyPuzzle>(scene).Single()));EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void CaptureImpacts()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var center=new Vector3(55,terrain.SampleHeight(new Vector3(55,0,419))+terrain.transform.position.y,419);var go=new GameObject("Temporary impact verification",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=center+new Vector3(4,4,-5);camera.transform.LookAt(center);camera.fieldOfView=60;camera.nearClipPlane=.04f;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;var target=new RenderTexture(1280,720,24);target.Create();var previous=RenderTexture.active;
            try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes("Library/QuietPresentation-impacts.png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);}
            finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
        }
        public static void Inspect()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var report=new StringBuilder();
            foreach(var text in All<TextMesh>(scene))report.AppendLine("TEXT "+text.name+" = "+text.text);
            var data=new SerializedObject(All<LetterManager>(scene).Single()).FindProperty("fragments");
            for(int i=0;i<data.arraySize;i++){var item=(Component)data.GetArrayElementAtIndex(i).objectReferenceValue;foreach(var filter in item.GetComponentsInChildren<MeshFilter>(true))report.AppendLine($"FRAGMENT {i} {item.name} mesh={AssetDatabase.GetAssetPath(filter.sharedMesh)} vertices={filter.sharedMesh.vertexCount}");}
            foreach(string path in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Art/Maximo/Interactables/Cartas/CartaLanus"}).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()){int front=0;var uv=mesh.uv;var tri=mesh.triangles;for(int j=0;j<tri.Length;j+=3)if(uv[tri[j]].y>.49f&&uv[tri[j+1]].y>.49f&&uv[tri[j+2]].y>.49f)front++;report.AppendLine($"MODEL {path} {mesh.name} uv={uv.Length} frontTriangles={front} min={uv.Min(u=>u.y)} max={uv.Max(u=>u.y)}");}
            File.WriteAllText("Library/QuietPresentation-inventory.txt",report.ToString());
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<SaveStatusToast>(scene).Length>0)throw new InvalidOperationException("Already applied");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Simplify presentation and use designer letter art");
            var presentation=All<MainPresentation>(scene).Single();Set(presentation,"combatInstructions",null);foreach(var text in All<TextMesh>(scene).Where(t=>t.name=="Example Instructions"||t.text.Contains("PRUEBA DE COMBATE"))){Undo.RecordObject(text.gameObject,"Hide floating combat prototype");text.gameObject.SetActive(false);}
            var pause=All<PauseMenu>(scene).Single();var pausePanel=(GameObject)new SerializedObject(pause).FindProperty("panel").objectReferenceValue;Undo.RecordObject(pausePanel,"Options open only on request");pausePanel.SetActive(false);
            var host=New("SaveStatus",All<CanvasSafeArea>(scene).Single().transform,true);var toast=host.AddComponent<SaveStatusToast>();var panel=New("BottomRight",host.transform,true);var rect=(RectTransform)panel.transform;Box(rect,new Vector2(1,0),new Vector2(-170,36),new Vector2(280,34));var image=panel.AddComponent<Image>();image.color=new Color(.04f,.06f,.07f,.55f);image.raycastTarget=false;
            var label=New("Label",panel.transform,true).AddComponent<TextMeshProUGUI>();var textRect=label.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(10,0);textRect.offsetMax=new Vector2(-10,0);label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/InterTight-Medium SDF.asset");label.fontSize=20;label.alignment=TextAlignmentOptions.MidlineRight;label.color=new Color(.8f,.84f,.84f);label.raycastTarget=false;Set(toast,"panel",panel);Set(toast,"label",label);Set(toast,"session",All<UiSessionCoordinator>(scene).Single());Set(All<CheckpointController>(scene).Single(),"saveStatus",toast);panel.SetActive(false);
            // Keep the host stretched; its child owns the unobtrusive bottom-right anchor.
            var hostRect=(RectTransform)host.transform;hostRect.anchorMin=Vector2.zero;hostRect.anchorMax=Vector2.one;hostRect.offsetMin=hostRect.offsetMax=Vector2.zero;
            var puzzle=All<LetterAssemblyPuzzle>(scene).Single();var puzzleData=new SerializedObject(puzzle);var board=(RectTransform)puzzleData.FindProperty("board").objectReferenceValue;Undo.RecordObject(board,"Portrait letter layout");board.sizeDelta=new Vector2(500,650);
            ConfigureDesignerPieces(scene,puzzleData);
            BuildImpactMarks(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        static void BuildImpactMarks(Scene scene)
        {
            var terrain=All<Terrain>(scene).Single();float Ground(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            var root=New("Bombardment traces",null);var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/MainIntegration/Data/WeatherMenu/ColdPeatMud.mat"));material.SetColor("_BaseColor",new Color(.28f,.27f,.25f));AssetDatabase.CreateAsset(material,Data+"/ScorchedSoil.mat");int index=0;
            foreach(var center in new[]{new Vector2(55,419),new Vector2(44,478)})
            {
                var vertices=new List<Vector3>{new Vector3(center.x,Ground(center.x,center.y)+.025f,center.y)};var uv=new List<Vector2>{center/3.5f};var colors=new List<Color>{Color.white};var triangles=new List<int>();float[] radii={.5f,.8f,1,1.2f};
                for(int ring=0;ring<4;ring++)for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;float radius=radii[ring]*2.3f*(1+Mathf.Sin(angle*5+index)*.07f);float x=center.x+Mathf.Cos(angle)*radius,z=center.y+Mathf.Sin(angle)*radius;vertices.Add(new Vector3(x,Ground(x,z)+(ring==2?.16f:.028f),z));uv.Add(new Vector2(x,z)/3.5f);colors.Add(new Color(1,1,1,ring==3?0:1));}
                for(int i=0;i<32;i++){int n=(i+1)%32;triangles.AddRange(new[]{0,n+1,i+1});for(int ring=0;ring<3;ring++){int a=1+ring*32+i,b=1+ring*32+n,c=a+32,d=b+32;triangles.AddRange(new[]{a,b,c,c,b,d});}}
                var mesh=new Mesh{name="Scorched impact rim"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/Impact_"+index+".asset");var mark=New("Impact mark "+index,root.transform);mark.AddComponent<MeshFilter>().sharedMesh=mesh;mark.AddComponent<MeshRenderer>().sharedMaterial=material;
                var barrel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Outsourced/Props/Military_Free/Prefabs/Decorations/Barrel_005.prefab"),root.transform);Undo.RegisterCreatedObjectUndo(barrel,"Overturned supplies near shelter");barrel.name="Overturned supply barrel "+index;barrel.transform.position=new Vector3(center.x+2.7f,Ground(center.x+2.7f,center.y),center.y);barrel.transform.rotation=Quaternion.Euler(0,index*45,82);var renderers=barrel.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);barrel.transform.localScale*=1.2f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);barrel.transform.position+=Vector3.up*(Ground(barrel.transform.position.x,barrel.transform.position.z)-bounds.min.y+.025f);foreach(var filter in barrel.GetComponentsInChildren<MeshFilter>())if(!filter.GetComponent<Collider>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;index++;
            }
        }
    }
}
#endif
