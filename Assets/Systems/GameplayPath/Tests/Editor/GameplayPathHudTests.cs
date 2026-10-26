#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using NUnit.Framework;
using StoryTell.GameplayPath;
using StoryTell.PhysicalMap;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class GameplayPathHudTests
{
    [Test] public void SavedBoundary_ContainsMissionRoute_AndWallsMatchEdges()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameplayPathController>(true)).Single();
        var boundary=root.GetComponentInChildren<GameplayPathBoundary>();
        Assert.That(boundary.Barriers.Length,Is.EqualTo(boundary.Outline.Length));
        foreach(var wall in boundary.Barriers){Assert.That(wall.enabled,Is.False);Assert.That(wall.isTrigger,Is.False);}
        foreach(var item in root.GetComponentsInChildren<Transform>(true))
        {
            if(!(item.name.StartsWith("Ruben_")||item.name.StartsWith("Sargento_")||item.name.StartsWith("Oficial_")||item.name.StartsWith("Fragmento_")||item.name.StartsWith("Point_")||item.name=="Kitchen_Table"||item.name=="Mesa_Suministros"))continue;
            Assert.That(boundary.Contains(new Vector2(item.position.x,item.position.z),out _,out float distance),Is.True,item.name);
            Assert.That(distance,Is.GreaterThan(7),item.name+" must be beyond the warning margin");
        }
        Assert.That(boundary.Contains(new Vector2(300,300),out _,out _),Is.False);
    }
    [UnityTest]public IEnumerator Checklist_ChangesChecksAndAnnouncements_HidesOnlyDuringMapAndModalUI()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        var c=Object.FindAnyObjectByType<GameplayPathController>();var hud=Object.FindAnyObjectByType<GameplayPathTaskHud>();
        Assert.That(hud.IsVisible,Is.True);Assert.That(hud.Checklist,Does.Contain("[ ] Hablar con Rubén"));
        c.Progress.Advance(PathAction.Ruben);yield return null;yield return null;
        Assert.That(hud.Announcement,Does.Contain("Recogé el equipo"));Assert.That(hud.Checklist,Does.Contain("[X] Hablar con Rubén"));
        c.Interact(PathAction.Helmet);c.Interact(PathAction.Map);yield return null;yield return null;
        Assert.That(hud.Checklist,Does.Contain("[X] Casco"));Assert.That(hud.Checklist,Does.Contain("[ ] FAL"));
        yield return (IEnumerator)typeof(GameplayPathNextTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"Library/GameplayPath-hud-checklist.png",1280,720});
        var map=Object.FindAnyObjectByType<PhysicalMapController>();map.SetOpen(true,true);yield return null;yield return null;Assert.That(hud.IsVisible,Is.False);
        map.SetOpen(false,true);yield return null;yield return null;Assert.That(hud.IsVisible,Is.True);
        c.OpenReading("Comprobación de interfaz");yield return null;yield return null;Assert.That(hud.IsVisible,Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator Boundary_FirstAttemptTurnsAndRestoresControls_SecondAttemptIsPhysicallyBlocked()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var c=Object.FindAnyObjectByType<GameplayPathController>();var boundary=Object.FindAnyObjectByType<GameplayPathBoundary>();
        var cc=c.Player.GetComponent<CharacterController>();var movement=c.Player.GetComponent<PlayerControllerScript>();movement.enabled=false;
        Teleport(cc,new Vector3(80,0,232));boundary.Evaluate();Teleport(cc,new Vector3(80,0,223));c.Player.rotation=Quaternion.Euler(0,180,0);boundary.Evaluate();
        Assert.That(boundary.Returning,Is.True);Assert.That(c.IsBusy,Is.True);Assert.That(boundary.Barriers[0].enabled,Is.False);
        float until=Time.unscaledTime+2;while(boundary.Returning&&Time.unscaledTime<until)yield return null;
        Assert.That(boundary.Returning,Is.False);Assert.That(boundary.Barriers[0].enabled,Is.True);Assert.That(c.IsBusy,Is.False);
        Assert.That(movement.enabled,Is.False,"Restore prior state, including already-disabled movement.");Assert.That(c.Player.forward.z,Is.GreaterThan(0.98f));
        for(int i=0;i<20;i++)cc.Move(Vector3.back);
        Assert.That(c.Player.position.z,Is.GreaterThan(220),"The actual CharacterController must collide on another attempt.");
        Assert.That(boundary.Returning,Is.False);
        var p=c.Progress;p.Advance(PathAction.Ruben);p.Collect(PathEquipment.All);p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);
        for(int i=0;i<4;i++)p.CollectFragment(i);p.AssembleLetter();p.Advance(PathAction.Ruben);p.Advance(PathAction.Sergeant);p.Advance(PathAction.Crate);p.Advance(PathAction.Officer);p.Advance(PathAction.BattleEntrance);
        movement.enabled=true;Assert.That(c.BeginRouteReturn(),Is.True);Assert.That(c.FinishBattle(true),Is.True);c.EndRouteReturn();
        Assert.That(movement.enabled,Is.False,"A return ending during the final fade must not restore player input.");
        yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
    static void Teleport(CharacterController cc,Vector3 point)
    {
        var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.z>=t.transform.position.z&&point.x<=t.transform.position.x+t.terrainData.size.x&&point.z<=t.transform.position.z+t.terrainData.size.z);
        point.y=terrain.SampleHeight(point)+terrain.transform.position.y+0.15f;cc.enabled=false;cc.transform.position=point;cc.enabled=true;Physics.SyncTransforms();
    }
}
#endif
