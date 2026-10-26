using Systems.InteractionSystem.Scripts;
using UnityEngine;
namespace StoryTell.MainIntegration
{
    public sealed class MapPickup : InteractableScript
    {
        [SerializeField] private MainPresentation presentation;
        protected override void OnInteract(){presentation.UnlockMap();gameObject.SetActive(false);}
    }
}
