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
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.2f;
        [SerializeField] private float indicatorSlideDistance = 18f;
        [SerializeField] private float choicesSlideDistance = 28f;
        [SerializeField] private float timerSlideDistance = 14f;

        private List<WalkieDecisionButton> currentChoices = new List<WalkieDecisionButton>();
        private WalkieDecisionAsset _currentAsset;
        private WalkieUIPanelTransition _indicatorTransition;
        private WalkieUIPanelTransition _choicesTransition;
        private WalkieUIPanelTransition _timerTransition;

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

            _indicatorTransition = CreateTransition(indicatorPanel, new Vector2(indicatorSlideDistance, 0f));
            _choicesTransition = CreateTransition(choicesPanelRoot, new Vector2(-choicesSlideDistance, 0f));
            _timerTransition = CreateTransition(timerPanel, new Vector2(0f, -timerSlideDistance));
            HideAll(true);
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

            bool responseNeeded = _state.IsChoosing;
            if (responseNeeded && _currentAsset != _state.CurrentAsset)
                SetupChoices(_state.CurrentAsset);

            bool showChoices = responseNeeded && walkieTalkie.IsEquipped;
            // Talking is a deliberate quiet phase between conversation steps.
            // The next interaction UI animates back only when a response is ready.
            bool showIndicator = responseNeeded && !walkieTalkie.IsEquipped;
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
            if (_choicesTransition) _choicesTransition.SetVisible(show);
            else if (root) root.SetActive(show);
        }

        public void ShowIndicator(bool show) {
            if (_indicatorTransition) _indicatorTransition.SetVisible(show);
            else if (indicatorPanel) indicatorPanel.SetActive(show);
        }

        public void ShowTimer(bool show) {
            if (_timerTransition) _timerTransition.SetVisible(show);
            else if (timerPanel) timerPanel.SetActive(show);
        }

        private void HideAll(bool immediate = false) {
            if (immediate)
            {
                if (_timerTransition) _timerTransition.SetVisible(false, true);
                if (_indicatorTransition) _indicatorTransition.SetVisible(false, true);
                if (_choicesTransition) _choicesTransition.SetVisible(false, true);
                return;
            }

            ShowTimer(false);
            ShowIndicator(false);
            ShowChoices(false);
        }

        private WalkieUIPanelTransition CreateTransition(GameObject panel, Vector2 hiddenOffset)
        {
            if (!panel) return null;

            WalkieUIPanelTransition transition = panel.GetComponent<WalkieUIPanelTransition>();
            if (!transition) transition = panel.AddComponent<WalkieUIPanelTransition>();
            transition.Initialize(hiddenOffset, transitionDuration);
            return transition;
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

    // Presentation-only transition. It never owns or mutates walkie interaction state.
    public class WalkieUIPanelTransition : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _shownPosition;
        private Vector2 _hiddenOffset;
        private float _duration;
        private float _elapsed;
        private float _startAlpha;
        private float _targetAlpha;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private bool _targetVisible;
        private bool _initialized;
        private bool _animating;

        public void Initialize(Vector2 hiddenOffset, float duration)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (!_canvasGroup) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _rectTransform = transform as RectTransform;
            _shownPosition = _rectTransform ? _rectTransform.anchoredPosition : Vector2.zero;
            _hiddenOffset = hiddenOffset;
            _duration = Mathf.Max(0.05f, duration);
            _targetVisible = gameObject.activeSelf;
            _initialized = true;
        }

        public void SetVisible(bool visible, bool immediate = false)
        {
            if (!_initialized || (_targetVisible == visible && !immediate)) return;

            _targetVisible = visible;
            if (visible && !gameObject.activeSelf) gameObject.SetActive(true);

            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            _targetAlpha = visible ? 1f : 0f;
            _targetPosition = _shownPosition + (visible ? Vector2.zero : _hiddenOffset);

            if (immediate)
            {
                _canvasGroup.alpha = _targetAlpha;
                if (_rectTransform) _rectTransform.anchoredPosition = _targetPosition;
                _animating = false;
                if (!visible) gameObject.SetActive(false);
                return;
            }

            _startAlpha = _canvasGroup.alpha;
            _startPosition = _rectTransform ? _rectTransform.anchoredPosition : Vector2.zero;
            _elapsed = 0f;
            _animating = true;
        }

        private void Update()
        {
            if (!_animating) return;

            _elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            float eased = progress * progress * (3f - 2f * progress);
            _canvasGroup.alpha = Mathf.Lerp(_startAlpha, _targetAlpha, eased);
            if (_rectTransform)
                _rectTransform.anchoredPosition = Vector2.Lerp(_startPosition, _targetPosition, eased);

            if (progress < 1f) return;

            _animating = false;
            if (!_targetVisible) gameObject.SetActive(false);
        }
    }
}
