using UnityEngine;

namespace StoryTell.Weapons
{
    [CreateAssetMenu(menuName = "StoryTell/Weapons/Weapon Definition", fileName = "WD_NewWeapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Service Rifle";

        [Header("Ballistics")]
        [SerializeField, Min(0f)] private float damage = 34f;
        [SerializeField, Min(1f)] private float range = 120f;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.18f;
        [SerializeField, Range(0f, 10f)] private float hipSpreadDegrees = 1.5f;
        [SerializeField, Range(0f, 10f)] private float aimSpreadDegrees = 0.25f;

        [Header("Magazine")]
        [SerializeField, Min(1)] private int magazineCapacity = 20;
        [SerializeField, Min(0)] private int maxCarriedMagazines = 4;
        [SerializeField, Min(0f)] private float reloadDuration = 2.2f;

        [Header("Handling")]
        [SerializeField, Range(1f, 179f)] private float aimFieldOfView = 50f;
        [SerializeField, Min(0.01f)] private float aimTransitionSpeed = 10f;
        [SerializeField, Min(0f)] private float clearJamDuration = 2.5f;

        public string DisplayName => displayName;
        public float Damage => damage;
        public float Range => range;
        public float FireInterval => fireInterval;
        public float HipSpreadDegrees => hipSpreadDegrees;
        public float AimSpreadDegrees => aimSpreadDegrees;
        public int MagazineCapacity => magazineCapacity;
        public int MaxCarriedMagazines => maxCarriedMagazines;
        public float ReloadDuration => reloadDuration;
        public float AimFieldOfView => aimFieldOfView;
        public float AimTransitionSpeed => aimTransitionSpeed;
        public float ClearJamDuration => clearJamDuration;

        public void ConfigureForTests(int capacity, int maxMagazines, float weaponDamage = 34f)
        {
            magazineCapacity = Mathf.Max(1, capacity);
            maxCarriedMagazines = Mathf.Max(0, maxMagazines);
            damage = Mathf.Max(0f, weaponDamage);
        }

        private void OnValidate()
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Weapon" : displayName.Trim();
            damage = Mathf.Max(0f, damage);
            range = Mathf.Max(1f, range);
            fireInterval = Mathf.Max(0.01f, fireInterval);
            magazineCapacity = Mathf.Max(1, magazineCapacity);
            maxCarriedMagazines = Mathf.Max(0, maxCarriedMagazines);
            reloadDuration = Mathf.Max(0f, reloadDuration);
            aimTransitionSpeed = Mathf.Max(0.01f, aimTransitionSpeed);
            clearJamDuration = Mathf.Max(0f, clearJamDuration);
        }
    }
}
