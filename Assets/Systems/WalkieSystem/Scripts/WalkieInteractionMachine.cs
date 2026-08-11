using System;
using Systems.DecisionSystem;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieInteractionMachine : MonoBehaviour
    {
        
        private WalkieInteractionStates _currentState = WalkieInteractionStates.Finished;
        private WalkieDecisionAsset _currentAsset;

        public enum WalkieInteractionStates {
            Awaiting,
            Choosing,
            Talking,
            Finished
        }
        
        public bool IsAwaiting => _currentState == WalkieInteractionStates.Awaiting;
        public bool IsChoosing => _currentState == WalkieInteractionStates.Choosing;
        public bool IsFinished => _currentState == WalkieInteractionStates.Finished;
        public bool IsTalking => _currentState == WalkieInteractionStates.Talking;

        public WalkieInteractionStates CurrentState => _currentState;
        public WalkieInteractionStates PreviousState { get; private set; }
        public WalkieDecisionAsset CurrentAsset => _currentAsset;

        private void OnEnable() => GameEventBus.Subscribe<WalkieDecisionAsset>(GameplayEvents.WalkieTalkieTrigger, SetAsset);
        private void OnDisable() => GameEventBus.Unsubscribe<WalkieDecisionAsset>(GameplayEvents.WalkieTalkieTrigger, SetAsset);

        public void SwitchState(WalkieInteractionStates newState) {
            PreviousState = _currentState;
            _currentState = newState;
        }

        private void SetAsset(WalkieDecisionAsset asset) => _currentAsset = asset;
    }
}
