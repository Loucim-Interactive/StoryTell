using Systems.InteractionSystem.Scripts;
using UnityEngine;

namespace StoryTell.Weapons
{
    public sealed class WeaponTrialPickup : InteractableScript
    {
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField] private bool ammunition;
        [SerializeField, Min(1)] private int magazineCount = 3;
        public bool IsAmmunition => ammunition;
        public bool TryCollect()
        {
            if (trial == null || !isActiveAndEnabled) return false;
            bool collected = ammunition ? trial.CollectAmmunition(magazineCount) : trial.CollectRifle();
            if (collected) gameObject.SetActive(false);
            return collected;
        }
        protected override void OnInteract() => TryCollect();
    }
}
