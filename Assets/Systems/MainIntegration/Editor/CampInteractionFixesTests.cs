#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using InteractionSystem.Scripts.Utils;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.InteractionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InspectionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class CampInteractionFixesTests
{
    static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
    [Test] public void KitchenInvitationTracksTheDiningTableAndWorldObjectsUseE()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        var table=Object.FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Mesa de cocina");var views=new SerializedObject(One<ScenicFocus>()).FindProperty("views");int kitchens=0;
        for(int i=0;i<views.arraySize;i++){var v=views.GetArrayElementAtIndex(i);if(!v.FindPropertyRelative("caption").stringValue.Contains("comedor"))continue;var target=(Transform)v.FindPropertyRelative("target").objectReferenceValue;var entrance=(Transform)v.FindPropertyRelative("entrance").objectReferenceValue;Assert.That(target.IsChildOf(table.transform),Is.True);Assert.That(Vector3.Distance(target.position,table.bounds.center),Is.LessThan(1));Assert.That(Vector3.Distance(entrance.position,table.bounds.center),Is.InRange(8,13));kitchens++;}Assert.That(kitchens,Is.EqualTo(1));
        var objects=Object.FindObjectsByType<InteractableScript>(FindObjectsInactive.Include).Where(i=>i.UIInteraction!=null&&i.InspectInteractable&&!i.GetComponentInParent<DialogueSystem.Scripts.SpeakerScript>()).ToArray();Assert.That(objects.Length,Is.GreaterThan(8));foreach(var item in objects)Assert.That(item.UIInteraction.interactionType,Is.EqualTo(EInteractions.EKeypress),item.name);
        var reticles=Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include).Where(i=>i.name=="Dot").ToArray();Assert.That(reticles.Length,Is.EqualTo(1));foreach(var dot in reticles){Assert.That(dot.enabled,Is.False);Assert.That(dot.gameObject.activeSelf,Is.False);}foreach(var decoration in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include).Where(i=>i.name=="Cross"))Assert.That(decoration.gameObject.activeSelf,Is.True);
    }
    [UnityTest]public IEnumerator RiflesStayCenteredDuringInspectionAndRestoreToTheTable()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        One<InteractionScript>().enabled=false;One<Systems.Missions.FindObjects.EquipmentManager>().StartMission();yield return null;var inspection=One<InspectionScript>();var camera=Camera.main;var items=Object.FindObjectsByType<InteractableScript>(FindObjectsInactive.Include).Where(i=>i.name=="RIFLE"||i.name=="FAL2 (2)"||i.name=="Casco"||i.name=="WalkieTalkie").ToArray();Assert.That(items.Length,Is.EqualTo(4));float oldAspect=camera.aspect;
        foreach(var item in items)
        {
            item.gameObject.SetActive(true);if(item is EquipmentInteractableScript equipment)equipment.fireEvents=false;
            var parent=item.transform.parent;var pos=item.transform.position;var rot=item.transform.rotation;var scale=item.transform.localScale;
            foreach(float aspect in new[]{16f/9,4f/3})
            {
                camera.aspect=aspect;inspection.StartInspect(item.gameObject);yield return null;Assert.That(inspection.IsInspecting,Is.True,item.name);
                foreach(float zoom in new[]{0f,3f,-6f})
                {
                    inspection.AdjustMagnification(zoom);yield return null;var renderers=item.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);var center=camera.WorldToViewportPoint(bounds.center);
                    Assert.That(center.x,Is.InRange(.40f,.60f),item.name+" horizontal center");Assert.That(center.y,Is.InRange(.25f,.75f),item.name+" vertical center");Assert.That(center.z,Is.InRange(.3f,1.5f),item.name+" viewing distance");
                }
                if(item.name=="RIFLE"&&aspect>1.7f){inspection.AdjustMagnification(3);yield return null;yield return Capture(camera,"Library/CampInteractionFixes-rifle.png");}
                inspection.StopInspect();yield return null;Assert.That(item.transform.parent,Is.EqualTo(parent));Assert.That(Vector3.Distance(item.transform.position,pos),Is.LessThan(.001f));Assert.That(Quaternion.Angle(item.transform.rotation,rot),Is.LessThan(.01f));Assert.That(Vector3.Distance(item.transform.localScale,scale),Is.LessThan(.001f));
            }
        }
        camera.aspect=oldAspect;yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator CoastUsesTheTerrainTextureScaleAndHasGroundedGrass()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=One<Terrain>();var skirt=Object.FindObjectsByType<MeshFilter>().Single(f=>f.name=="HorizonTerrainSkirt");var mesh=skirt.sharedMesh;var layer=terrain.terrainData.terrainLayers.First();var coastVertices=mesh.vertices;var coastUV=mesh.uv;int expected=terrain.terrainData.heightmapResolution;Assert.That(mesh.vertexCount,Is.EqualTo(4*8*expected));
        for(int i=0;i<expected;i++){var world=skirt.transform.TransformPoint(coastVertices[i]);Assert.That(Mathf.Abs(world.y-terrain.SampleHeight(world)-terrain.transform.position.y),Is.LessThan(.002f));Assert.That(coastUV[i].x,Is.EqualTo((world.x-terrain.transform.position.x+layer.tileOffset.x)/layer.tileSize.x).Within(.00001f));}
        var grass=Object.FindObjectsByType<MeshFilter>().Where(f=>f.transform.parent&&f.transform.parent.name=="Coastal grass continuation").ToArray();Assert.That(grass.Length,Is.GreaterThanOrEqualTo(1));Assert.That(grass.Sum(g=>g.sharedMesh.vertexCount),Is.GreaterThan(1000));
        var temporary=new GameObject("Test coast support sampler");temporary.transform.SetParent(skirt.transform,false);var collider=temporary.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();int checkedPatches=0;
        foreach(var patch in grass)foreach(var local in patch.sharedMesh.vertices.Where((v,i)=>i%83==0).Take(80)){var world=patch.transform.TransformPoint(local);if(!collider.Raycast(new Ray(world+Vector3.up*10,Vector3.down),out var hit,30))continue;Assert.That(world.y-hit.point.y,Is.InRange(-.2f,3.5f),patch.name);checkedPatches++;}Assert.That(checkedPatches,Is.GreaterThan(20));Object.DestroyImmediate(temporary);
        yield return new EnterPlayMode();for(int i=0;i<8;i++)yield return null;var go=new GameObject("Coastal verification camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=3200;camera.cullingMask=~(1<<5);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;camera.transform.position=new Vector3(45,29.53f,270);camera.transform.LookAt(new Vector3(-100,10,270));yield return null;yield return Capture(camera,"Library/CampInteractionFixes-coast.png");camera.transform.position=new Vector3(5,29.53f,270);camera.transform.LookAt(new Vector3(-35,26,290));yield return null;yield return Capture(camera,"Library/CampInteractionFixes-coast-seam.png");Object.Destroy(go);yield return new ExitPlayMode();
    }
    static IEnumerator Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);target.Create();var old=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;target.Release();Object.Destroy(target);}yield return null;
    }
}
#endif
