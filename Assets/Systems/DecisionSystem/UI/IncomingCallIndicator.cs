using Systems.EventSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.DecisionSystem.UI
{
    public class IncomingCallIndicator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private Image countdownFill; // optional

        private float localTimer;
        private float localDuration;
        private bool counting;

        private void OnEnable()
        {
            GameEventBus.Subscribe<DecisionPendingPayload>(DecisionEvents.Pending, OnPending);
            GameEventBus.Subscribe<DecisionChoicesReadyPayload>(DecisionEvents.ChoicesReady, OnChoicesReady);
            GameEventBus.Subscribe<WalkieDecisionAsset>(DecisionEvents.Ignored, OnIgnored);
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<DecisionPendingPayload>(DecisionEvents.Pending, OnPending);
            GameEventBus.Unsubscribe<DecisionChoicesReadyPayload>(DecisionEvents.ChoicesReady, OnChoicesReady);
            GameEventBus.Unsubscribe<WalkieDecisionAsset>(DecisionEvents.Ignored, OnIgnored);
        }

        private void Update()
        {
            if (!counting) return;
            localTimer = Mathf.Max(0f, localTimer - Time.deltaTime);
            if (countdownFill) countdownFill.fillAmount = localDuration > 0f ? localTimer / localDuration : 0f;
        }

        private void OnPending(DecisionPendingPayload p)
        {
            promptLabel.text = "Radio — press F to answer";
            counting = p.DurationSeconds > 0f;
            localDuration = p.DurationSeconds;
            localTimer = p.DurationSeconds;
            if (countdownFill) countdownFill.gameObject.SetActive(counting);
            SetVisible(true);
        }

        private void OnChoicesReady(DecisionChoicesReadyPayload p) => SetVisible(false);
        private void OnIgnored(WalkieDecisionAsset decision) => SetVisible(false);

        private void SetVisible(bool visible)
        {
            counting = counting && visible;
            root.alpha = visible ? 1f : 0f;
            root.blocksRaycasts = visible;
            root.interactable = visible;
        }
    }
}