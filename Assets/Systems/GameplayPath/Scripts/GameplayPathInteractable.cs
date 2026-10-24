using Systems.InteractionSystem.Scripts;
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
        protected override void OnInteract()
        {
            if (controller != null && controller.Interact(action, fragmentIndex, readingText) && hideAfterUse)
                gameObject.SetActive(false);
        }
    }
}
