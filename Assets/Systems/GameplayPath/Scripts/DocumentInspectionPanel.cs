using StoryTell.PhysicalMap;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StoryTell.GameplayPath
{
    public sealed class DocumentInspectionPanel : MonoBehaviour
    {
        [SerializeField] private InspectionScript inspection;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text title, body;
        [SerializeField] private ScrollRect scroll;
        private InputAction scrollAction;
        private bool showing, mapWasEnabled;
        private InspectableDocument current;
        public bool IsVisible => showing;
        public TMP_Text Body => body;

        private void OnEnable()
        {
            scrollAction ??= new InputAction("ScrollDocument", InputActionType.Value, "<Mouse>/scroll/y");
            scrollAction.Enable();
            GameEventBus.Subscribe<GameObject>(GameplayEvents.StartInspection, Show);
            GameEventBus.Subscribe(GameplayEvents.EndInspection, Hide);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<GameObject>(GameplayEvents.StartInspection, Show);
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, Hide);
            scrollAction?.Disable();
            Hide();
        }

        private void OnDestroy() { scrollAction?.Dispose(); }

        private void Show(GameObject target)
        {
            if (inspection == null || !inspection.IsInspecting || inspection.CurrentTarget != target) return;
            var document = target.GetComponent<InspectableDocument>();
            if (document == null) return;
            if (!showing) mapWasEnabled = map != null && map.enabled;
            showing = true;
            current = document;
            if (map != null) { map.SetOpen(false, true); map.enabled = false; }
            title.text = document.title;
            body.text = document.transcription;
            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
            scroll.verticalNormalizedPosition = 1;
        }

        private void Update()
        {
            if (!showing) return;
            if (inspection == null || !inspection.IsInspecting) { Hide(); return; }
            float delta = scrollAction.ReadValue<float>();
            float distance = scroll.content.rect.height - scroll.viewport.rect.height;
            if (distance > 0 && Mathf.Abs(delta) > 0.01f)
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + delta / 120f * 90f / distance);
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (!showing) return;
            showing = false;
            if (map != null) map.enabled = mapWasEnabled;
            if (current != null && current.hideAfterInspection) current.gameObject.SetActive(false);
            current = null;
        }
    }
}
