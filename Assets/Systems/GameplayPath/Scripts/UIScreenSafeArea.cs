using UnityEngine;

namespace StoryTell.GameplayPath
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIScreenSafeArea : MonoBehaviour
    {
        private Rect lastArea;
        private Vector2Int lastSize;
        private void OnEnable() { Apply(); }
        private void Update()
        {
            if (lastArea != Screen.safeArea || lastSize != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rect = (RectTransform)transform;
            lastArea = Screen.safeArea; lastSize = new Vector2Int(Screen.width, Screen.height);
            rect.anchorMin = new Vector2(lastArea.xMin / Screen.width, lastArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(lastArea.xMax / Screen.width, lastArea.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
