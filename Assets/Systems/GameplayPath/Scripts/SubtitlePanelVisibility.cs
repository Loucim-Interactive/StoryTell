using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTell.GameplayPath
{
    public sealed class SubtitlePanelVisibility : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text narration, speaker;
        private void LateUpdate()
        {
            if (background != null)
                background.enabled = !string.IsNullOrWhiteSpace(narration?.text) || !string.IsNullOrWhiteSpace(speaker?.text);
        }
    }
}
