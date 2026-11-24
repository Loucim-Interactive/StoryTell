#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using StoryTell.MainIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class KitchenPotAuthoring
{
    public const string Data="Assets/Systems/MainIntegration/Data/KitchenPot";
    public const string ModelPath="Assets/Art/Props/Kitchen/Olla_Terminada.fbx";
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(All<Transform>(s).Any(t=>t.name=="Olla de cocina - modelo de diseño"))throw new InvalidOperationException("Already applied");Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Use designer pot with a localized meal interaction");
        var table=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);var pot=(GameObject)PrefabUtility.InstantiatePrefab(model,s);Undo.RegisterCreatedObjectUndo(pot,"Place the designer pot");pot.name="Olla de cocina - modelo de diseño";Undo.SetTransformParent(pot.transform,table.transform,"Keep the pot on its table");
        var source=pot.GetComponentsInChildren<MeshFilter>().Single();var localBounds=source.sharedMesh.bounds;float radius=localBounds.extents.z;float factor=.65f/(2*radius);var tableMesh=table.GetComponent<MeshFilter>().sharedMesh.bounds;bool longZ=tableMesh.size.z*Mathf.Abs(table.transform.lossyScale.z)>tableMesh.size.x*Mathf.Abs(table.transform.lossyScale.x);pot.transform.localRotation=Quaternion.Euler(0,longZ?90:0,0);pot.transform.localScale=Vector3.one*factor;pot.transform.localPosition=Vector3.zero;
        var bowlBottom=new Vector3(localBounds.max.x-radius,localBounds.min.y,localBounds.center.z);var support=new Vector3(table.bounds.center.x,table.bounds.max.y+.012f,table.bounds.center.z);pot.transform.position+=support-source.transform.TransformPoint(bowlBottom);
        foreach(var child in pot.GetComponentsInChildren<Transform>())child.gameObject.layer=0;foreach(var mesh in pot.GetComponentsInChildren<MeshFilter>()){var solid=Undo.AddComponent<MeshCollider>(mesh.gameObject);solid.sharedMesh=mesh.sharedMesh;solid.convex=false;solid.isTrigger=false;}
        var meal=All<CampMeal>(s).Single();Undo.RecordObject(meal.transform,"Keep the meal mission attached to the pot interior");Undo.SetTransformParent(meal.transform,table.transform,"Keep the meal on the current table");meal.transform.localScale=Vector3.one;meal.transform.rotation=Quaternion.identity;float soupHeight=Mathf.Lerp(localBounds.min.y,localBounds.max.y,.72f);meal.transform.position=source.transform.TransformPoint(new Vector3(bowlBottom.x,soupHeight,bowlBottom.z));
        var oldRenderer=meal.GetComponent<MeshRenderer>();var oldMesh=meal.GetComponent<MeshFilter>().sharedMesh;Undo.RecordObject(oldRenderer,"Remove the provisional ration visual");oldRenderer.enabled=false;foreach(var oldCollider in meal.GetComponents<Collider>()){Undo.RecordObject(oldCollider,"Replace the oversized meal collider");oldCollider.enabled=false;}
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Caldo caliente provisional"};material.SetColor("_BaseColor",new Color(.25f,.15f,.065f));material.SetFloat("_Smoothness",.42f);AssetDatabase.CreateAsset(material,Data+"/WarmMeal.mat");var soup=new GameObject("Contenido de la olla",typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(soup,"Show food inside the actual pot");soup.transform.SetParent(meal.transform,false);soup.transform.localScale=new Vector3(.54f/oldMesh.bounds.size.x,.006f/oldMesh.bounds.size.y,.54f/oldMesh.bounds.size.z);soup.GetComponent<MeshFilter>().sharedMesh=oldMesh;soup.GetComponent<MeshRenderer>().sharedMaterial=material;soup.layer=0;
        var area=meal.GetComponent<BoxCollider>();if(!area)area=Undo.AddComponent<BoxCollider>(meal.gameObject);Undo.RecordObject(area,"Set the pot interior interaction area");area.enabled=true;area.isTrigger=true;area.center=Vector3.up*.012f;area.size=new Vector3(.43f,.035f,.43f);meal.gameObject.layer=6;
        Undo.RecordObject(meal,"Localized eat prompt");meal.interactableName="Comida caliente";meal.UIInteraction.label="Comer";meal.UIInteraction.interactionType=InteractionSystem.Scripts.Utils.EInteractions.EKeypress;var data=new SerializedObject(meal);var colliders=data.FindProperty("interactionColliders");colliders.arraySize=1;colliders.GetArrayElementAtIndex(0).objectReferenceValue=area;data.FindProperty("subInteractions").boolValue=false;data.FindProperty("inspectInteraction").boolValue=false;data.FindProperty("focusInteraction").boolValue=false;data.FindProperty("stateDescription").boolValue=false;data.ApplyModifiedProperties();
        foreach(var c in All<Component>(s))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);foreach(var t in pot.GetComponentsInChildren<Transform>())if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);File.WriteAllText("Library/KitchenPot-applied.txt","Complete source model retained as one mesh.\nBowl diameter: 0.65m.\nMeal trigger: "+area.bounds+"\nMeal position: "+meal.transform.position+"\nDesigner pot position: "+pot.transform.position+"\nTable position retained: "+table.transform.position);
    }
    public static void RefineContents()
    {
        var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var meal=All<CampMeal>(s).Single();var soup=meal.transform.Find("Contenido de la olla");var mesh=soup.GetComponent<MeshFilter>().sharedMesh;Undo.RecordObject(soup,"Fit the food surface inside the pot");soup.localScale=new Vector3(.54f/mesh.bounds.size.x,.006f/mesh.bounds.size.y,.54f/mesh.bounds.size.z);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
    }
    public static void Survey()
    {
        AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport);var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);if(!model)throw new InvalidOperationException("Model did not import");var report=new StringBuilder();var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);report.AppendLine("IMPORT scale="+importer.globalScale+" fileScale="+importer.fileScale+" units="+importer.useFileScale);
        foreach(var mesh in model.GetComponentsInChildren<MeshFilter>(true))report.AppendLine("MESH "+mesh.name+" local="+mesh.transform.localPosition+" scale="+mesh.transform.lossyScale+" meshBounds="+mesh.sharedMesh.bounds+" vertices="+mesh.sharedMesh.vertexCount);
        foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true)){report.AppendLine("RENDERER "+renderer.name+" "+renderer.bounds);foreach(var mat in renderer.sharedMaterials)report.AppendLine("MATERIAL "+mat?.name+" shader="+mat?.shader.name+" color="+(mat&&mat.HasProperty("_Color")?mat.color:Color.white));}
        var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var meal=All<CampMeal>(s).Single();report.AppendLine("MEAL "+meal.name+" "+meal.transform.position+" layer="+meal.gameObject.layer+" scale="+meal.transform.lossyScale);var table=All<MeshRenderer>(s).Single(r=>r.name=="Mesa de cocina");report.AppendLine("TABLE "+table.bounds+" transform="+table.transform.position+" yaw="+table.transform.eulerAngles.y);foreach(var child in table.GetComponentsInChildren<Transform>(true))report.AppendLine("TABLE CHILD "+child.name);
        var preview=(GameObject)PrefabUtility.InstantiatePrefab(model);preview.name="Olla model preview";preview.transform.position=new Vector3(0,200,0);foreach(var t in preview.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var renderers=preview.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);report.AppendLine("COMPLETE "+bounds);
        foreach(var r in renderers){r.sharedMaterials=r.sharedMaterials.Select(m=>{var n=new Material(Shader.Find("Universal Render Pipeline/Lit"));n.SetColor("_BaseColor",m&&m.HasProperty("_Color")?m.color:new Color(.5f,.5f,.5f));return n;}).ToArray();}
        var host=new GameObject("Model preview camera",typeof(Camera));var camera=host.GetComponent<Camera>();camera.cullingMask=1<<31;camera.fieldOfView=40;camera.nearClipPlane=.01f;camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.28f,.29f);camera.transform.position=bounds.center+new Vector3(1.4f,.9f,-1.6f).normalized*bounds.size.magnitude*1.6f;camera.transform.LookAt(bounds.center);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var target=new RenderTexture(1024,768,24);target.Create();var previous=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1024,768,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1024,768),0,0);pixels.Apply();File.WriteAllBytes("Library/KitchenPot-model-preview.png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);}finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(preview);}
        File.WriteAllText("Library/KitchenPot-survey.txt",report.ToString());
    }
}
#endif
