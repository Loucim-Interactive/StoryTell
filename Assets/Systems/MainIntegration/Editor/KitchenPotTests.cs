#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using InteractionSystem.Scripts.Utils;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.InteractionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class KitchenPotTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    [UnityTest]public IEnumerator OnlyThePotInteriorOffersFoodAndTheMissionStillRequiresDiegosInstruction()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        yield return VerifyPotInPlay();
        yield return new ExitPlayMode();
    }
    private IEnumerator VerifyPotInPlay()
    {
        var meal=One<CampMeal>();var routine=One<CampRoutine>();var table=Object.FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Mesa de cocina");Assert.That(table.enabled,Is.True);Assert.That(Vector3.Distance(table.transform.position,new Vector3(83.7f,27.800615f,253.5f)),Is.LessThan(.03f));
        var pot=GameObject.Find("Olla de cocina - modelo de diseño");Assert.That(pot,Is.Not.Null);var model=pot.GetComponentsInChildren<MeshFilter>().Single();var bounds=model.sharedMesh.bounds;float radius=bounds.extents.z;var center=new Vector3(bounds.max.x-radius,0,bounds.center.z);float diameter=model.transform.TransformVector(Vector3.forward*radius*2).magnitude;Assert.That(diameter,Is.EqualTo(.65f).Within(.015f));
        var solid=pot.GetComponentInChildren<MeshCollider>();
        var oldVisual=meal.GetComponent<MeshRenderer>();
        var trigger=meal.GetComponent<BoxCollider>();
        File.WriteAllText("Library/KitchenPot-target-debug.txt","Pot colliders="+string.Join(",",pot.GetComponentsInChildren<Collider>().Select(c=>c.GetType().Name))+"\nMeal components="+string.Join(",",meal.GetComponents<Component>().Select(c=>c.GetType().Name))+"\nMeal position="+meal.transform.position);
        Assert.That(pot.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(1));
        Assert.That(solid,Is.Not.Null,"Designer pot needs its solid mesh collider");
        Assert.That(solid.isTrigger,Is.False);
        Assert.That(oldVisual,Is.Not.Null,"Preserve the former meal visual reference");
        Assert.That(oldVisual.enabled,Is.False);
        Assert.That(trigger,Is.Not.Null,"The meal needs its small interior box trigger");
        Assert.That(trigger.isTrigger,Is.True);
        Assert.That(meal.GetComponents<Collider>().Count(c=>c.enabled),Is.EqualTo(1));
        Assert.That(meal.UIInteraction,Is.Not.Null);
        Assert.That(meal.UIInteraction.interactionType,Is.EqualTo(EInteractions.EKeypress));
        Assert.That(meal.InspectInteractable,Is.False);
        var interaction=One<InteractionScript>();
        Assert.That(interaction,Is.Not.Null,"Player interaction controller must exist");
        interaction.enabled=false;
        var originField=typeof(InteractionScript).GetField("interactionOrigin",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(originField,Is.Not.Null,"Interaction origin field must exist");
        var original=(Transform)originField.GetValue(interaction);
        var probe=new GameObject("Pot interaction ray probe");
        originField.SetValue(interaction,probe.transform);
        var detect=typeof(InteractionScript).GetMethod("DetectInteraction",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(detect,Is.Not.Null,"Interaction ray method must exist");
        InteractableScript Aim(Vector3 from,Vector3 target){probe.transform.position=from;probe.transform.LookAt(target);Physics.SyncTransforms();return (InteractableScript)detect.Invoke(interaction,null);}
        var interior=meal.GetComponent<BoxCollider>().bounds.center;Assert.That(Aim(interior+Vector3.up,interior),Is.SameAs(meal));int approaches=0;
        for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;var eye=interior+new Vector3(Mathf.Cos(angle)*1.8f,.55f,Mathf.Sin(angle)*1.8f);if(Aim(eye,interior)==meal)approaches++;}Assert.That(approaches,Is.GreaterThanOrEqualTo(3),"Food should be reachable from several sides");
        var rim=model.transform.TransformPoint(new Vector3(center.x+radius*.99f,bounds.max.y,center.z));var handle=model.transform.TransformPoint(new Vector3(bounds.min.x+.2f,bounds.max.y,center.z));var corner=table.GetComponent<MeshFilter>().sharedMesh.bounds;var bareTable=table.transform.TransformPoint(new Vector3(corner.min.x+.1f,corner.max.y,corner.min.z+.1f));
        foreach(var point in new[]{rim,handle,bareTable})Assert.That(Aim(point+Vector3.up,point),Is.Not.SameAs(meal),"Only the interior, not the rim, handle or table, should offer food");
        routine.Restore(false,false);meal.Interact();Assert.That(routine.Ate,Is.False);routine.Restore(true,false);int completions=0;routine.Changed+=()=>completions++;meal.Interact();Assert.That(routine.Ate,Is.True);meal.Interact();Assert.That(completions,Is.EqualTo(1));
        originField.SetValue(interaction,original);Object.Destroy(probe);
        var cameraHost=new GameObject("Kitchen pot review camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();camera.fieldOfView=55;camera.nearClipPlane=.03f;camera.farClipPlane=3200;camera.cullingMask=~(1<<5);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;camera.transform.position=interior+new Vector3(1.1f,.85f,1.4f);camera.transform.LookAt(interior);yield return null;yield return Capture(camera,"Library/KitchenPot-in-game.png");Object.Destroy(cameraHost);
    }
    static IEnumerator Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);target.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);}finally{RenderTexture.active=old;target.Release();Object.Destroy(target);}yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;AudioListener.pause=false;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
