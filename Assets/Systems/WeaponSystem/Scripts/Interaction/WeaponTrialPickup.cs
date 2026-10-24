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
        public override bool UsesExternalPrompt => true;
        public bool TryCollect()
        {
            if (trial == null || !isActiveAndEnabled) return false;
            if (!ammunition)
            {
                if (!trial.CollectRifle()) return false;
                gameObject.SetActive(false); return true;
            }
            int added = trial.CollectMagazineCount(magazineCount);
            if (added <= 0) return false;
            magazineCount -= added;
            if (magazineCount == 0) gameObject.SetActive(false);
            return true;
        }
        protected override void OnInteract() => TryCollect();
    }
}
