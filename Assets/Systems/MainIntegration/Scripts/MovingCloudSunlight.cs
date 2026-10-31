using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StoryTell.MainIntegration
{
    [RequireComponent(typeof(Light),typeof(UniversalAdditionalLightData))]
    public sealed class MovingCloudSunlight:MonoBehaviour
    {
        [SerializeField] private Vector2 windMetresPerSecond=new Vector2(.65f,.22f);
        [SerializeField] private Vector2 initialOffset=new Vector2(12,7);
        private UniversalAdditionalLightData lightData;
        private float elapsed;
        public Vector2 WindMetresPerSecond=>windMetresPerSecond;
        private void Awake()=>lightData=GetComponent<UniversalAdditionalLightData>();
        private void OnEnable()=>Evaluate(elapsed);
        private void Update(){elapsed+=Time.deltaTime;Evaluate(elapsed);}
        // Also allows fixed-time previews of the same cloud coverage in the Editor.
        public void Evaluate(float seconds)
        {
            if(!lightData)lightData=GetComponent<UniversalAdditionalLightData>();
            var size=lightData.lightCookieSize;
            lightData.lightCookieOffset=new Vector2(Mathf.Repeat(initialOffset.x+seconds*windMetresPerSecond.x,Mathf.Max(1,size.x)),Mathf.Repeat(initialOffset.y+seconds*windMetresPerSecond.y,Mathf.Max(1,size.y)));
        }
    }
}
