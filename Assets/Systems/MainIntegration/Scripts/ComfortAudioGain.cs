using UnityEngine;

namespace StoryTell.MainIntegration
{
    // Multiplying samples preserves source-specific volume curves and spatial attenuation.
    [RequireComponent(typeof(AudioSource))]
    public sealed class ComfortAudioGain : MonoBehaviour
    {
        [SerializeField] private bool voice;
        private volatile float gain=1;
        public static void AttachTo(AudioSource source)
        {
            if(source&&!source.GetComponent<ComfortAudioGain>()&&source.GetComponents<AudioSource>().Length==1&&!source.GetComponent<AudioListener>())source.gameObject.AddComponent<ComfortAudioGain>();
        }
        private void OnEnable(){ComfortSettings.Changed+=Apply;Apply();}
        private void OnDisable(){ComfortSettings.Changed-=Apply;}
        private void Apply(){gain=voice?ComfortSettings.Current.voices:ComfortSettings.Current.effects;}
        private void OnAudioFilterRead(float[] samples,int channels)
        {
            float level=gain;if(level>=.9999f)return;
            for(int i=0;i<samples.Length;i++)samples[i]*=level;
        }
    }
}
