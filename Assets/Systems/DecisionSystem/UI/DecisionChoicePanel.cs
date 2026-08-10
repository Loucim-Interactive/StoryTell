using System.Collections.Generic;
using Systems.DecisionSystem.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.EventSystem.Scripts;

namespace Systems.DecisionSystem
{
    public class DecisionChoicePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Transform choiceListContainer;
        [SerializeField] private DecisionChoiceButton choiceButtonPrefab;
        [SerializeField] private Image countdownFill; // optional

        private readonly List<DecisionChoiceButton> _spawned = new();
        private float _localTimer;
        private float _localDuration;
        private bool _counting;

        private void OnEnable()
        {
            GameEventBus.Subscribe<DecisionChoicesReadyPayload>(DecisionEvents.ChoicesReady, OnReady);
            GameEventBus.Subscribe(DecisionEvents.ChoicesClosed, OnClosed);
            GameEventBus.Subscribe<DecisionResolvedPayload>(DecisionEvents.Resolved, OnResolved);
            GameEventBus.Subscribe<WalkieDecisionAsset>(DecisionEvents.Ignored, OnIgnored);
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<DecisionChoicesReadyPayload>(DecisionEvents.ChoicesReady, OnReady);
            GameEventBus.Unsubscribe(DecisionEvents.ChoicesClosed, OnClosed);
            GameEventBus.Unsubscribe<DecisionResolvedPayload>(DecisionEvents.Resolved, OnResolved);
            GameEventBus.Unsubscribe<WalkieDecisionAsset>(DecisionEvents.Ignored, OnIgnored);
        }

        private void Update()
        {
            if (!_counting) return;
            _localTimer = Mathf.Max(0f, _localTimer - Time.deltaTime);
            if (countdownFill) countdownFill.fillAmount = _localDuration > 0f ? _localTimer / _localDuration : 0f;
        }

        private void OnReady(DecisionChoicesReadyPayload p)
        {
            promptText.text = p.Decision.PromptText;
            BuildChoices(p.Decision);

            _counting = p.RemainingSeconds > 0f;
            _localDuration = p.RemainingSeconds;
            _localTimer = p.RemainingSeconds;
            if (countdownFill) countdownFill.gameObject.SetActive(_counting);

            SetVisible(true);
        }

        private void OnClosed() => SetVisible(false);
        private void OnResolved(DecisionResolvedPayload p) => SetVisible(false);
        private void OnIgnored(WalkieDecisionAsset decision) => SetVisible(false);

        private void BuildChoices(WalkieDecisionAsset decision)
        {
            ClearChoices();
            for (int i = 0; i < decision.Choices.Count; i++)
            {
                var view = Instantiate(choiceButtonPrefab, choiceListContainer);
                int index = i;
                view.Setup(decision.Choices[i].Label,
                    () => GameEventBus.Raise(DecisionEvents.ChoiceSelected, index));
                _spawned.Add(view);
            }
        }

        private void ClearChoices()
        {
            foreach (var view in _spawned) Destroy(view.gameObject);
            _spawned.Clear();
        }

        private void SetVisible(bool visible)
        {
            _counting = _counting && visible;
            root.alpha = visible ? 1f : 0f;
            root.blocksRaycasts = visible;
            root.interactable = visible;
            if (!visible) ClearChoices();
        }
    }
}