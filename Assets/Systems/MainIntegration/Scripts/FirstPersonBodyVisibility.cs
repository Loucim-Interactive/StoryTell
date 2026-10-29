using UnityEngine;
using UnityEngine.Rendering;

namespace StoryTell.MainIntegration
{
    /// <summary>Preserves the player's world shadow without rendering the inside of their mesh.</summary>
    public sealed class FirstPersonBodyVisibility : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers = System.Array.Empty<Renderer>();
        private ShadowCastingMode[] previous;
        private void OnEnable()
        {
            previous = new ShadowCastingMode[bodyRenderers.Length];
            for (int i=0;i<bodyRenderers.Length;i++) if(bodyRenderers[i])
            { previous[i]=bodyRenderers[i].shadowCastingMode; bodyRenderers[i].shadowCastingMode=ShadowCastingMode.ShadowsOnly; }
        }
        private void OnDisable()
        {
            if(previous==null)return;
            for(int i=0;i<bodyRenderers.Length;i++)if(bodyRenderers[i])bodyRenderers[i].shadowCastingMode=previous[i];
        }
    }
}
