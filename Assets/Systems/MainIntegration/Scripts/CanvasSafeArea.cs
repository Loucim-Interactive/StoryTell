using UnityEngine;
namespace StoryTell.MainIntegration
{
    [ExecuteAlways]
    public sealed class CanvasSafeArea : MonoBehaviour
    {
        private Rect previous;private Vector2Int screen;
        private void LateUpdate()=>Refresh();
        private void OnEnable()=>Refresh();
        public void Refresh()
        {
            var size=new Vector2Int(Screen.width,Screen.height);var area=Screen.safeArea;if(size.x<=0||size.y<=0)return;
            if(area==previous&&size==screen)return;previous=area;screen=size;
            var rect=(RectTransform)transform;rect.anchorMin=new Vector2(area.xMin/size.x,area.yMin/size.y);rect.anchorMax=new Vector2(area.xMax/size.x,area.yMax/size.y);rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
