using UnityEngine;
using Systems.DialogueSystem.Scripts;

namespace StoryTell.MainIntegration
{
    public sealed class FrontApproachAmbience:MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private LevelProgress progress;
        [SerializeField] private UiSessionCoordinator session;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private AudioSource distantExplosions;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioSource[] campVoices;
        [SerializeField] private ParticleSystem[] smoke;
        [SerializeField] private float startZ=335,endZ=525;
        private float[] originalVoiceVolumes;
        private float untilExplosion=18;
        public float Intensity{get;private set;}
        public float ExplosionInterval=>Mathf.Lerp(30,11,Intensity);
        private void Awake(){originalVoiceVolumes=new float[campVoices.Length];for(int i=0;i<campVoices.Length;i++)if(campVoices[i])originalVoiceVolumes[i]=campVoices[i].volume;ComfortAudioGain.AttachTo(distantExplosions);}
        private void Update()
        {
            if(!player)return;ApplyForPosition(player.position.z);
            if(Time.timeScale<=0||Intensity<.08f||(session&&session.Mode!=UiMode.None)||(dialogue&&dialogue.IsInConversation)||(progress&&progress.Stage==RouteStage.Battle))return;
            untilExplosion-=Time.deltaTime;
            if(untilExplosion<=0&&distantExplosions&&!distantExplosions.isPlaying&&clips.Length>0){distantExplosions.clip=clips[Random.Range(0,clips.Length)];distantExplosions.Play();untilExplosion=ExplosionInterval*Random.Range(.85f,1.2f);}
        }
        public void ApplyForPosition(float z)
        {
            Intensity=Mathf.SmoothStep(0,1,Mathf.InverseLerp(startZ,endZ,z));
            if(distantExplosions)distantExplosions.volume=Mathf.Lerp(.015f,.24f,Intensity)*Intensity;
            if(originalVoiceVolumes!=null)for(int i=0;i<campVoices.Length;i++)if(campVoices[i])campVoices[i].volume=originalVoiceVolumes[i]*Mathf.Lerp(1,.15f,Intensity);
            foreach(var plume in smoke)if(plume){var emission=plume.emission;emission.rateOverTime=Intensity*5;}
        }
        private void OnDisable(){if(distantExplosions)distantExplosions.Stop();if(originalVoiceVolumes!=null)for(int i=0;i<campVoices.Length;i++)if(campVoices[i])campVoices[i].volume=originalVoiceVolumes[i];foreach(var plume in smoke)if(plume){var emission=plume.emission;emission.rateOverTime=0;}}
    }
}
