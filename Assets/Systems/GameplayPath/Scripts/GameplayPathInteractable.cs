using Systems.InteractionSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.EventSystem.Scripts;
using UnityEngine;
namespace StoryTell.GameplayPath
{
    public sealed class GameplayPathInteractable : InteractableScript
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private PathAction action;
        [SerializeField] private int fragmentIndex;
        [SerializeField] private bool hideAfterUse;
        [SerializeField, TextArea(3, 12)] private string readingText;
        public PathAction Action => action;
        public int FragmentIndex => fragmentIndex;
        public GameplayPathController Controller => controller;
        public override bool UsesExternalPrompt => true;
        private bool hideOnInspectionEnd;
        protected override void OnEnable()
        {
            base.OnEnable();
            GameEventBus.Subscribe(GameplayEvents.EndInspection, EndDocumentInspection);
        }
        protected override void OnDisable()
        {
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, EndDocumentInspection);
            base.OnDisable();
        }
        private void EndDocumentInspection()
        {
            if (!hideOnInspectionEnd) return;
            hideOnInspectionEnd = false;
            gameObject.SetActive(false);
        }
        protected override void OnInteract()
        {
            if (action == PathAction.Read && GetComponent<InspectableDocument>() != null) return;
            if (action == PathAction.Fragment && GetComponent<InspectableDocument>() != null)
            {
                hideOnInspectionEnd = controller != null && controller.Interact(action, fragmentIndex, readingText) && hideAfterUse;
                return;
            }
            if (controller != null && controller.Interact(action, fragmentIndex, readingText) && hideAfterUse)
                gameObject.SetActive(false);
        }
    }
}
