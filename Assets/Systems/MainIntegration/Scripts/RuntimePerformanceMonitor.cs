using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public sealed class RuntimePerformanceMonitor : MonoBehaviour
    {
        [SerializeField] private UiSessionCoordinator session;
        [SerializeField] private LevelProgress progress;
        private readonly float[] frames=new float[1800];
        private int count,index;
        private long gcPeak;
        private ProfilerRecorder gc;
        private void OnEnable(){gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);}
        private void OnDisable(){gc.Dispose();}
        private void LateUpdate()
        {
            if(session.Mode!=UiMode.None||Time.timeScale<=0)return;
            frames[index]=Time.unscaledDeltaTime*1000;index=(index+1)%frames.Length;count=Mathf.Min(count+1,frames.Length);
            if(gc.Valid)gcPeak=Math.Max(gcPeak,gc.LastValue);
        }
        [Serializable] public sealed class Report
        {
            public string utc,unity,device,graphics,stage;
            public bool editor;
            public int samples;
            public float averageMs,p95Ms,estimatedFps;
            public long peakAllocatedBytes;
        }
        public Report Capture()
        {
            float total=0;var sorted=new float[count];Array.Copy(frames,sorted,count);Array.Sort(sorted);foreach(float time in sorted)total+=time;
            float average=count>0?total/count:0;
            return new Report{utc=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,device=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName,stage=progress.Stage.ToString(),editor=Application.isEditor,samples=count,averageMs=average,p95Ms=count>0?sorted[Mathf.Min(count-1,Mathf.CeilToInt(count*.95f)-1)]:0,estimatedFps=average>0?1000/average:0,peakAllocatedBytes=gcPeak};
        }
        public bool Export()
        {
            try
            {
                string folder=Path.Combine(Application.persistentDataPath,"Performance");Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder,"Level1-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json"),JsonUtility.ToJson(Capture(),true));return true;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){return false;}
        }
    }
}
