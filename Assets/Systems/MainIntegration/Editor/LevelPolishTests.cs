#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using StoryTell.PhysicalMap;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.WeaponSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class LevelPolishTests
{
    const string Scene="Assets/Scenes/Level1.unity";
    [Test] public void Scene_HasCollisionOnStaticRocksAndTents_AndGroundedMissionItems()
    {
        var scene=EditorSceneManager.OpenScene(Scene);var roots=scene.GetRootGameObjects();
        var filters=roots.SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh&&(f.name.StartsWith("RockCl")||f.name.StartsWith("Tent_"))).ToArray();
        Assert.That(filters.Length,Is.GreaterThan(10));foreach(var f in filters)Assert.That(f.GetComponent<Collider>(),Is.Not.Null,f.name);
        var player=Object.FindAnyObjectByType<PlayerControllerScript>();Assert.That(player.transform.position.z,Is.InRange(250,300));
        var route=player.GetComponent<RouteBoundary>();Assert.That(route.Contains(player.transform.position),Is.True);
        Assert.That(route.Contains(new Vector3(500,0,500)),Is.False);Assert.That(route.Contains(new Vector3(5,0,430)),Is.False);
        foreach(var piece in Object.FindObjectsByType<LetterInteractableScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.name.StartsWith("MateoLetterFragment")))
        {
            Assert.That(route.Contains(piece.transform.position),Is.True,piece.name);
            Assert.That(Physics.Raycast(piece.transform.position+Vector3.up*.08f,Vector3.down,out var hit,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),Is.True,piece.name);
        }
        Assert.That(RenderSettings.fog,Is.True);Assert.That(RenderSettings.skybox.shader.name,Is.EqualTo("StoryTell/ColdOvercastSky"));
        foreach(var shader in new[]{Shader.Find("StoryTell/ColdOvercastSky"),Shader.Find("StoryTell/DistantWater")})Assert.That(ShaderUtil.ShaderHasError(shader),Is.False,shader.name);
        foreach(var root in roots)foreach(var transform in root.GetComponentsInChildren<Transform>(true))Assert.That(transform.GetComponents<Component>().All(c=>c),Is.True,transform.name);
    }
    [UnityTest] public IEnumerator Map_StableReadableAndMarkerInFrontOfPaper_BodyHidden()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var presentation=Object.FindAnyObjectByType<MainPresentation>();presentation.UnlockMap();var map=presentation.Map;map.SetOpen(true,true);
        yield return null;yield return new WaitForSeconds(.2f);
        Assert.That(map.GetComponent<Rigidbody>().isKinematic,Is.True);
        Assert.That(Vector3.Distance(map.transform.position,map.ViewAnchor.TransformPoint(map.Settings.localHoldPosition)),Is.LessThan(.002f));
        var reference=map.GetComponent<MapWorldReference>();Assert.That(reference.TryWorldToMapPosition(map.ViewAnchor.position,out var p,out var inside),Is.True);Assert.That(inside,Is.True);
        Assert.That(Vector3.Dot(p-map.transform.position,map.ViewAnchor.position-map.transform.position),Is.GreaterThan(0));
        var corners=map.GetComponent<MeshRenderer>().bounds;var camera=map.GetComponentInChildren<Camera>();
        var marker=Object.FindAnyObjectByType<MapMarkerController>();Assert.That(marker.IsInsideMap,Is.True);
        var visibility=Object.FindAnyObjectByType<FirstPersonBodyVisibility>();var data=new SerializedObject(visibility);var bodies=data.FindProperty("bodyRenderers");Assert.That(bodies.arraySize,Is.GreaterThan(0));
        for(int i=0;i<bodies.arraySize;i++)Assert.That(((Renderer)bodies.GetArrayElementAtIndex(i).objectReferenceValue).shadowCastingMode,Is.EqualTo(ShadowCastingMode.ShadowsOnly));
        yield return Capture("Library/LevelPolish-map.png");map.SetOpen(false,true);yield return Capture("Library/LevelPolish-camp.png");
        yield return new ExitPlayMode();
    }
    [Test] public void Route_GroundAndCollisionPermitTravelFromCampToCombatEntrance()
    {
        var scene=EditorSceneManager.OpenScene(Scene);var route=Object.FindAnyObjectByType<RouteBoundary>();var terrain=Object.FindAnyObjectByType<Terrain>();
        var gate=(GameObject)new SerializedObject(route).FindProperty("frontBarrier").objectReferenceValue;
        var player=Object.FindAnyObjectByType<PlayerControllerScript>();var playerController=player.GetComponent<CharacterController>();
        bool gateActive=gate.activeSelf,controllerEnabled=playerController.enabled;gate.SetActive(false);playerController.enabled=false;Physics.SyncTransforms();
        try
        {
            const int minX=0,minZ=235,width=76,height=180;const float step=2;
            var visited=new bool[width,height];var passable=new sbyte[width,height];var heights=new float[width,height];
            for(int x=0;x<width;x++)for(int z=0;z<height;z++)heights[x,z]=terrain.SampleHeight(new Vector3(minX+x*step,0,minZ+z*step))+terrain.transform.position.y;
            bool CanVisit(int x,int z)
            {
                if(x<0||z<0||x>=width||z>=height)return false;if(passable[x,z]!=0)return passable[x,z]>0;
                var ground=new Vector3(minX+x*step,heights[x,z],minZ+z*step);
                bool clear=route.Contains(ground)&&!Physics.CheckCapsule(ground+Vector3.up*.36f,ground+Vector3.up*1.48f,.23f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                passable[x,z]=(sbyte)(clear?1:-1);return clear;
            }
            var queue=new System.Collections.Generic.Queue<Vector2Int>();var start=new Vector2Int(Mathf.RoundToInt((player.transform.position.x-minX)/step),Mathf.RoundToInt((player.transform.position.z-minZ)/step));
            Assert.That(CanVisit(start.x,start.y),Is.True,"Camp spawn must have capsule clearance");queue.Enqueue(start);visited[start.x,start.y]=true;
            bool reached=false;
            var dirs=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(minZ+p.y*step>=526){reached=true;break;}
                foreach(var d in dirs){var n=p+d;if(!CanVisit(n.x,n.y)||visited[n.x,n.y]||Mathf.Abs(heights[p.x,p.y]-heights[n.x,n.y])>step*.9f)continue;visited[n.x,n.y]=true;queue.Enqueue(n);}
            }
            Assert.That(reached,Is.True,"There must be an unobstructed walkable route through the native zones after finishing missions");
        }
        finally{gate.SetActive(gateActive);playerController.enabled=controllerEnabled;}
    }
    [UnityTest] public IEnumerator LetterHints_UpdateToRemainingFragment_AndRouteRejectsEscape()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var letter=Object.FindAnyObjectByType<LetterManager>();var provider=Object.FindObjectsByType<LetterHintProvider>(FindObjectsSortMode.None).First();
        Assert.That(provider.GetHint(),Is.Null);letter.StartMission();var first=provider.GetHint();Assert.That(first,Is.Not.Null);
        var parts=Object.FindObjectsByType<LetterInteractableScript>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("MateoLetterFragment")).OrderBy(f=>f.name).ToArray();
        parts[0].Interact();parts[0].OnInspectionEnded();yield return null;Assert.That(letter.CollectedCount,Is.EqualTo(1));Assert.That(provider.GetHint(),Is.Not.SameAs(first));
        var player=Object.FindAnyObjectByType<PlayerControllerScript>();var controller=player.GetComponent<CharacterController>();var safe=player.transform.position;
        controller.enabled=false;player.transform.position=new Vector3(500,safe.y,500);controller.enabled=true;yield return null;
        Assert.That(Vector3.Distance(player.transform.position,safe),Is.LessThan(1));Assert.That(player.GetComponent<RouteBoundary>().CanProceed,Is.False);
        // The outpost can now be explored freely without touching a small waypoint.
        controller.enabled=false;player.transform.position=new Vector3(94,safe.y,353);controller.enabled=true;yield return null;
        Assert.That(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(94,353)),Is.LessThan(1));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator Weapon_FlashEmitsAndExpires()
    {
        EditorSceneManager.OpenScene(Scene);yield return new EnterPlayMode();yield return null;
        var weapon=Object.FindAnyObjectByType<PlayerWeaponController>();weapon.EquipWeapon("FAL");weapon.SetPresentationBlocked(false);
        var presentation=weapon.GetComponent<WeaponPresentation>();var flash=(ParticleSystem)new SerializedObject(presentation).FindProperty("flash").objectReferenceValue;
        Assert.That(weapon.TryShoot(),Is.True);Assert.That(flash.particleCount,Is.GreaterThan(0));flash.gameObject.SetActive(false);yield return new WaitForSeconds(.2f);
        Assert.That(flash.particleCount,Is.Zero);yield return new ExitPlayMode();
    }
    static IEnumerator Capture(string path)
    {
        yield return null;var camera=Camera.main;var target=new RenderTexture(1280,720,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();Object.Destroy(target);}
    }
}
#endif
