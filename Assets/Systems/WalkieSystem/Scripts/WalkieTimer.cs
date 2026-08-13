using UnityEngine;
using UnityEngine.UI;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieTimer : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider slider;
        [Tooltip("RectTransform that shrinks horizontally. Defaults to this object.")]
        [SerializeField] private RectTransform barRect;

        private Vector3 _fullScale;

        private void Awake()
        {
            if (!fillImage) fillImage = GetComponent<Image>();
            if (!slider) slider = GetComponent<Slider>();
            if (!barRect) barRect = transform as RectTransform;
            if (barRect)
            {
                // Keep the rendered rectangle in place while ensuring horizontal
                // scaling contracts equally toward the center from both ends.
                float pivotOffset = 0.5f - barRect.pivot.x;
                barRect.anchoredPosition += new Vector2(pivotOffset * barRect.rect.width, 0f);
                barRect.pivot = new Vector2(0.5f, barRect.pivot.y);
                _fullScale = barRect.localScale;
            }
        }

        public void SetProgress(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (barRect)
                barRect.localScale = new Vector3(_fullScale.x * normalized, _fullScale.y, _fullScale.z);

            // Keep supporting a deliberately configured Filled Image or Slider.
            if (fillImage && fillImage.type == Image.Type.Filled)
                fillImage.fillAmount = normalized;
            if (slider) slider.normalizedValue = normalized;
        }
    }
}
