using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    public enum WeaponContamination
    {
        Clean,
        Water,
        Sand,
        Mud
    }

    [DisallowMultipleComponent]
    public sealed class WeaponCondition : MonoBehaviour
    {
        [System.Serializable]
        private struct JamInterval
        {
            [Min(1)] public int minimumShots;
            [Min(1)] public int maximumShots;

            public int Roll()
            {
                int minimum = Mathf.Max(1, minimumShots);
                int maximum = Mathf.Max(minimum, maximumShots);
                return Random.Range(minimum, maximum + 1);
            }

            public void SetExact(int shots)
            {
                minimumShots = maximumShots = Mathf.Max(1, shots);
            }
        }

        [SerializeField] private JamInterval cleanInterval = new() { minimumShots = 25, maximumShots = 40 };
        [SerializeField] private JamInterval waterInterval = new() { minimumShots = 15, maximumShots = 25 };
        [SerializeField] private JamInterval sandInterval = new() { minimumShots = 8, maximumShots = 15 };
        [SerializeField] private JamInterval mudInterval = new() { minimumShots = 4, maximumShots = 10 };
        [SerializeField] private WeaponContamination contamination;
        [SerializeField] private UnityEvent onConditionChanged;
        [SerializeField] private UnityEvent onJamTriggered;

        private int shotsSinceLastJam;
        private int nextJamShot;
        private bool forceNextJam;

        public WeaponContamination Contamination => contamination;
        public int ShotsSinceLastJam => shotsSinceLastJam;
        public int NextJamShot => nextJamShot;

        private void Awake()
        {
            RollNextInterval();
        }

        public bool EvaluateShotForJam()
        {
            if (nextJamShot <= 0)
                RollNextInterval();

            if (forceNextJam)
            {
                forceNextJam = false;
                TriggerJam();
                return true;
            }

            shotsSinceLastJam++;
            if (shotsSinceLastJam < nextJamShot)
                return false;

            TriggerJam();
            return true;
        }

        public void ForceNextJam()
        {
            forceNextJam = true;
        }

        public void ApplyContamination(WeaponContamination value)
        {
            if (value <= contamination)
                return;

            contamination = value;
            RollNextInterval();
            onConditionChanged?.Invoke();
        }

        public void CleanWeapon()
        {
            contamination = WeaponContamination.Clean;
            shotsSinceLastJam = 0;
            forceNextJam = false;
            RollNextInterval();
            onConditionChanged?.Invoke();
        }

        public void ConfigureExactIntervalsForTests(int clean, int water, int sand, int mud)
        {
            cleanInterval.SetExact(clean);
            waterInterval.SetExact(water);
            sandInterval.SetExact(sand);
            mudInterval.SetExact(mud);
            RollNextInterval();
        }

        private void TriggerJam()
        {
            shotsSinceLastJam = 0;
            RollNextInterval();
            onJamTriggered?.Invoke();
        }

        private void RollNextInterval()
        {
            nextJamShot = contamination switch
            {
                WeaponContamination.Water => waterInterval.Roll(),
                WeaponContamination.Sand => sandInterval.Roll(),
                WeaponContamination.Mud => mudInterval.Roll(),
                _ => cleanInterval.Roll()
            };
        }
    }
}
