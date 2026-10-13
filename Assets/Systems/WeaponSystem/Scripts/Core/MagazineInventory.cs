using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    public sealed class MagazineInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int magazineCapacity = 20;
        [SerializeField, Min(0)] private int maxMagazineCount = 4;
        [SerializeField] private List<int> reserveMagazineRounds = new();
        [SerializeField] private UnityEvent onInventoryChanged;

        public int MagazineCapacity => magazineCapacity;
        public int MaxMagazineCount => maxMagazineCount;
        public int MagazineCount => reserveMagazineRounds.Count;
        public int TotalReserveRounds
        {
            get
            {
                int total = 0;
                for (int i = 0; i < reserveMagazineRounds.Count; i++)
                    total += reserveMagazineRounds[i];
                return total;
            }
        }

        public bool IsFull => MagazineCount >= maxMagazineCount;

        public void Configure(int capacity, int maxCount)
        {
            magazineCapacity = Mathf.Max(1, capacity);
            maxMagazineCount = Mathf.Max(0, maxCount);
            SanitizeInventory();
        }

        public bool AddMagazine(int rounds)
        {
            if (IsFull || rounds <= 0)
                return false;

            reserveMagazineRounds.Add(Mathf.Clamp(rounds, 1, magazineCapacity));
            onInventoryChanged?.Invoke();
            return true;
        }

        public int AddFullMagazines(int count)
        {
            int added = 0;
            for (int i = 0; i < Mathf.Max(0, count); i++)
            {
                if (!AddMagazine(magazineCapacity))
                    break;
                added++;
            }
            return added;
        }

        public bool TryTakeNextMagazine(out int rounds)
        {
            if (reserveMagazineRounds.Count == 0)
            {
                rounds = 0;
                return false;
            }

            rounds = reserveMagazineRounds[0];
            reserveMagazineRounds.RemoveAt(0);
            onInventoryChanged?.Invoke();
            return true;
        }

        public int GetMagazineRounds(int index)
        {
            return index >= 0 && index < reserveMagazineRounds.Count
                ? reserveMagazineRounds[index]
                : 0;
        }

        public void Clear()
        {
            if (reserveMagazineRounds.Count == 0)
                return;
            reserveMagazineRounds.Clear();
            onInventoryChanged?.Invoke();
        }

        private void OnValidate()
        {
            magazineCapacity = Mathf.Max(1, magazineCapacity);
            maxMagazineCount = Mathf.Max(0, maxMagazineCount);
            SanitizeInventory();
        }

        private void SanitizeInventory()
        {
            reserveMagazineRounds ??= new List<int>();
            while (reserveMagazineRounds.Count > maxMagazineCount)
                reserveMagazineRounds.RemoveAt(reserveMagazineRounds.Count - 1);
            for (int i = reserveMagazineRounds.Count - 1; i >= 0; i--)
            {
                if (reserveMagazineRounds[i] <= 0)
                    reserveMagazineRounds.RemoveAt(i);
                else
                    reserveMagazineRounds[i] = Mathf.Min(reserveMagazineRounds[i], magazineCapacity);
            }
        }
    }
}
