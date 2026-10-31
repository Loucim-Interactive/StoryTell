using Systems.Missions;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
namespace StoryTell.MainIntegration
{
    public sealed class MissionAnnouncement : MonoBehaviour
    {
        [SerializeField] private MissionsUIManager missions;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        private readonly Queue<string> pending = new();
        private float remaining;
        public string CurrentMessage => label ? label.text : "";
        public void Clear(){pending.Clear();remaining=0;label.text="";panel.SetActive(false);}
        public void ShowMessage(string message){if(!string.IsNullOrWhiteSpace(message)&&pending.Count<8)pending.Enqueue(message);}
        public void ShowPickup(string message){if(string.IsNullOrWhiteSpace(message))return;label.text=message;remaining=2.5f;}
        private void LateUpdate()
        {
            bool visible=missions&&missions.IsVisible;
            if(visible&&remaining<=0&&pending.Count>0){label.text=pending.Dequeue();remaining=3.5f;}
            panel.SetActive(visible&&remaining>0);
            if(visible&&remaining>0)remaining-=Time.unscaledDeltaTime;
        }
    }
}
