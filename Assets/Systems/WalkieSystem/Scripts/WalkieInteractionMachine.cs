using System;
using System.Collections;
using Systems.DecisionSystem;
using Systems.DialogueSystem.Scripts;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieInteractionMachine : MonoBehaviour
    {
        
        private WalkieInteractionStates _currentState = WalkieInteractionStates.Finished;
        [SerializeField] private AudioSource callerAudioSource;
        [SerializeField] private DialogueManagerScript dialogueManager;

        private WalkieDecisionAsset _currentAsset;
        private Coroutine _callerRoutine;
        private float _timeRemaining;

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
        public float TimeRemaining => _timeRemaining;
        public float TimeNormalized => _currentAsset != null && _currentAsset.AllowIgnore
            ? Mathf.Clamp01(_timeRemaining / _currentAsset.TimeWindowSeconds)
            : 0f;
        public bool HasTimedResponse => IsChoosing && _currentAsset != null && _currentAsset.AllowIgnore;

        public event Action<WalkieInteractionStates> StateChanged;
        public event Action<WalkieDecisionAsset, int> Resolved;

        private void Awake()
        {
            if (!callerAudioSource)
                callerAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            if (!dialogueManager)
                dialogueManager = FindFirstObjectByType<DialogueManagerScript>();
        }

        private void OnEnable() => GameEventBus.Subscribe<WalkieDecisionAsset>(GameplayEvents.WalkieTalkieTrigger, BeginInteraction);
        private void OnDisable()
        {
            GameEventBus.Unsubscribe<WalkieDecisionAsset>(GameplayEvents.WalkieTalkieTrigger, BeginInteraction);
            if (_callerRoutine != null) StopCoroutine(_callerRoutine);
        }

        private void Update()
        {
            if (!HasTimedResponse) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - Time.deltaTime);
            if (_timeRemaining <= 0f)
                Resolve(-1);
        }

        public void SwitchState(WalkieInteractionStates newState) {
            if (_currentState == newState) return;
            PreviousState = _currentState;
            _currentState = newState;
            Debug.Log("New walkie state: " + newState);
            StateChanged?.Invoke(newState);
        }

        public void Resolve(int choiceIndex)
        {
            if (!IsChoosing || !_currentAsset) return;
            RadioDecisionChoice choice = null;

            WalkieDecisionAsset resolvedAsset = _currentAsset;
            if (choiceIndex >= 0 && choiceIndex < resolvedAsset.Choices.Count) {
                choice = resolvedAsset.Choices[choiceIndex];
                choice.RaiseSelected();
            }
            else
            {
                choiceIndex = -1;
                resolvedAsset.RaiseIgnored();
            }

            SwitchState(WalkieInteractionStates.Finished);
            Resolved?.Invoke(resolvedAsset, choiceIndex);

            if (choice != null && choice.NextAsset ) {
                BeginInteraction(choice.NextAsset); // after choice is resolved, if next asset, go begin that. (and this loops)
            }
            else {
                _currentAsset = null;
                _timeRemaining = 0f;
            }
        }

        private void BeginInteraction(WalkieDecisionAsset asset)
        {
            if (asset == null || !IsFinished)
            {
                if (asset == null) Debug.LogWarning("Walkie trigger has no decision asset.", this);
                else Debug.LogWarning("Ignoring walkie trigger while another interaction is active.", this);
                return;
            }

            _currentAsset = asset;
            _callerRoutine = StartCoroutine(PlayCallerThenRequestResponse(asset));
        }

        private IEnumerator PlayCallerThenRequestResponse(WalkieDecisionAsset asset)
        {
            SwitchState(WalkieInteractionStates.Talking);

            if (!string.IsNullOrWhiteSpace(asset.PromptText) && dialogueManager)
                dialogueManager.StateThought(asset.PromptText, asset.CallerName); // Temporary prompt presentation until walkie dialogue has its own view.
        
            float elapsed = 0f;
            if (asset.CallerVoiceClip && callerAudioSource)
            {
                callerAudioSource.clip = asset.CallerVoiceClip;
                callerAudioSource.Play();
            }

            // Do not expose choices until both the authored reading delay and any
            // available caller audio have completed.
            while (elapsed < asset.ResponseDelaySeconds ||
                   (callerAudioSource && callerAudioSource.isPlaying))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _callerRoutine = null;
            if (_currentAsset != asset) yield break;

            _timeRemaining = asset.AllowIgnore ? asset.TimeWindowSeconds : 0f;
            SwitchState(WalkieInteractionStates.Choosing);
        }
    }
}
