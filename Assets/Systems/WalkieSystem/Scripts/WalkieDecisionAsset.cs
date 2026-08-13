using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.DecisionSystem
{
    [CreateAssetMenu(menuName = "Systems/Decision System/Walkie Decision", fileName = "NewWalkieDecision")]
    public class WalkieDecisionAsset : ScriptableObject
    {
        [Header("Content")]
        [Tooltip("The line the caller says. Shown above the choice list.")]
        [SerializeField] private string promptText;
        [SerializeField] private string callerName;

        [Tooltip("Voice line played when the call starts. Choices become available after it finishes.")]
        [SerializeField] private AudioClip callerVoiceClip;

        [Tooltip("Minimum seconds the caller prompt remains active before response choices appear. Used as the speaking duration when no voice clip is assigned.")]
        [SerializeField, Min(0f)] private float responseDelaySeconds = 2f;

        [SerializeField] private List<RadioDecisionChoice> choices = new();

        [Header("Timing")]
        [Tooltip("If true, the player can miss/ignore this call. A countdown starts the moment the call comes in.")]
        [SerializeField] private bool allowIgnore;

        [Tooltip("Seconds available to answer before it's treated as ignored. Only used if AllowIgnore is true.")]
        [SerializeField, Min(0.1f)] private float timeWindowSeconds = 15f;

        [Tooltip("Optional inspector-driven hook fired when a timed decision is ignored.")]
        [SerializeField] private UnityEngine.Events.UnityEvent onIgnored;

        public string CallerName => callerName;
        public string PromptText => promptText;
        public AudioClip CallerVoiceClip => callerVoiceClip;
        public float ResponseDelaySeconds => responseDelaySeconds;
        public IReadOnlyList<RadioDecisionChoice> Choices => choices;
        public bool AllowIgnore => allowIgnore;
        public float TimeWindowSeconds => timeWindowSeconds;
        public void RaiseIgnored() => onIgnored?.Invoke();
    }

    [Serializable]
    public class RadioDecisionChoice
    {
        [SerializeField] private string label;
        [SerializeField] private WalkieDecisionAsset nextDecision;

        [Tooltip("Optional inspector-driven hook. Fires in addition to the DecisionResolvedEvent on the bus.")]
        [SerializeField] private UnityEngine.Events.UnityEvent onSelected;

        public WalkieDecisionAsset NextAsset => nextDecision;
        public string Label => label;
        public void RaiseSelected() => onSelected?.Invoke();
    }
}
