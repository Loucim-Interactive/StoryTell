using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.Equipment {
    public class EquipmentInteractableScript : InteractableScript, IInspectionTarget
    {
        [Header("Settings")]
        public bool fireEvents = true;
        [Tooltip("Equip only after the player finishes or interrupts inspection.")]
        public bool equipAfterInspection;

        protected override void OnInteract() {
            if (!fireEvents || (equipAfterInspection && InspectInteractable)) return;
            Equip();
        }

        public void OnInspectionEnded() {
            if (fireEvents && equipAfterInspection) Equip();
        }

        private void Equip() {
            GameEventBus.Raise(GameplayEvents.PickupAction, this);
            gameObject.SetActive(false);
        }
    }
}
