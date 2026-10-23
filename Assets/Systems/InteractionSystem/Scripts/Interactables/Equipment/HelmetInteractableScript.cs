using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.Equipment {
    public class HelmetInteractableScript : InteractableScript
    {
        [Header("Settings")]
        public bool fireEvents = true;

        protected override void OnInteract() {
            if (!fireEvents) return;
            GameEventBus.Raise(GameplayEvents.EquipAction);
            gameObject.SetActive(false);
        }
    }
}
