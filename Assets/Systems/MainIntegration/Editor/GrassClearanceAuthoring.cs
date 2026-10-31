#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using UnityEngine.SceneManagement;

namespace StoryTell.MainIntegration.Editor
{
    public static class GrassClearanceAuthoring
    {
        [Serializable] public sealed class Spot { public string name; public Vector3 position; public float radius; }
        [Serializable] public sealed class Report { public string terrain; public int resolution; public Vector3 size; public string[] prototypes; public Spot[] spots; public long before,after; public int changedCells; public float affectedPercent; }
        static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static Spot[] Spots(Scene scene)
        {
            var spots=new List<Spot>();
            foreach(var npc in All<SpeakerScript>(scene))spots.Add(new Spot{name=npc.name,position=npc.transform.position,radius=2});
            foreach(var item in All<InteractableScript>(scene))
            {
                if(item.GetComponentInParent<SpeakerScript>()||item.GetComponentInParent<FirstPersonControllerSystem.Scripts.ControllerSys.PlayerControllerScript>())continue;
                spots.Add(new Spot{name=item.name,position=item.transform.position,radius=1.5f});
            }
            foreach(var fire in All<CampfireMotion>(scene))spots.Add(new Spot{name=fire.name,position=fire.transform.position,radius=2});
            foreach(var table in All<MeshRenderer>(scene).Where(r=>r.name.StartsWith("wooden_table")))spots.Add(new Spot{name=table.name,position=table.bounds.center,radius=Mathf.Clamp(Mathf.Max(table.bounds.extents.x,table.bounds.extents.z)+.8f,1.5f,2.8f)});
            return spots.ToArray();
        }
        static Report Describe(Scene scene,Terrain terrain)=>new Report{terrain=AssetDatabase.GetAssetPath(terrain.terrainData),resolution=terrain.terrainData.detailResolution,size=terrain.terrainData.size,prototypes=terrain.terrainData.detailPrototypes.Select(p=>p.prototypeTexture?p.prototypeTexture.name:p.prototype?p.prototype.name:"Unknown").ToArray(),spots=Spots(scene)};
        public static void Preview()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var report=Describe(scene,terrain);
            File.WriteAllText("Library/GrassClearance-preview.json",JsonUtility.ToJson(report,true));
        }
        static float Remaining(Vector3 world,Spot[] spots)
        {
            float remaining=1;
            foreach(var spot in spots)
            {
                float distance=Vector2.Distance(new Vector2(world.x,world.z),new Vector2(spot.position.x,spot.position.z));
                if(distance>=spot.radius)continue;
                float t=Mathf.InverseLerp(spot.radius*.45f,spot.radius,distance);
                remaining=Mathf.Min(remaining,Mathf.Lerp(.18f,1,Mathf.SmoothStep(0,1,t)));
            }
            return remaining;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");var terrain=All<Terrain>(scene).Single();var data=terrain.terrainData;var report=Describe(scene,terrain);
            string backup="Library/GrassClearance-backup";Directory.CreateDirectory(backup);string file=backup+"/Terrain-before.asset";if(File.Exists(file))throw new InvalidOperationException("Already applied or backup exists");File.Copy(report.terrain,file);File.WriteAllText(backup+"/AssetPath.txt",report.terrain);
            int width=data.detailWidth,height=data.detailHeight;var origin=terrain.transform.position;var masks=new float[height,width];int affected=0;
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)
            {
                float sum=0;for(int sz=0;sz<4;sz++)for(int sx=0;sx<4;sx++)sum+=Remaining(origin+new Vector3((x+(sx+.5f)/4)/width*data.size.x,0,(z+(sz+.5f)/4)/height*data.size.z),report.spots);
                masks[z,x]=sum/16;if(masks[z,x]<1)affected++;
            }
            report.affectedPercent=100f*affected/(width*height);if(report.affectedPercent>2)throw new InvalidOperationException("Local clearance exceeds 2 percent of terrain");
            Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Reduce grass beside interactive objects");Undo.RegisterCompleteObjectUndo(data,"Local grass clearance");
            for(int layer=0;layer<data.detailPrototypes.Length;layer++)
            {
                if(!report.prototypes[layer].ToLowerInvariant().Contains("grass"))continue;
                var values=data.GetDetailLayer(0,0,width,height,layer);var original=(int[,])values.Clone();
                for(int z=0;z<height;z++)for(int x=0;x<width;x++){int old=values[z,x];report.before+=old;values[z,x]=Mathf.RoundToInt(old*masks[z,x]);report.after+=values[z,x];if(values[z,x]!=old)report.changedCells++;}
                data.SetDetailLayer(0,0,layer,values);
                var actual=data.GetDetailLayer(0,0,width,height,layer);
                for(int z=0;z<height;z++)for(int x=0;x<width;x++)if(actual[z,x]!=values[z,x]||(masks[z,x]==1&&actual[z,x]!=original[z,x]))throw new InvalidOperationException("Grass verification failed");
            }
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();terrain.Flush();
            File.WriteAllText("Assets/Systems/MainIntegration/Data/WeatherMenu/GrassClearanceReport.json",JsonUtility.ToJson(report,true));AssetDatabase.Refresh();
            WeatherMenuAuthoring.CaptureDetails();
        }
    }
}
#endif
