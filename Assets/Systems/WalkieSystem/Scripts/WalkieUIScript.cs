using System.Collections.Generic;
using Systems.DecisionSystem;
using Systems.DecisionSystem.UI;
using UnityEngine;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieUIScript : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup _root;
        [SerializeField] private GameObject indicatorPanel;
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private DecisionManagerScript decisionManager;
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private GameObject choicesContent;
        [Tooltip("Root containing the choice sidebar, walkie icon, and choice content.")]
        [SerializeField] private GameObject choicesPanelRoot;

        [SerializeField] private GameObject choicePrefab;
        [SerializeField] private WalkieInteractionMachine _state;
        [SerializeField] private WalkieTalkieScript walkieTalkie;
        [SerializeField] private WalkieTimer timer;

        [Header("UI Settings")]
        [SerializeField] private float choicePaddingBottom = 1f;

        private List<WalkieDecisionButton> currentChoices = new List<WalkieDecisionButton>();
        private WalkieDecisionAsset _currentAsset;

        private void Start() {
            if (!_state) _state = FindFirstObjectByType<WalkieInteractionMachine>();
            if (!decisionManager) decisionManager = FindFirstObjectByType<DecisionManagerScript>();
            if (!walkieTalkie) walkieTalkie = FindFirstObjectByType<WalkieTalkieScript>();
            if (!timer && timerPanel) timer = timerPanel.GetComponentInChildren<WalkieTimer>(true);
            if (!timer && timerPanel) timer = timerPanel.AddComponent<WalkieTimer>();
            if (!choicesContent) choicesContent = choicesPanel;
            if (!choicesPanelRoot && choicesPanel)
                choicesPanelRoot = choicesPanel.transform.parent
                    ? choicesPanel.transform.parent.gameObject
                    : choicesPanel;
            HideAll();
        }

        public void Update() {
            if (!_state || !decisionManager || !walkieTalkie) {
                SetRootVisible(false);
                return;
            }

            bool interactionActive = !_state.IsFinished && _state.CurrentAsset != null;
            if (!interactionActive) {
                decisionManager.SetChoosing(false);
                HideAll();
                SetRootVisible(false);
                if (_currentAsset != null) ClearChoices();
                return;
            }

            SetRootVisible(true);

            if (_currentAsset != _state.CurrentAsset) SetupChoices(_state.CurrentAsset);

            bool responseNeeded = _state.IsChoosing;
            bool showChoices = responseNeeded && walkieTalkie.IsEquipped;
            bool showIndicator = (_state.IsTalking || responseNeeded) && !walkieTalkie.IsEquipped;
            bool showTimer = _state.HasTimedResponse;

            ShowIndicator(showIndicator);
            ShowChoices(showChoices);
            ShowTimer(showTimer);
            decisionManager.SetChoosing(showChoices);

            if (timer && showTimer) timer.SetProgress(_state.TimeNormalized);
            HandleChoiceSelection();
        }

        private void SetRootVisible(bool visible)
        {
            if (!_root) return;
            _root.alpha = visible ? 1f : 0f;
            _root.interactable = visible;
            _root.blocksRaycasts = visible;
        }

        public void ShowChoices(bool show) {
            GameObject root = choicesPanelRoot ? choicesPanelRoot : choicesPanel;
            if (root) root.SetActive(show);
        }

        public void ShowIndicator(bool show) {
            indicatorPanel.SetActive(show);
        }

        public void ShowTimer(bool show) {
            timerPanel.SetActive(show);
        }

        private void HideAll() {
            ShowTimer(false);
            ShowIndicator(false);
            ShowChoices(false);
        }

        private void SetupChoices(WalkieDecisionAsset asset)
        {
            ClearChoices();
            if (!asset) return;
            _currentAsset = asset;

            decisionManager.SetAmountChoices(asset.Choices.Count);
            decisionManager.SetInitialChosen(0);

            foreach (var choice in asset.Choices)
            {
                GameObject view = Instantiate(choicePrefab, choicesContent.transform);

                WalkieDecisionButton button = view.GetComponent<WalkieDecisionButton>();
                button.Setup(choice.Label);

                currentChoices.Add(button);
            }

            if (currentChoices.Count > 0) currentChoices[0].SetSelected(true);
        }

        private void ClearChoices()
        {
            foreach (WalkieDecisionButton choice in currentChoices)
                if (choice) Destroy(choice.gameObject);

            currentChoices.Clear();
            _currentAsset = null;
            if (decisionManager) decisionManager.SetAmountChoices(0);
        }

        private void HandleChoiceSelection() {
            if (decisionManager.CurrentIndex == decisionManager.PreviousIndex) return;
            int counter = 0;
            foreach (var choice in currentChoices) {
                if (counter == decisionManager.CurrentIndex) choice.SetSelected(true);
                else choice.SetSelected(false);
                counter++;
            }
        }
    }
}
