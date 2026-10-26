using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using UnityEngine;

namespace StoryTell.GameplayPath
{
    // Scene-local soundscape: does not change listener volume, dialogue, or weapon audio.
    public sealed class Level1Atmosphere : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField] private AudioSource wind, campRustle, distantWar, coastalBird;
        [SerializeField] private AudioClip[] distantClips;
        [SerializeField] private AudioClip seagull;
        [SerializeField] private Transform camp, frontline;
        [SerializeField, Range(0,1)] private float windLevel=0.16f, rustleLevel=0.09f, warLevel=0.26f;
        [SerializeField] private float campFogDensity=0.0032f, frontFogDensity=0.0045f;
        private float originalFog, nextWar, nextBird;
        private bool started;
        public float FrontProximity { get; private set; }
        public AudioSource Wind => wind;
        public AudioSource DistantWar => distantWar;
        private void OnEnable(){originalFog=RenderSettings.fogDensity;}
        private void Start()
        {
            wind.volume=campRustle.volume=distantWar.volume=coastalBird.volume=0;
            wind.Play();campRustle.Play();nextWar=Time.time+12;nextBird=Time.time+18;started=true;
        }
        private void Update()
        {
            float distance=Vector3.Distance(controller.Player.position,frontline.position);
            FrontProximity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,235,distance));
            bool ended=controller.Progress.Stage==PathStage.Complete||trial.IsFinishing;
            float focus=ended?0:controller.IsBusy?0.4f:map.IsOpen?0.65f:1;
            wind.volume=Mathf.MoveTowards(wind.volume,windLevel*Mathf.Lerp(0.85f,1.15f,FrontProximity)*focus,Time.unscaledDeltaTime*0.4f);
            campRustle.volume=Mathf.MoveTowards(campRustle.volume,rustleLevel*(1-FrontProximity)*focus,Time.unscaledDeltaTime*0.4f);
            distantWar.volume=Mathf.MoveTowards(distantWar.volume,warLevel*Mathf.Lerp(0.25f,1,FrontProximity)*focus,Time.unscaledDeltaTime*0.5f);
            coastalBird.volume=Mathf.MoveTowards(coastalBird.volume,0.065f*(1-FrontProximity)*focus,Time.unscaledDeltaTime*0.4f);
            RenderSettings.fogDensity=Mathf.Lerp(campFogDensity,frontFogDensity,FrontProximity);
            if(ended){distantWar.Stop();coastalBird.Stop();return;}
            if(Time.time>=nextWar&&!controller.IsBusy&&!map.IsOpen&&distantClips.Length>0)
            {
                nextWar=Time.time+Random.Range(17f,29f);
                // Explosions remain ahead of the player; existing close VFX retain their own audio.
                distantWar.transform.position=frontline.position+Vector3.right*Random.Range(-35f,35f)+Vector3.forward*35;
                distantWar.clip=distantClips[Random.Range(0,distantClips.Length)];distantWar.pitch=Random.Range(0.96f,1.03f);distantWar.Play();
            }
            if(Time.time>=nextBird&&!controller.IsBusy&&!map.IsOpen&&FrontProximity<0.35f&&seagull)
            {
                nextBird=Time.time+Random.Range(35f,55f);coastalBird.transform.position=camp.position+new Vector3(-20,12,12);coastalBird.clip=seagull;coastalBird.Play();
            }
        }
        private void OnDisable()
        {
            RenderSettings.fogDensity=originalFog;
            if(!started)return;
            wind.Stop();campRustle.Stop();distantWar.Stop();coastalBird.Stop();
        }
    }
}
