#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.WeaponSystem;
using Object = UnityEngine.Object;

namespace StoryTell.MainIntegration.Editor
{
    public static class LevelPolishAuthoring
    {
        const string Data="Assets/Systems/MainIntegration/Data/Polish";
        static Terrain terrain;
        static Material stone, wood, ember, flame;
        static T Add<T>(GameObject obj) where T:Component=>Undo.AddComponent<T>(obj);
        static GameObject New(string name,Transform parent=null)
        {var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Polish Level1");if(parent)g.transform.SetParent(parent,false);return g;}
        static void Set(Object obj,string name,Object value){var so=new SerializedObject(obj);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedProperties();}
        static void Float(Object obj,string name,float value){var so=new SerializedObject(obj);so.FindProperty(name).floatValue=value;so.ApplyModifiedProperties();}
        static void Bool(Object obj,string name,bool value){var so=new SerializedObject(obj);so.FindProperty(name).boolValue=value;so.ApplyModifiedProperties();}
        static void Array(Object obj,string name,Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();}
        static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
        {var existing=AssetDatabase.LoadAssetAtPath<Material>(Data+"/"+name+".mat");if(existing)return existing;var m=new Material(Shader.Find(shader));m.name=name;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);else m.color=color;AssetDatabase.CreateAsset(m,Data+"/"+name+".mat");return m;}
        static Vector3 Ground(float x,float z,float offset=0)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+offset,z);
        static Bounds BoundsOf(GameObject obj)
        {var rs=obj.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Seat(GameObject obj,float y)
        {Undo.RecordObject(obj.transform,"Seat prop on its support");obj.transform.position+=Vector3.up*(y-BoundsOf(obj).min.y);}
        static GameObject Prop(string path,string name,Vector3 pos,Transform parent,float maximumSize=0)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);Undo.RegisterCreatedObjectUndo(g,"Add camp detail");g.name=name;g.transform.position=pos;
            if(maximumSize>0){var b=BoundsOf(g);g.transform.localScale*=maximumSize/Mathf.Max(b.size.x,b.size.y,b.size.z);}
            Seat(g,pos.y);return g;
        }
        static void SolidMeshes(GameObject obj)
        {
            foreach(var filter in obj.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.sharedMesh||filter.GetComponent<Collider>())continue;
                var collider=Add<MeshCollider>(filter.gameObject);collider.sharedMesh=filter.sharedMesh;
            }
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            if(s.GetRootGameObjects().Any(g=>g.name=="Level1_ExperiencePolish"))throw new InvalidOperationException("Already applied.");
            Directory.CreateDirectory("Library/LevelPolishBackups");File.Copy("Assets/Scenes/Level1.unity","Library/LevelPolishBackups/Level1-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Level1 experience polish");
            terrain=All<Terrain>(s).Single();var root=New("Level1_ExperiencePolish");
            stone=Mat("ColdStone",new Color(.23f,.25f,.24f));wood=Mat("WetTimber",new Color(.18f,.13f,.09f));ember=Mat("Embers",new Color(.34f,.08f,.015f));
            flame=Mat("SoftFire",new Color(1,.48f,.07f,1),"Universal Render Pipeline/Particles/Unlit");
            var player=All<PlayerControllerScript>(s).Single();var camera=player.GetComponentInChildren<Camera>();
            Undo.RecordObject(player.transform,"Begin the route in camp");player.transform.position=Ground(85,279,.12f);player.transform.rotation=Quaternion.Euler(0,145,0);
            var visibility=Add<FirstPersonBodyVisibility>(player.gameObject);Array(visibility,"bodyRenderers",player.GetComponentsInChildren<Renderer>(true).Where(r=>PathOf(r.transform).Contains("/Model/")).ToArray());
            Undo.RecordObject(camera,"Reduce near clipping of held props");camera.nearClipPlane=.06f;
            // Only static environment geometry, never equipment, papers, characters or aircraft.
            int collisions=0;
            foreach(var f in All<MeshFilter>(s).Where(f=>PathOf(f.transform).StartsWith("===ENVIRONMENT===")&&(f.name.StartsWith("RockCl")||f.name.StartsWith("SM_Rocks")||f.name.StartsWith("Tent_"))))
            {if(f.GetComponent<Collider>()||!f.sharedMesh)continue;var c=Add<MeshCollider>(f.gameObject);c.sharedMesh=f.sharedMesh;collisions++;}
            ConfigureMap(s);
            BuildRoute(s,player,root.transform);
            BuildCampsAndHints(s,root.transform);
            ConfigureAtmosphere(s,root.transform,camera);
            ConfigureWeapon(s,root.transform);
            // Fix only the new map pickup against the existing equipment table.
            var pickup=All<MapPickup>(s).Single();var table=All<MeshRenderer>(s).Single(r=>r.name=="wooden_table (3)");
            Undo.RecordObject(pickup.transform,"Place map on equipment table");pickup.transform.position=new Vector3(90.8f,table.bounds.max.y+.015f,260.5f);
            foreach(var component in All<Component>(s))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
            File.WriteAllText("Library/LevelPolish-applied.txt",$"Added static mesh collisions: {collisions}\nPlayer starts in camp. Route, hints, map, weapon and cold atmosphere authored.");
        }
        static void ConfigureMap(Scene scene)
        {
            var map=All<PhysicalMapController>(scene).Single();var settings=map.Settings;Undo.RecordObject(settings,"Stable legible map");
            settings.localHoldPosition=new Vector3(-.13f,-.015f,.62f);settings.localHoldEulerAngles=new Vector3(-90,0,0);
            settings.width=.58f;settings.height=.386667f;settings.openCloseDuration=.2f;
            settings.windStrength=0;settings.windTorque=0;settings.bendStrength=0;settings.foldAmount=0;settings.bendPoints=System.Array.Empty<MapBendPoint>();
            Bool(map.GetComponent<MapPhysicsController>(),"stableHold",true);
            var mf=map.GetComponent<MeshFilter>();Undo.RecordObject(mf,"Use wider map mesh");map.GetComponent<PaperDeformationController>().RebuildMesh(settings);
            var mesh=Object.Instantiate(mf.sharedMesh);mesh.name="ReadableMap";AssetDatabase.CreateAsset(mesh,Data+"/ReadableMap.asset");mf.sharedMesh=mesh;
            Float(map.GetComponent<MapWorldReference>(),"markerHeight",.006f);EditorUtility.SetDirty(settings);
        }
        static void BuildRoute(Scene scene,PlayerControllerScript player,Transform root)
        {
            var path=Add<RouteBoundary>(player.gameObject);Set(path,"player",player.transform);Set(path,"controller",player.GetComponent<CharacterController>());Set(path,"equipment",All<EquipmentManager>(scene).Single());Set(path,"letter",All<LetterManager>(scene).Single());Float(path,"frontGateZ",398);
            var points=new[]{new Vector3(80,0,274),new Vector3(87,0,300),new Vector3(110,0,340),new Vector3(94,0,352),new Vector3(36,0,365),new Vector3(72,0,414),new Vector3(38,0,460),new Vector3(36,0,496),new Vector3(56,0,532),new Vector3(63,0,550),new Vector3(70,0,568)};
            var so=new SerializedObject(path);var sections=so.FindProperty("sections");sections.arraySize=points.Length;
            // The camp remains a broad searchable area; the front is a readable corridor.
            for(int i=0;i<points.Length;i++){var p=sections.GetArrayElementAtIndex(i);p.FindPropertyRelative("from").vector3Value=i==0?points[0]:points[i-1];p.FindPropertyRelative("to").vector3Value=points[i];p.FindPropertyRelative("radius").floatValue=i==0?35:i<4?12:i==4?17:i<7?10:12;}
            so.ApplyModifiedProperties();
            var barrier=New("Front_MissionGate",root);barrier.transform.position=Ground(60,398,4);var box=Add<BoxCollider>(barrier);box.size=new Vector3(90,14,1);Set(path,"frontBarrier",barrier);
            // A single static boundary mesh follows the union of permitted sections.
            var walls=New("Route_OuterInvisibleWall",root);var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();const float cell=2;
            for(float x=-5;x<151;x+=cell)for(float z=230;z<590;z+=cell)
            {
                if(!path.Contains(new Vector3(x,0,z)))continue;
                for(int side=0;side<4;side++)
                {
                    Vector2 d=side==0?Vector2.left:side==1?Vector2.right:side==2?Vector2.down:Vector2.up;
                    if(path.Contains(new Vector3(x+d.x*cell,0,z+d.y*cell)))continue;
                    Vector3 middle=new Vector3(x+d.x*cell*.5f,0,z+d.y*cell*.5f);Vector3 tangent=new Vector3(-d.y,0,d.x)*cell*.5f;
                    var a=middle-tangent;var b=middle+tangent;a.y=Ground(a.x,a.z).y-2;b.y=Ground(b.x,b.z).y-2;int n=vertices.Count;
                    vertices.Add(a);vertices.Add(b);vertices.Add(b+Vector3.up*14);vertices.Add(a+Vector3.up*14);
                    triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});
                }
            }
            var mesh=new Mesh{name="RoutePerimeter"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/RoutePerimeter.asset");Add<MeshCollider>(walls).sharedMesh=mesh;
            // Existing events remain; resize only their entry volumes to the defined path.
            var battle=All<Collider>(scene).Single(c=>c.name=="Trigger - walk forward 4m");Undo.RecordObject(battle.transform,"Align combat entrance to path");battle.transform.position=Ground(56,529,1.2f);if(battle is BoxCollider bc){Undo.RecordObject(bc,"Span the designated entrance");bc.size=new Vector3(22/bc.transform.lossyScale.x,5/bc.transform.lossyScale.y,3/bc.transform.lossyScale.z);}
            // Sparse landmarks on the safe side of the route, away from the walking lane.
            Fire("AdvancedPost_Fire",Ground(99,349),root);
            Fire("RestStop_Fire",Ground(57,389),root);
            Prop("Assets/Art/Outsourced/Props/Defensive_props/Prefabs/boxes/box3.prefab","Front_SupplyCache",Ground(62,390),root,1.1f);
        }
        static void BuildCampsAndHints(Scene scene,Transform root)
        {
            const string tent="Assets/Art/Outsourced/Props/Military_Free/Prefabs/Buildings/Tent_002.prefab";
            const string box="Assets/Art/Outsourced/Props/Defensive_props/Prefabs/boxes/box3.prefab";
            var a=Prop(tent,"LetterCamp_WestShelter",Ground(57,272),root);SolidMeshes(a);
            var b=Prop(tent,"LetterCamp_Stores",Ground(65,297),root);b.transform.rotation=Quaternion.Euler(0,90,0);Seat(b,Ground(65,297).y);SolidMeshes(b);
            Fire("CampMain_Fire",Ground(83,275),root);Fire("WestShelter_Fire",Ground(61,278),root);Fire("Stores_Fire",Ground(70,297),root);
            var positions=new[]{new Vector2(63,280),new Vector2(70,291),new Vector2(80,281),new Vector2(66,265)};
            string[] descriptions={"Vi un papel junto al cajón del refugio del oeste. Buscá cerca de la fogata pequeña.","El viento llevó otro hacia los suministros del norte del campamento. Revisá el cajón junto a la carpa.","Hay uno cerca de la mesa de correspondencia, al lado de Mateo. No debería estar lejos.","Quedó un pedazo en los suministros del extremo oeste, cerca de los dos refugios."};
            var fragments=All<LetterInteractableScript>(scene).Where(f=>f.name.StartsWith("MateoLetterFragment")).OrderBy(f=>f.name).ToArray();var hints=new ConversationSO[4];
            for(int i=0;i<4;i++)
            {
                var support=Prop(box,"LetterSupport_"+(i+1),Ground(positions[i].x,positions[i].y),root,.8f);SolidMeshes(support);var bounds=BoundsOf(support);
                Undo.RecordObject(fragments[i].transform,"Place paper on a searchable support");fragments[i].transform.position=new Vector3(bounds.center.x,bounds.max.y+.012f,bounds.center.z);
                var line=ScriptableObject.CreateInstance<DialogueSO>();var so=new SerializedObject(line);so.FindProperty("_speaker").enumValueIndex=(int)ESpeakers.Simon;so.FindProperty("_dialogueText").stringValue=descriptions[i];so.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/LetterHintLine"+i+".asset");
                hints[i]=ScriptableObject.CreateInstance<ConversationSO>();hints[i].Dialogues=new[]{line};AssetDatabase.CreateAsset(hints[i],Data+"/LetterHintConversation"+i+".asset");
            }
            var simon=All<SpeakerScript>(scene).Single(c=>c.name=="NPC - Simon");var jose=All<SpeakerScript>(scene).Single(c=>c.name=="NPC - Jose");
            Undo.RecordObject(simon.transform,"Place scout at letter shelter");simon.transform.position=Ground(63,277,.03f);
            Undo.RecordObject(jose.transform,"Place scout at stores");jose.transform.position=Ground(71,295,.03f);
            foreach(var speaker in new[]{simon,jose})
            {var provider=Add<LetterHintProvider>(speaker.gameObject);Set(provider,"mission",All<LetterManager>(scene).Single());Array(provider,"fragments",fragments);Array(provider,"hints",hints);}
        }
        static void Fire(string name,Vector3 pos,Transform parent)
        {
            var g=New(name,parent);g.transform.position=pos;
            for(int i=0;i<7;i++){float angle=i*Mathf.PI*2/7;var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);Undo.RegisterCreatedObjectUndo(rock,"Build small fire ring");rock.name="FireStone";rock.transform.SetParent(g.transform,false);rock.transform.localPosition=new Vector3(Mathf.Cos(angle)*.4f,.1f,Mathf.Sin(angle)*.4f);rock.transform.localScale=new Vector3(.24f,.18f,.2f);rock.GetComponent<Renderer>().sharedMaterial=stone;}
            var glow=New("WarmLandmarkLight",g.transform);glow.transform.localPosition=Vector3.up*.65f;var light=Add<Light>(glow);light.type=LightType.Point;light.color=new Color(1,.57f,.27f);light.range=6;light.intensity=2;light.shadows=LightShadows.None;
            var particles=Add<ParticleSystem>(New("SmallFlames",g.transform));particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particles.main;main.startLifetime=.65f;main.startSpeed=.55f;main.startSize=.22f;main.startColor=new Color(1,.5f,.07f,.7f);main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;var em=particles.emission;em.rateOverTime=22;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.22f;shape.angle=9;particles.transform.localRotation=Quaternion.Euler(-90,0,0);particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=flame;
        }
        static void ConfigureAtmosphere(Scene scene,Transform root,Camera camera)
        {
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.48f,.55f,.59f);RenderSettings.fogDensity=.0065f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.55f,.63f);RenderSettings.ambientEquatorColor=new Color(.31f,.35f,.38f);RenderSettings.ambientGroundColor=new Color(.16f,.18f,.19f);
            var sky=new Material(Shader.Find("StoryTell/ColdOvercastSky"));AssetDatabase.CreateAsset(sky,Data+"/OvercastSky.mat");RenderSettings.skybox=sky;
            var sun=All<Light>(scene).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun){Undo.RecordObject(sun,"Soft cold daylight");sun.color=new Color(.83f,.9f,1);sun.intensity=.9f;sun.shadows=LightShadows.Soft;sun.shadowBias=.045f;sun.shadowNormalBias=.35f;}
            var sea=GameObject.CreatePrimitive(PrimitiveType.Plane);Undo.RegisterCreatedObjectUndo(sea,"Distant sea horizon");sea.name="DistantSea_VisualOnly";sea.transform.SetParent(root,false);sea.transform.position=new Vector3(750,-1,750);sea.transform.localScale=new Vector3(2000,1,2000);sea.GetComponent<Collider>().enabled=false;sea.GetComponent<Renderer>().sharedMaterial=Mat("DistantSea",new Color(.12f,.21f,.25f),"StoryTell/DistantWater");
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();var white=profile.Add<WhiteBalance>(true);white.temperature.Override(-12);var color=profile.Add<ColorAdjustments>(true);color.saturation.Override(-8);color.contrast.Override(5);color.postExposure.Override(.12f);AssetDatabase.CreateAsset(profile,Data+"/ColdAtmosphere.asset");
            var volume=Add<Volume>(New("ColdAtmosphere",root));volume.isGlobal=true;volume.priority=-10;volume.sharedProfile=profile;
            var audio=Add<AudioSource>(New("DistantWind",root));audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/SoundSystem/Assets/Ambient/Nature/Wind-Blowing.mp3");audio.loop=true;audio.playOnAwake=true;audio.spatialBlend=0;audio.volume=.08f;
        }
        static void ConfigureWeapon(Scene scene,Transform root)
        {
            var weapon=All<PlayerWeaponController>(scene).Single();var so=new SerializedObject(weapon);var visual=(GameObject)so.FindProperty("falVisual").objectReferenceValue;
            var p=Add<WeaponPresentation>(weapon.gameObject);Set(p,"weapon",weapon);Set(p,"visual",visual.transform);
            var mechanism=Add<AudioSource>(New("FAL_MechanismAudio",weapon.transform));mechanism.playOnAwake=false;mechanism.spatialBlend=0;Set(p,"mechanismAudio",mechanism);Set(p,"mechanismClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/SoundSystem/Assets/Radio/MechanicalClick.mp3"));
            var muzzle=New("FAL_MuzzleEffects",visual.transform);var camera=weapon.GetComponentInChildren<Camera>();var bounds=BoundsOf(visual);
            // View-space forward extent of the existing FAL, independent of imported model axes.
            var centers=visual.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).ToArray();Vector3 local=camera.transform.InverseTransformPoint(bounds.center);local.z+=bounds.extents.magnitude*.7f;muzzle.transform.position=camera.transform.TransformPoint(local);muzzle.transform.rotation=camera.transform.rotation;Set(p,"muzzle",muzzle.transform);
            var light=Add<Light>(New("ShotLight",muzzle.transform));light.color=new Color(1,.72f,.4f);light.range=2;light.intensity=1.3f;light.enabled=false;Set(p,"muzzleLight",light);
            var flash=Add<ParticleSystem>(New("ShotFlash",muzzle.transform));flash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=flash.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.045f;main.startSpeed=0;main.startSize=.1f;main.startColor=new Color(1,.75f,.28f,.85f);main.maxParticles=4;var em=flash.emission;em.enabled=false;flash.GetComponent<ParticleSystemRenderer>().sharedMaterial=flame;Set(p,"flash",flash);
        }
        public static void Refine()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();
            var fire=AssetDatabase.LoadAssetAtPath<Material>(Data+"/SoftFire.mat");Undo.RecordObject(fire,"Soft edges for flames and muzzle flash");fire.shader=Shader.Find("StoryTell/SoftEmberParticle");fire.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(fire);
            var supportTable=All<MeshRenderer>(scene).Single(r=>r.name=="wooden_table (3)");
            foreach(var gear in All<Systems.InteractionSystem.Scripts.Interactables.Equipment.EquipmentInteractableScript>(scene))Seat(gear.gameObject,supportTable.bounds.max.y+.012f);
            foreach(var hints in All<LetterHintProvider>(scene))
            {
                Seat(hints.gameObject,Ground(hints.transform.position.x,hints.transform.position.z).y+.015f);
                if(hints.name=="NPC - Jose")
                {
                    var conversations=new SerializedObject(hints).FindProperty("hints");var own=new ConversationSO[conversations.arraySize];
                    for(int i=0;i<own.Length;i++)
                    {
                        string path=Data+"/JoseHint"+i+".asset";own[i]=AssetDatabase.LoadAssetAtPath<ConversationSO>(path);if(own[i])continue;
                        var source=(ConversationSO)conversations.GetArrayElementAtIndex(i).objectReferenceValue;var line=Object.Instantiate(source.Dialogues[0]);var data=new SerializedObject(line);data.FindProperty("_speaker").enumValueIndex=(int)ESpeakers.Jose;data.ApplyModifiedProperties();AssetDatabase.CreateAsset(line,Data+"/JoseHintLine"+i+".asset");own[i]=ScriptableObject.CreateInstance<ConversationSO>();own[i].Dialogues=new[]{line};AssetDatabase.CreateAsset(own[i],path);
                    }
                    Array(hints,"hints",own);
                }
            }
            foreach(var collider in All<MeshCollider>(scene).Where(c=>c.name.StartsWith("Tent_")&&c.sharedMesh))
            {
                var source=collider.sharedMesh;if(source.name.EndsWith("_TwoSidedCollision"))continue;
                string path=Data+"/TentCollision_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=Object.Instantiate(source);mesh.name=source.name+"_TwoSidedCollision";var original=source.triangles;var triangles=new int[original.Length*2];System.Array.Copy(original,triangles,original.Length);for(int i=0;i<original.Length;i+=3){triangles[original.Length+i]=original[i+2];triangles[original.Length+i+1]=original[i+1];triangles[original.Length+i+2]=original[i];}mesh.triangles=triangles;AssetDatabase.CreateAsset(mesh,path);}
                Undo.RecordObject(collider,"Collide with tent fabric from either side");collider.sharedMesh=mesh;
            }
            var marker=All<MapMarkerController>(scene).Single();Undo.RecordObject(marker.transform,"Small readable location dot");marker.transform.localScale=new Vector3(.013f,.002f,.013f);
            foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Entrances()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();var route=All<RouteBoundary>(scene).Single();
            var so=new SerializedObject(route);var entries=so.FindProperty("entrances");entries.arraySize=2;
            var points=new[]{new Vector3(110,0,340),new Vector3(72,0,414)};
            for(int i=0;i<2;i++){var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("point").vector3Value=points[i];e.FindPropertyRelative("radius").floatValue=7;e.FindPropertyRelative("exitZ").floatValue=i==0?350:426;}so.ApplyModifiedProperties();
            var root=scene.GetRootGameObjects().Single(g=>g.name=="Level1_ExperiencePolish");
            if(!root.transform.Find("EntranceLandmarks"))
            {
                var markers=New("EntranceLandmarks",root.transform);
                var locations=new[]{new Vector2(101,334),new Vector2(119,334),new Vector2(63,410),new Vector2(81,410),new Vector2(46,526),new Vector2(66,526)};
                foreach(var location in locations)
                {var prop=Prop("Assets/Art/Outsourced/Props/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab","Entrance_Sandbags",Ground(location.x,location.y),markers.transform,2.2f);SolidMeshes(prop);}
            }
            foreach(var component in All<Component>(scene))if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Horizon()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");terrain=All<Terrain>(scene).Single();var root=scene.GetRootGameObjects().Single(g=>g.name=="Level1_ExperiencePolish");
            if(root.transform.Find("HorizonTerrainSkirt"))return;
            var size=terrain.terrainData.size;var origin=terrain.transform.position;const float margin=350;const int samples=60;
            var inner=new[]{new Vector3(0,0,0),new Vector3(size.x,0,0),new Vector3(size.x,0,size.z),new Vector3(0,0,size.z)};
            var outer=new[]{new Vector3(-margin,-1.5f,-margin),new Vector3(size.x+margin,-1.5f,-margin),new Vector3(size.x+margin,-1.5f,size.z+margin),new Vector3(-margin,-1.5f,size.z+margin)};
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            for(int side=0;side<4;side++)
            {
                int offset=vertices.Count;
                for(int i=0;i<=samples;i++)
                {float t=i/(float)samples;var a=origin+Vector3.Lerp(inner[side],inner[(side+1)%4],t);a.y=terrain.SampleHeight(a)+origin.y-.015f;var b=origin+Vector3.Lerp(outer[side],outer[(side+1)%4],t);vertices.Add(a);vertices.Add(b);}
                for(int i=0;i<samples;i++){int n=offset+i*2;triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
            var mesh=new Mesh{name="DistantTerrainSkirt"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/DistantTerrainSkirt.asset");
            var skirt=New("HorizonTerrainSkirt",root.transform);Add<MeshFilter>(skirt).sharedMesh=mesh;Add<MeshRenderer>(skirt).sharedMaterial=Mat("DistantGround",new Color(.25f,.26f,.22f));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        public static void Audit()
        {
            var s=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var b=new StringBuilder();
            foreach(var t in All<Terrain>(s))b.AppendLine($"TERRAIN {PathOf(t.transform)} p={t.transform.position} size={t.terrainData.size}");
            foreach(var r in All<Renderer>(s).Where(r=>!(r is ParticleSystemRenderer)))b.AppendLine($"RENDER {PathOf(r.transform)} p={r.transform.position} scale={r.transform.lossyScale} bounds={r.bounds} collider={(r.GetComponent<Collider>() ? r.GetComponent<Collider>().GetType().Name : "none")}");
            foreach(var c in All<Collider>(s).Where(c=>c.isTrigger))b.AppendLine($"TRIGGER {PathOf(c.transform)} bounds={c.bounds} components={string.Join(",",c.GetComponents<Component>().Where(x=>x).Select(x=>x.GetType().Name))}\n{string.Join("\n",c.GetComponents<MonoBehaviour>().Where(x=>x).Select(x=>EditorJsonUtility.ToJson(x)))}");
            foreach(var c in All<Camera>(s))b.AppendLine($"CAMERA {PathOf(c.transform)} local={c.transform.localPosition} p={c.transform.position} scale={c.transform.lossyScale} near={c.nearClipPlane}");
            foreach(var c in All<CharacterController>(s))b.AppendLine($"PLAYER {PathOf(c.transform)} center={c.center} height={c.height} radius={c.radius}");
            foreach(var c in All<Animator>(s))b.AppendLine($"ANIMATOR {PathOf(c.transform)} avatar={c.avatar?.name}");
            File.WriteAllText("Library/LevelPolish-audit.txt",b.ToString());
        }
    }
}
#endif
