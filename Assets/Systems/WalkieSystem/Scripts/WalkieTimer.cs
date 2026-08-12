using UnityEngine;
using UnityEngine.UI;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieTimer : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider slider;

        private void Awake()
        {
            if (!fillImage) fillImage = GetComponent<Image>();
            if (!slider) slider = GetComponent<Slider>();
        }

        public void SetProgress(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (fillImage) fillImage.fillAmount = normalized;
            if (slider) slider.normalizedValue = normalized;
        }
    }
}
