using Systems.Missions;
using TMPro;
using UnityEngine;
namespace StoryTell.MainIntegration
{
    public sealed class MissionAnnouncement : MonoBehaviour
    {
        [SerializeField] private MissionsUIManager missions;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        private float until;
        private void OnEnable(){missions.MissionAdded+=Show;}
        private void OnDisable(){if(missions)missions.MissionAdded-=Show;}
        private void Show(string title){label.text="NUEVA MISIÓN · "+title;until=Time.unscaledTime+7;}
        private void LateUpdate(){panel.SetActive(Time.unscaledTime<until&&missions.IsVisible);}
    }
}
