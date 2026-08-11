using System;
using System.Collections.Generic;
using Systems.DecisionSystem;
using Systems.DecisionSystem.UI;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieUIScript : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup _root;
        [SerializeField] private GameObject indicatorPanel;
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private DecisionManagerScript decisionManager;

        [SerializeField] private GameObject choicePrefab;
        [SerializeField] private WalkieInteractionMachine _state;

        [Header("UI Settings")]
        [SerializeField] private float choicePaddingBottom = 1f;

        private List<WalkieDecisionButton> currentChoices = new List<WalkieDecisionButton>();
        private WalkieDecisionAsset _currentAsset;

        private void Start() {
            HideAll();
            if (!_state) _state = FindFirstObjectByType<WalkieInteractionMachine>();
            decisionManager = FindFirstObjectByType<DecisionManagerScript>();
        }

        public void Update() {
            if (_state.CurrentState == WalkieInteractionMachine.WalkieInteractionStates.Finished) {
                _root.alpha = 0;
                _root.interactable = false;
                return;
            }
            
            _root.alpha = 1;
            _root.interactable = true;
            
            if (_currentAsset != _state.CurrentAsset) SetupChoices(_state.CurrentAsset);
            
            switch (_state.CurrentState) {
                case WalkieInteractionMachine.WalkieInteractionStates.Awaiting:
                    HideAll();
                    ShowIndicator(true);
                    ShowTimer(true);
                    break;
                case WalkieInteractionMachine.WalkieInteractionStates.Choosing:
                    HideAll();
                    ShowChoices(true);
                    ShowTimer(true);
                    break;
                case WalkieInteractionMachine.WalkieInteractionStates.Talking:
                    HideAll();
                    ShowIndicator(true);
                    break;
            }

            HandleChoiceSelection();
        }

        public void ShowChoices(bool show) {
            choicesPanel.SetActive(show);
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
            _currentAsset = asset;

            decisionManager.SetAmountChoices(asset.Choices.Count);
            decisionManager.SetInitialChosen(0);

            foreach (var choice in asset.Choices)
            {
                GameObject view = Instantiate(choicePrefab, choicesPanel.transform);

                WalkieDecisionButton button = view.GetComponent<WalkieDecisionButton>();
                button.Setup(choice.Label);

                currentChoices.Add(button);
            }
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
