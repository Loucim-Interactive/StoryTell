using TMPro;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public sealed class SaveStatusToast:MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UiSessionCoordinator session;
        private float remaining,nextSavedNotice;
        public string Message=>label.text;
        public bool IsVisible=>panel.activeSelf;
        public void Show(bool success)
        {
            if(success&&Time.unscaledTime<nextSavedNotice)return;
            label.text=success?"Checkpoint guardado":"No se pudo guardar";remaining=success?2:4;
            if(success)nextSavedNotice=Time.unscaledTime+10;
        }
        public void Clear(){remaining=0;panel.SetActive(false);}
        private void LateUpdate(){bool visible=remaining>0&&(!session||session.Mode==UiMode.None);panel.SetActive(visible);if(visible)remaining-=Time.unscaledDeltaTime;}
        private void OnDisable(){if(panel)panel.SetActive(false);}
    }
}
