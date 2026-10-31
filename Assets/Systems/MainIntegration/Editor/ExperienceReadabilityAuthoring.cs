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
using Systems.InteractionSystem.Scripts;
using Systems.DialogueSystem.Scripts;

namespace StoryTell.MainIntegration.Editor
{
    public static class ExperienceReadabilityAuthoring
    {
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        const string Data="Assets/Systems/MainIntegration/Data/Readability";
        static void Set(UnityEngine.Object o,string name,UnityEngine.Object value){var so=new SerializedObject(o);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedProperties();}
        static void Array(UnityEngine.Object o,string name,UnityEngine.Object[] values){var so=new SerializedObject(o);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();}
        static GameObject New(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Experience readability");g.transform.SetParent(parent,false);return g;}
        public static void Inspect()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var text=new StringBuilder();
            foreach(var a in All<AudioSource>(scene))text.AppendLine($"AUDIO {PathOf(a.transform)} pos={a.transform.position} clip={AssetDatabase.GetAssetPath(a.clip)} loop={a.loop} play={a.playOnAwake} vol={a.volume} spatial={a.spatialBlend}");
            foreach(var p in All<ParticleSystem>(scene))text.AppendLine($"PARTICLE {PathOf(p.transform)} pos={p.transform.position} rate={p.emission.rateOverTime.constant} max={p.main.maxParticles}");
            foreach(var zone in All<RouteProgressZone>(scene))text.AppendLine($"ZONE {PathOf(zone.transform)} pos={zone.transform.position}");
            foreach(var r in All<MeshRenderer>(scene).Where(r=>r.name.StartsWith("Tent_")))text.AppendLine($"TENT {PathOf(r.transform)} pos={r.bounds.center}");
            File.WriteAllText("Library/ExperienceReadability-inventory.txt",text.ToString());
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");if(scene.GetRootGameObjects().Any(g=>g.name=="ExperienceReadability"))throw new InvalidOperationException("Already applied");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Front approach and readable interactions");var root=New("ExperienceReadability",null);
            var material=new Material(Shader.Find("StoryTell/InteractionFocus"));AssetDatabase.CreateAsset(material,Data+"/InteractionFocus.mat");
            var interaction=All<InteractionScript>(scene).Single();var focus=Undo.AddComponent<InteractionFocusHighlight>(root);Set(focus,"interaction",interaction);Set(focus,"origin",All<Camera>(scene).Single(c=>c.CompareTag("MainCamera")).transform);Set(focus,"session",All<UiSessionCoordinator>(scene).Single());Set(focus,"highlightMaterial",material);
            var atmosphere=Undo.AddComponent<FrontApproachAmbience>(root);Set(atmosphere,"player",interaction.transform);Set(atmosphere,"session",All<UiSessionCoordinator>(scene).Single());Set(atmosphere,"dialogue",All<DialogueManagerScript>(scene).Single());Set(atmosphere,"progress",All<LevelProgress>(scene).Single());Array(atmosphere,"campVoices",System.Array.Empty<UnityEngine.Object>());
            var terrain=All<Terrain>(scene).Single();Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);
            var audio=Undo.AddComponent<AudioSource>(New("Distant front explosions",root.transform));audio.transform.position=Ground(65,590)+Vector3.up*4;audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=.85f;audio.minDistance=80;audio.maxDistance=450;audio.rolloffMode=AudioRolloffMode.Linear;audio.volume=0;Undo.AddComponent<AudioLowPassFilter>(audio.gameObject).cutoffFrequency=2800;Set(atmosphere,"distantExplosions",audio);
            Array(atmosphere,"clips",new[]{"Distant-Explosion01","Distant-Explosion02"}.Select(n=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/SoundSystem/Assets/Ambient/War/Explosions/"+n+".mp3")).ToArray());
            var plumes=new System.Collections.Generic.List<ParticleSystem>();
            foreach(var position in new[]{new Vector2(60,416),new Vector2(48,475),new Vector2(67,519)})
            {
                var go=New("Distant damaged shelter smoke",root.transform);go.transform.position=Ground(position.x,position.y)+Vector3.up*.5f;go.transform.rotation=Quaternion.Euler(-90,0,0);var ps=Undo.AddComponent<ParticleSystem>(go);ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.startLifetime=7;main.startSpeed=.8f;main.startSize=2;main.startColor=new Color(.5f,.5f,.5f,.65f);main.maxParticles=36;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=0;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=15;shape.radius=.8f;var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.4f;velocity.y=.3f;velocity.z=.2f;var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2.5f));var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/MainIntegration/Data/WeatherMenu/FireSmoke.mat");plumes.Add(ps);
            }
            Array(atmosphere,"smoke",plumes.ToArray());
            var source=All<MeshRenderer>(scene).First(r=>r.name=="Tent_002 (1)");var sourceMesh=source.GetComponent<MeshFilter>().sharedMesh;
            int index=0;foreach(var position in new[]{new Vector2(60,416),new Vector2(48,475)})
            {
                var mesh=UnityEngine.Object.Instantiate(sourceMesh);mesh.name="Collapsed shelter canvas";var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){var world=source.transform.TransformPoint(vertices[i]);vertices[i]=new Vector3((world.x-source.bounds.center.x)*.85f,Mathf.Max(.02f,(world.y-source.bounds.min.y)*.32f),(world.z-source.bounds.center.z)*.85f);}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"/CollapsedShelter_"+index+".asset");var go=New("Collapsed shelter "+index,root.transform);go.transform.position=Ground(position.x,position.y);go.transform.rotation=Quaternion.Euler(0,index*45,0);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;go.AddComponent<MeshCollider>().sharedMesh=mesh;index++;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            RefineShelterGrounding();
        }
        public static void RefineShelterGrounding()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();float Ground(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;
            var source=All<MeshRenderer>(scene).First(r=>r.name=="Tent_002 (1)");var original=source.GetComponent<MeshFilter>().sharedMesh.vertices;
            foreach(var shelter in All<MeshFilter>(scene).Where(f=>f.name.StartsWith("Collapsed shelter")))
            {
                Undo.RecordObject(shelter.sharedMesh,"Fit damaged canvas to sloped terrain");var vertices=new Vector3[original.Length];
                for(int i=0;i<vertices.Length;i++){var world=source.transform.TransformPoint(original[i]);var local=new Vector3((world.x-source.bounds.center.x)*.85f,Mathf.Max(.02f,(world.y-source.bounds.min.y)*.32f),(world.z-source.bounds.center.z)*.85f);var position=shelter.transform.TransformPoint(local);local.y+=Ground(position)-shelter.transform.position.y;vertices[i]=local;}
                shelter.sharedMesh.vertices=vertices;shelter.sharedMesh.RecalculateNormals();shelter.sharedMesh.RecalculateTangents();shelter.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(shelter.sharedMesh);shelter.GetComponent<MeshCollider>().sharedMesh=shelter.sharedMesh;
                for(int i=0;i<3;i++)
                {
                    if(shelter.transform.Find("Broken support "+i))continue;
                    var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);Undo.RegisterCreatedObjectUndo(beam,"Broken timber beside damaged shelter");beam.name="Broken support "+i;beam.transform.SetParent(shelter.transform,false);var position=shelter.transform.TransformPoint(new Vector3((i-1)*1.3f,0,i*.7f));position.y=Ground(position)+.18f;beam.transform.position=position;beam.transform.rotation=Quaternion.Euler(0,25+i*65,8);beam.transform.localScale=new Vector3(2.8f,.16f,.18f);beam.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Systems/MainIntegration/Data/WeatherMenu/CharredLogs.mat");
                }
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"/FrontSmoke.mat");if(!material){material=new Material(Shader.Find("StoryTell/WeatherParticle"));material.SetColor("_Color",new Color(.16f,.17f,.18f,.5f));AssetDatabase.CreateAsset(material,Data+"/FrontSmoke.mat");}
            foreach(var smoke in All<ParticleSystem>(scene).Where(p=>p.name=="Distant damaged shelter smoke")){Undo.RecordObject(smoke,"Distinct smoke above damaged shelters");var main=smoke.main;main.startColor=new Color(.8f,.8f,.8f,.9f);Undo.RecordObject(smoke.GetComponent<ParticleSystemRenderer>(),"Darker front smoke");smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
#endif
