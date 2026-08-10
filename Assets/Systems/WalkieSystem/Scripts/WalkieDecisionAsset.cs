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

        [SerializeField] private List<RadioDecisionChoice> choices = new();

        [Header("Timing")]
        [Tooltip("If true, the player can miss/ignore this call. A countdown starts the moment the call comes in.")]
        [SerializeField] private bool allowIgnore;

        [Tooltip("Seconds available to answer before it's treated as ignored. Only used if AllowIgnore is true.")]
        [SerializeField, Min(0.1f)] private float timeWindowSeconds = 15f;

        public string PromptText => promptText;
        public IReadOnlyList<RadioDecisionChoice> Choices => choices;
        public bool AllowIgnore => allowIgnore;
        public float TimeWindowSeconds => timeWindowSeconds;
    }

    [Serializable]
    public class RadioDecisionChoice
    {
        [SerializeField] private string label;

        [Tooltip("Optional inspector-driven hook. Fires in addition to the DecisionResolvedEvent on the bus.")]
        [SerializeField] private UnityEngine.Events.UnityEvent onSelected;

        public string Label => label;
        public void RaiseSelected() => onSelected?.Invoke();
    }
}