using UnityEngine;
namespace StoryTell.MainIntegration
{
    // Main's original Canvas uses constant pixels. Scale only the added panels, never its authored UI.
    [ExecuteAlways,DefaultExecutionOrder(-1000)]
    public sealed class ReferenceCanvasSpace : MonoBehaviour
    {
        private void LateUpdate()=>Fit();
        public void Fit()
        {
            var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;if(!parent)return;
            Vector2 available=parent.rect.size;if(available.x<=0||available.y<=0)return;
            float scale=Mathf.Min(available.x/1920,available.y/1080);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*0.5f;rect.anchoredPosition=Vector2.zero;rect.localScale=Vector3.one*scale;rect.sizeDelta=available/scale;
        }
    }
}
