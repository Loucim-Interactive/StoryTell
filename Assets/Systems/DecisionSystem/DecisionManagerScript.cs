using UnityEngine;
using Systems.EventSystem.Scripts;
using Systems.WalkieSystem.Scripts;

namespace Systems.DecisionSystem
{
    public enum DecisionState { Idle, Pending, AwaitingChoice }

    public class DecisionManager : MonoBehaviour
    {
        private DecisionState state = DecisionState.Idle;
        private WalkieDecisionAsset currentDecision;
        private readonly DecisionTimer timer = new();
        private bool radioIsVisible;

        private void OnEnable()
        {
            GameEventBus.Subscribe<WalkieDecisionAsset>(DecisionEvents.Requested, OnDecisionRequested);
            GameEventBus.Subscribe<bool>(RadioEvents.VisibilityChanged, OnRadioVisibilityChanged);
            GameEventBus.Subscribe<int>(DecisionEvents.ChoiceSelected, OnChoiceSelected);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<WalkieDecisionAsset>(DecisionEvents.Requested, OnDecisionRequested);
            GameEventBus.Unsubscribe<bool>(RadioEvents.VisibilityChanged, OnRadioVisibilityChanged);
            GameEventBus.Unsubscribe<int>(DecisionEvents.ChoiceSelected, OnChoiceSelected);
        }

        private void Update()
        {
            if (!timer.IsRunning) return;
            timer.Tick(Time.deltaTime);
            if (timer.IsExpired) ResolveIgnored();
        }

        private void OnDecisionRequested(WalkieDecisionAsset decision)
        {
            if (state != DecisionState.Idle)
            {
                Debug.LogWarning($"[DecisionSystem] New decision requested while '{currentDecision?.name}' is still active. Ignoring.", this);
                return;
            }

            currentDecision = decision;
            state = DecisionState.Pending;

            if (currentDecision.AllowIgnore)
                timer.Start(currentDecision.TimeWindowSeconds);
            else
                timer.Stop();

            GameEventBus.Raise(DecisionEvents.Pending,
                new DecisionPendingPayload(currentDecision, currentDecision.AllowIgnore ? currentDecision.TimeWindowSeconds : 0f));

            TryShowChoices();
        }

        private void OnRadioVisibilityChanged(bool isVisible)
        {
            radioIsVisible = isVisible;

            if (radioIsVisible)
            {
                TryShowChoices();
            }
            else if (state == DecisionState.AwaitingChoice)
            {
                state = DecisionState.Pending;
                GameEventBus.Raise(DecisionEvents.ChoicesClosed);
            }
        }

        private void TryShowChoices()
        {
            if (currentDecision == null || !radioIsVisible || state == DecisionState.AwaitingChoice) return;

            state = DecisionState.AwaitingChoice;
            GameEventBus.Raise(DecisionEvents.ChoicesReady,
                new DecisionChoicesReadyPayload(currentDecision, timer.IsRunning ? timer.Remaining : 0f));
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (state != DecisionState.AwaitingChoice || currentDecision == null) return;
            if (choiceIndex < 0 || choiceIndex >= currentDecision.Choices.Count) return;

            var choice = currentDecision.Choices[choiceIndex];
            choice.RaiseSelected();
            GameEventBus.Raise(DecisionEvents.Resolved, new DecisionResolvedPayload(currentDecision, choice, choiceIndex));
            Reset();
        }

        private void ResolveIgnored()
        {
            GameEventBus.Raise(DecisionEvents.Ignored, currentDecision);
            Reset();
        }

        private void Reset()
        {
            timer.Stop();
            currentDecision = null;
            state = DecisionState.Idle;
        }
    }
}