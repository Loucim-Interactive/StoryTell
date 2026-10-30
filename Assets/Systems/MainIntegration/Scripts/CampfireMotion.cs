using UnityEngine;
namespace StoryTell.MainIntegration
{
    public sealed class CampfireMotion:MonoBehaviour
    {
        [SerializeField] private Light glow;
        private float intensity,seed;
        private void Awake(){intensity=glow.intensity;seed=Mathf.Abs(transform.position.x*.37f+transform.position.z*.13f);}
        private void Update(){if(glow)glow.intensity=intensity*(.82f+Mathf.PerlinNoise(seed,Time.time*2.1f)*.3f);}
    }
}
