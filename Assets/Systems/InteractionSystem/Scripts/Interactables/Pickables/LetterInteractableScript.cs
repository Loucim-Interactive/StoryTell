using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.Pickables {
    public class LetterInteractableScript : InteractableScript, IInspectionTarget
    {
        [Header("Settings")]
        public bool fireEvents = true;
        [Tooltip("Collect this letter only after the player leaves inspection.")]
        public bool collectAfterInspection;

        protected override void OnInteract() {
            if (!fireEvents || (collectAfterInspection && InspectInteractable)) return;
            GameEventBus.Raise(GameplayEvents.PickupAction, this);
        }

        public void OnInspectionEnded() {
            if (fireEvents && collectAfterInspection)
                GameEventBus.Raise(GameplayEvents.PickupAction, this);
        }
    }
}
