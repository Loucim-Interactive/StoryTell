#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StoryTell.GameplayPath;
using StoryTell.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Rendering;

public sealed class GameplayPathTests
{
    [Test] public void Progress_RequiresEveryEquipmentItemAndCorrectOrder()
    {
        var p=new GameplayPathProgress();Assert.That(p.Advance(PathAction.Officer),Is.False);Assert.That(p.Collect(PathEquipment.Rifle),Is.False);
        Assert.That(p.Advance(PathAction.Ruben),Is.True);
        foreach(var item in new[]{PathEquipment.Helmet,PathEquipment.Rifle,PathEquipment.Ammunition,PathEquipment.Map})Assert.That(p.Collect(item),Is.True);
        Assert.That(p.Stage,Is.EqualTo(PathStage.Equipment));Assert.That(p.Collect(PathEquipment.Rifle),Is.False);
        Assert.That(p.Collect(PathEquipment.Radio),Is.True);Assert.That(p.Stage,Is.EqualTo(PathStage.ReturnEquipment));
    }
    [Test] public void Letter_EarlyFragmentsSurvive_ReadingNeverRequired_DuplicatesRejected()
    {
        var p=Equipped();for(int i=0;i<4;i++)Assert.That(p.CollectFragment(i),Is.True);
        Assert.That(p.CollectFragment(0),Is.False);Assert.That(p.CollectFragment(4),Is.False);
        p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);
        Assert.That(p.Stage,Is.EqualTo(PathStage.ReturnLetter));Assert.That(p.Advance(PathAction.Ruben),Is.True);Assert.That(p.Stage,Is.EqualTo(PathStage.Sergeant));
    }
    [Test] public void Progress_NoBattleBeforeDelivery_OnlyOneEnding()
    {
        var p=BeforeDelivery();Assert.That(p.Advance(PathAction.BattleEntrance),Is.False);Assert.That(p.Finish(),Is.False);
        Assert.That(p.Advance(PathAction.Officer),Is.True);Assert.That(p.Advance(PathAction.BattleEntrance),Is.True);
        Assert.That(p.Finish(),Is.True);Assert.That(p.Finish(),Is.False);
    }
    [Test] public void Spray_InGameplayPathNeverLatchesToBlack_RecoversAfterLongBurst()
    {
        var stress=new WeaponSprayStress();for(int i=0;i<100;i++)stress.RecordShot(i*0.12f,false);
        Assert.That(stress.Completed,Is.False);Assert.That(stress.Darkness,Is.LessThan(0.9f));stress.Tick(20,10);Assert.That(stress.Darkness,Is.Zero);
    }
    [Test] public void SavedScene_ContainsCoherentRouteReferences_NoMissingScripts()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var controller=All<GameplayPathController>(scene).Single();
        Assert.That(controller.Definition.totalEnemies,Is.EqualTo(45));Assert.That(controller.Definition.simultaneousEnemies,Is.InRange(5,8));
        Assert.That(All<GameplayPathInteractable>(scene).Count(x=>x.Action==PathAction.Fragment),Is.EqualTo(4));
        foreach(var action in new[]{PathAction.Helmet,PathAction.Rifle,PathAction.Ammunition,PathAction.Map,PathAction.Radio,PathAction.Crate,PathAction.Officer})
            Assert.That(All<GameplayPathInteractable>(scene).Count(x=>x.Action==action),Is.EqualTo(1),action.ToString());
        Assert.That(All<GameplayPathBattleGate>(scene).Count(),Is.EqualTo(1));Assert.That(All<GameplayPathEnemy>(scene).Count(),Is.EqualTo(8));
        Assert.That(All<WeaponTrialPickup>(scene).All(x=>!x.gameObject.activeInHierarchy),Is.True);
        Assert.That(All<NavMeshSurface>(scene).Any(s=>s.navMeshData!=null),Is.True);
        foreach(var t in All<Transform>(scene))Assert.That(t.GetComponents<Component>().All(c=>c!=null),Is.True,t.name);
        foreach(var scaler in All<CanvasScaler>(scene))Assert.That(scaler.uiScaleMode,Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize),scaler.name);
        foreach(var conversation in new[]{controller.Definition.introduction,controller.Definition.equipmentReady,controller.Definition.kitchen,controller.Definition.letterRequest,controller.Definition.letterReturn,controller.Definition.sergeant,controller.Definition.delivery})
        {Assert.That(conversation,Is.Not.Null);Assert.That(conversation.Dialogues.Length,Is.GreaterThan(1));Assert.That(conversation.Dialogues.All(d=>d!=null&&!string.IsNullOrWhiteSpace(d.DialogueText)),Is.True);}
    }
    [Test] public void Navigation_AllBattleRouteSegmentsReachPlayerEntrance()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var battle=All<GameplayPathBattle>(scene).Single();
        var routes=new SerializedObject(battle).FindProperty("routes");var gate=All<GameplayPathBattleGate>(scene).Single();
        for(int lane=0;lane<routes.arraySize;lane++)
        {
            var points=routes.GetArrayElementAtIndex(lane).FindPropertyRelative("points");Vector3 previous=Vector3.zero;
            for(int i=0;i<=points.arraySize;i++)
            {
                Vector3 position=i<points.arraySize?((Transform)points.GetArrayElementAtIndex(i).objectReferenceValue).position:gate.transform.position;
                Assert.That(NavMesh.SamplePosition(position,out var hit,6,NavMesh.AllAreas),Is.True,$"Lane {lane}, point {i} has no walkable ground");
                if(i>0){var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(previous,hit.position,NavMesh.AllAreas,path),Is.True);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),$"Disconnected route {lane}:{i}");}previous=hit.position;
            }
        }
    }
    [Test] public void Navigation_ConnectsNarration5_MiniCampAndBattleEntrance()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var gate=All<GameplayPathBattleGate>(scene).Single();
        var transforms=All<Transform>(scene);var points=new[]{transforms.First(t=>t.name=="Narration zone 5").position,transforms.First(t=>t.name=="Oficial_Recepcion_Caja").position,gate.transform.position};
        Vector3 previous=Vector3.zero;
        for(int i=0;i<points.Length;i++)
        {
            Assert.That(NavMesh.SamplePosition(points[i],out var hit,8,NavMesh.AllAreas),Is.True,"Route anchor "+i);
            if(i>0){var path=new NavMeshPath();NavMesh.CalculatePath(previous,hit.position,NavMesh.AllAreas,path);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),"The delivery and combat route must be connected.");}previous=hit.position;
        }
    }
    [UnityTest] public IEnumerator Runtime_WeaponGate_EnemyMovementAndContactEnding()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var controller=Object.FindAnyObjectByType<GameplayPathController>();var trial=Object.FindAnyObjectByType<Level1WeaponTrial>();
        Assert.That(trial.CanUseWeapon,Is.False);Assert.That(controller.TryEnterBattle(),Is.False);
        var p=controller.Progress;p.Advance(PathAction.Ruben);
        Assert.That(controller.Interact(PathAction.Rifle),Is.True);Assert.That(trial.HasRifle,Is.True);Assert.That(trial.Weapon.TryFire(),Is.EqualTo(WeaponFireResult.NotReady));
        Assert.That(controller.ShowRifle,Is.False,"The carried rifle stays hidden before combat.");
        var view = (GameObject)new SerializedObject(trial).FindProperty("heldVisual").objectReferenceValue;
        yield return null; Assert.That(view.activeSelf,Is.False);
        Assert.That(controller.Interact(PathAction.Map),Is.True);
        var map=Object.FindAnyObjectByType<StoryTell.PhysicalMap.PhysicalMapController>();
        map.SetOpen(true,true);yield return null;Assert.That(map.IsOpen,Is.True,"Map remains available outside combat.");map.SetOpen(false,true);
        var radio=controller.Player.GetComponent<Systems.WalkieSystem.Scripts.WalkieTalkieScript>();
        var enterRadio=radio.GetType().GetMethod("EnterWalkie",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var exitRadio=radio.GetType().GetMethod("ExitWalkie",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        enterRadio.Invoke(radio,null);yield return null;Assert.That(radio.IsEquipped,Is.True,"Radio remains available outside combat.");exitRadio.Invoke(radio,null);
        foreach(var item in new[]{PathEquipment.Helmet,PathEquipment.Ammunition,PathEquipment.Radio})p.Collect(item);
        p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);for(int i=0;i<4;i++)p.CollectFragment(i);
        p.Advance(PathAction.Ruben);p.Advance(PathAction.Sergeant);p.Advance(PathAction.Crate);p.Advance(PathAction.Officer);
        var gate=Object.FindAnyObjectByType<GameplayPathBattleGate>();var cc=controller.Player.GetComponent<CharacterController>();cc.enabled=false;controller.Player.position=gate.transform.position-Vector3.up*2;cc.enabled=true;
        Assert.That(controller.TryEnterBattle(),Is.True);Assert.That(controller.WeaponAllowed,Is.True);
        yield return null;Assert.That(view.activeSelf,Is.True,"Rifle is shown inside the active combat zone.");
        Vector3 combatPosition=controller.Player.position;
        var zone=(BoxCollider)new SerializedObject(trial).FindProperty("useZone").objectReferenceValue;
        cc.enabled=false;controller.Player.position=zone.bounds.center+Vector3.right*(zone.bounds.extents.x+3);cc.enabled=true;
        yield return null;Assert.That(view.activeSelf,Is.False,"Leaving the zone hides the rifle again.");
        Assert.That(map.enabled,Is.True);enterRadio.Invoke(radio,null);Assert.That(radio.IsEquipped,Is.True);exitRadio.Invoke(radio,null);
        cc.enabled=false;controller.Player.position=combatPosition;cc.enabled=true;yield return null;
        enterRadio.Invoke(radio,null);yield return null;Assert.That(view.activeSelf,Is.False,"Radio and rifle must not overlap.");Assert.That(controller.WeaponAllowed,Is.False);
        exitRadio.Invoke(radio,null);yield return null;Assert.That(view.activeSelf,Is.True);
        var serialized=new SerializedObject(trial);serialized.FindProperty("requireLockedPointer").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(trial.CanUseWeapon,Is.True);Assert.That(trial.Weapon.TryFire(),Is.EqualTo(WeaponFireResult.Fired));Assert.That(trial.IsFinishing,Is.False);
        var battle=Object.FindAnyObjectByType<GameplayPathBattle>();float until=Time.realtimeSinceStartup+12;
        while(battle.Spawned==0&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(battle.Spawned,Is.GreaterThan(0));var enemy=Object.FindAnyObjectByType<GameplayPathEnemy>();Assert.That(enemy,Is.Not.Null);
        Vector3 before=enemy.transform.position;float moveUntil=Time.time+1;while(Time.time<moveUntil)yield return null;Assert.That(Vector3.Distance(before,enemy.transform.position),Is.GreaterThan(0.05f));
        var agent=enemy.GetComponent<NavMeshAgent>();agent.isStopped=true;
        Vector3 shooterPosition=enemy.transform.position-Vector3.forward*6;
        cc.enabled=false;controller.Player.position=shooterPosition;cc.enabled=true;
        var camera=Camera.main;var look=controller.Player.GetComponent<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>();
        Vector3 delta=enemy.transform.position+Vector3.up*0.95f-camera.transform.position;controller.Player.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up));look.SetCameraPitch(-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);
        trial.Weapon.Condition.ConfigureExactIntervalsForTests(100,100,100,100);yield return null;trial.Weapon.SetAim(true);
        float health=enemy.Health;Assert.That(trial.Weapon.TryFire(),Is.EqualTo(WeaponFireResult.Fired));Assert.That(enemy.Health,Is.LessThan(health),"Hitscan must hit enemies sharing ====GAMEPLAY==== with the player.");
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(enemy.transform.parent);blocker.transform.position=(camera.transform.position+enemy.transform.position+Vector3.up)*0.5f;blocker.transform.localScale=new Vector3(3,3,0.5f);
        Physics.SyncTransforms();float fireAt=Time.time+trial.Weapon.Definition.FireInterval+0.1f;while(Time.time<fireAt)yield return null;
        health=enemy.Health;Assert.That(trial.Weapon.TryFire(),Is.EqualTo(WeaponFireResult.Fired));Assert.That(enemy.Health,Is.EqualTo(health),"Cover must block shots even under the common gameplay root.");Object.Destroy(blocker);
        Assert.That(enemy.ReceiveWeaponDamage(1000,enemy.transform.position,Vector3.forward,trial.gameObject),Is.True);Assert.That(battle.Defeated,Is.EqualTo(1));Assert.That(trial.IsFinishing,Is.False,"A temporarily empty group must not end the scene.");
        Assert.That(controller.FinishBattle(true),Is.True);Assert.That(controller.FinishBattle(false),Is.False);Assert.That(trial.CanUseWeapon,Is.False);
        until=Time.realtimeSinceStartup+6;while(!trial.Completed&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(trial.Completed,Is.True);Assert.That(trial.CentralDarkness,Is.EqualTo(1));yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator Runtime_All45EnemiesMustBeEliminatedBeforeVictory()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var controller=Object.FindAnyObjectByType<GameplayPathController>();var battle=Object.FindAnyObjectByType<GameplayPathBattle>();var trial=Object.FindAnyObjectByType<Level1WeaponTrial>();
        var p=controller.Progress;p.Advance(PathAction.Ruben);p.Collect(PathEquipment.All);p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);
        for(int i=0;i<4;i++)p.CollectFragment(i);p.Advance(PathAction.Ruben);p.Advance(PathAction.Sergeant);p.Advance(PathAction.Crate);p.Advance(PathAction.Officer);
        var config=Object.Instantiate(controller.Definition);config.enemySpeed=0.1f;config.spawnInterval=0;
        var s=new SerializedObject(battle);s.FindProperty("definition").objectReferenceValue=config;s.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(controller.TryEnterBattle(),Is.True);float deadline=Time.realtimeSinceStartup+18;
        while(battle.Defeated<45&&Time.realtimeSinceStartup<deadline)
        {
            foreach(var enemy in Object.FindObjectsByType<GameplayPathEnemy>(FindObjectsSortMode.None))enemy.ReceiveWeaponDamage(1000,enemy.transform.position,Vector3.forward,trial.gameObject);
            Assert.That(battle.Alive,Is.LessThanOrEqualTo(config.simultaneousEnemies));
            if(battle.Defeated<45)Assert.That(trial.IsFinishing,Is.False,$"Ended after only {battle.Defeated} enemies");yield return null;
        }
        Assert.That(battle.Spawned,Is.EqualTo(45));Assert.That(battle.Defeated,Is.EqualTo(45));Assert.That(trial.IsFinishing,Is.True);Assert.That(trial.CentralDarkness,Is.LessThan(1));
        deadline=Time.realtimeSinceStartup+8;while(!trial.Completed&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(trial.Completed,Is.True);Object.Destroy(config);yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator Runtime_ConversationCompletionAdvances_CancellationDoesNot()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;
        var controller=Object.FindAnyObjectByType<GameplayPathController>();var dialogue=Object.FindAnyObjectByType<Systems.DialogueSystem.Scripts.DialogueManagerScript>();
        controller.Interact(PathAction.Ruben);yield return null;Assert.That(controller.IsBusy,Is.True);
        dialogue.CancelConversation(controller);yield return null;yield return null;Assert.That(controller.Progress.Stage,Is.EqualTo(PathStage.Arrival));
        Time.timeScale=20;controller.Interact(PathAction.Ruben);float deadline=Time.realtimeSinceStartup+12;
        while(controller.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
        Time.timeScale=1;Assert.That(controller.Progress.Stage,Is.EqualTo(PathStage.Equipment));
        var camera=Camera.main;var look=controller.Player.GetComponent<Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera.CameraControllerScript>();
        var direction=new Vector3(78,controller.Player.position.y,277)-controller.Player.position;controller.Player.rotation=Quaternion.LookRotation(direction);look.SetCameraPitch(15);
        yield return null;Capture("Library/GameplayPath-arrival.png");
        Assert.That(controller.Interact(PathAction.Map),Is.True);
        var map=Object.FindAnyObjectByType<StoryTell.PhysicalMap.PhysicalMapController>();map.SetOpen(true,true);
        float ready=Time.time+0.5f;while(Time.time<ready)yield return null;
        Capture("Library/GameplayPath-map-tasks.png");
        var viewport=Camera.main.WorldToViewportPoint(map.transform.position);
        File.WriteAllText("Library/GameplayPath-map-diagnostics.txt",$"open={map.IsOpen} position={map.transform.position} viewport={viewport} camera={Camera.main.transform.position} anchor={map.ViewAnchor.position} mapCameraEnabled={map.GetComponent<StoryTell.PhysicalMap.PhysicalMapViewCamera>().MapCamera.enabled}");
        Assert.That(viewport.z,Is.InRange(0.1f,2),"The physical map must open at the player's hands, not fly in from world origin.");
        Assert.That(viewport.x,Is.InRange(0f,1f));Assert.That(viewport.y,Is.InRange(0f,1f));
        Assert.That(controller.Progress.Stage,Is.EqualTo(PathStage.Equipment));
        map.SetOpen(false,true);controller.OpenReading("Carta opcional\n\nEsta lectura no completa ni bloquea ninguna misión.");yield return null;
        Assert.That(controller.IsBusy,Is.True);Assert.That(controller.WeaponAllowed,Is.False);Capture("Library/GameplayPath-reading.png");
        yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
    private static GameplayPathProgress Equipped(){var p=new GameplayPathProgress();p.Advance(PathAction.Ruben);p.Collect(PathEquipment.All);return p;}
    private static GameplayPathProgress BeforeDelivery(){var p=Equipped();p.Advance(PathAction.Ruben);p.Advance(PathAction.Eat);p.Advance(PathAction.Ruben);for(int i=0;i<4;i++)p.CollectFragment(i);p.Advance(PathAction.Ruben);p.Advance(PathAction.Sergeant);p.Advance(PathAction.Crate);return p;}
    private static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    private static void Capture(string path)
    {
        var camera=Camera.main;var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=canvases.Select(c=>c.worldCamera).ToArray();var oldTarget=camera.targetTexture;var previous=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.3f;}
            rt.Create();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
            RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget;RenderTexture.active=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=cameras[i];}
            rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
    }
}
#endif
