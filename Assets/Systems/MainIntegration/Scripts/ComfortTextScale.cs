using TMPro;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ComfortTextScale : MonoBehaviour
    {
        [SerializeField] private float originalSize=24;
        private TMP_Text label;
        private void OnEnable(){label=GetComponent<TMP_Text>();ComfortSettings.Changed+=Apply;Apply();}
        private void OnDisable(){ComfortSettings.Changed-=Apply;}
        private void Apply(){if(label)label.fontSize=originalSize*ComfortSettings.Current.textSize;}
    }
}
