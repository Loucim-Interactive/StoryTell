using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponSequenceController : MonoBehaviour
    {
        [SerializeField] private WeaponController weapon;
        [SerializeField] private bool beginOnStart;
        [SerializeField] private int startingLoadedRounds = -1;
        [SerializeField, Min(0)] private int startingReserveMagazines = 2;
        [SerializeField] private bool startsClean = true;
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onSequenceEnded;

        public bool IsSequenceActive { get; private set; }

        public void Configure(
            WeaponController controller,
            int loadedRounds = -1,
            int reserveMagazines = 2,
            bool cleanWeapon = true)
        {
            weapon = controller;
            startingLoadedRounds = loadedRounds;
            startingReserveMagazines = Mathf.Max(0, reserveMagazines);
            startsClean = cleanWeapon;
        }

        private void Awake()
        {
            if (weapon == null)
                weapon = FindAnyObjectByType<WeaponController>();
        }

        private void Start()
        {
            if (beginOnStart)
                BeginSequence();
        }

        public void BeginSequence()
        {
            if (weapon == null || IsSequenceActive)
                return;

            IsSequenceActive = true;
            weapon.BeginSequence(startingLoadedRounds, startingReserveMagazines, startsClean);
            onSequenceStarted?.Invoke();
        }

        public void EndSequence()
        {
            if (weapon == null || !IsSequenceActive)
                return;

            IsSequenceActive = false;
            weapon.EndSequence();
            onSequenceEnded?.Invoke();
        }

        public void ForceNextShotToJam()
        {
            weapon?.ForceNextJam();
        }

        public void ApplyWater()
        {
            weapon?.ApplyContamination(WeaponContamination.Water);
        }

        public void ApplySand()
        {
            weapon?.ApplyContamination(WeaponContamination.Sand);
        }

        public void ApplyMud()
        {
            weapon?.ApplyContamination(WeaponContamination.Mud);
        }
    }
}
