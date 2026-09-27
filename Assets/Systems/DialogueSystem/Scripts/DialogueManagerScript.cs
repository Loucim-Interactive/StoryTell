using System.Collections;
using DialogueSystem.Scripts;
using Systems.DecisionSystem.UI;
using Systems.EventSystem.Scripts;
using Systems.WalkieSystem.Scripts;
using UnityEngine;

namespace Systems.DialogueSystem.Scripts
{
    public class DialogueManagerScript : MonoBehaviour
    {
        public UIDialogueScript uiDialogueScript;
        [SerializeField] private DecisionManagerScript decisionManager;
        public static DialogueManagerScript Instance { get; private set; }
        public bool IsInConversation { get; private set; }

        private Coroutine _conversationRoutine;
        private Coroutine _thoughtRoutine;
        private Object _conversationOwner;
        private int _conversationVersion;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (!uiDialogueScript) uiDialogueScript = FindFirstObjectByType<UIDialogueScript>();
            if (!decisionManager) decisionManager = FindFirstObjectByType<DecisionManagerScript>();
            if (uiDialogueScript) uiDialogueScript.CleanTexts();
        }

        private void OnEnable() => GameEventBus.Subscribe<string>(GameplayEvents.StateThought, StateThought);

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<string>(GameplayEvents.StateThought, StateThought);
            CancelConversation();
            StopThought();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryStartConversation(ConversationSO conversation, Object owner = null)
        {
            if (!isActiveAndEnabled || IsInConversation || !conversation || !uiDialogueScript) return false;

            // Both systems share subtitles and choices. Let an active radio call finish first.
            WalkieInteractionMachine radio = FindFirstObjectByType<WalkieInteractionMachine>();
            if (radio && !radio.IsFinished) return false;

            if (!decisionManager) decisionManager = FindFirstObjectByType<DecisionManagerScript>();
            if (decisionManager && decisionManager.HasPresenter && !decisionManager.TryAcquire(this)) return false;

            StopThought();
            IsInConversation = true;
            _conversationOwner = owner;
            int version = ++_conversationVersion;
            _conversationRoutine = StartCoroutine(RunConversation(conversation, version));
            return true;
        }

        // Playback belongs to the manager even when an NPC yields this compatibility API.
        public IEnumerator PlayConversation(ConversationSO conversation)
        {
            if (!TryStartConversation(conversation)) yield break;
            int version = _conversationVersion;
            while (IsInConversation && version == _conversationVersion) yield return null;
        }

        private IEnumerator RunConversation(ConversationSO conversation, int version)
        {
            // Ensure the coroutine handle is stored even for an empty conversation.
            yield return null;
            try
            {
                int lineIndex = 0;
                while (conversation && conversation.Dialogues != null && lineIndex < conversation.Dialogues.Length)
                {
                    DialogueSO line = conversation.Dialogues[lineIndex++];
                    if (!line) continue;
                    yield return uiDialogueScript.DisplayDialogue(line);
                    if (line.Choices == null || line.Choices.Count == 0) continue;

                    var labels = new string[line.Choices.Count];
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (line.Choices[i] == null || string.IsNullOrWhiteSpace(line.Choices[i].Label))
                        {
                            Debug.LogWarning($"Dialogue '{line.name}' has an empty choice at index {i}.", line);
                            yield break;
                        }
                        labels[i] = line.Choices[i].Label;
                    }

                    int selected = -1;
                    if (!decisionManager || !decisionManager.Present(this, labels, index => selected = index))
                    {
                        Debug.LogWarning("NPC choices need an active DecisionManager and configured decision UI in the scene.", this);
                        yield break;
                    }

                    // Keep the spoken line visible while the player considers a response.
                    while (selected < 0)
                    {
                        if (!decisionManager || !decisionManager.IsOwnedBy(this) || !decisionManager.HasPresenter)
                            yield break;
                        yield return null;
                    }

                    DialogueChoice choice = line.Choices[selected];
                    choice.RaiseSelected();
                    if (!IsInConversation || version != _conversationVersion) yield break;

                    switch (choice.Outcome)
                    {
                        case DialogueChoiceOutcome.End:
                            yield break;
                        case DialogueChoiceOutcome.Branch:
                            if (!choice.NextConversation)
                            {
                                Debug.LogWarning($"Branch '{choice.Label}' has no destination conversation.", line);
                                yield break;
                            }
                            conversation = choice.NextConversation;
                            lineIndex = 0;
                            break;
                    }
                    // Give the presenter a frame to observe the resolved request.
                    yield return null;
                }
            }
            finally
            {
                if (version == _conversationVersion) FinishConversation();
            }
        }

        public void CancelConversation(Object owner = null)
        {
            if (owner && owner != _conversationOwner) return;
            if (_conversationRoutine != null) StopCoroutine(_conversationRoutine);
            if (IsInConversation) FinishConversation();
        }

        private void FinishConversation()
        {
            if (decisionManager) decisionManager.Release(this);
            IsInConversation = false;
            _conversationOwner = null;
            _conversationRoutine = null;
            if (uiDialogueScript) uiDialogueScript.StopDialogue();
        }

        public void StateThought(string thought) => StateThought(thought, "");

        public void StateThought(string thought, string caller)
        {
            if (IsInConversation || !uiDialogueScript || string.IsNullOrWhiteSpace(thought)) return;
            StopThought();
            _thoughtRoutine = StartCoroutine(PlayThought(thought, caller));
        }

        private IEnumerator PlayThought(string thought, string caller)
        {
            yield return uiDialogueScript.DisplayDialogue(thought, caller);
            uiDialogueScript.CleanTexts();
            _thoughtRoutine = null;
        }

        private void StopThought()
        {
            if (_thoughtRoutine == null) return;
            StopCoroutine(_thoughtRoutine);
            _thoughtRoutine = null;
            if (uiDialogueScript) uiDialogueScript.StopDialogue();
        }
    }
}
