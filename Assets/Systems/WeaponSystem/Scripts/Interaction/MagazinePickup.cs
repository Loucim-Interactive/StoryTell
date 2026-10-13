using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MagazinePickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int magazineCount = 1;
        [SerializeField, Min(0)] private int roundsPerMagazine;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool disableWhenEmpty = true;
        [SerializeField] private UnityEvent onMagazineCollected;
        [SerializeField] private UnityEvent onInventoryFull;

        public int RemainingMagazineCount => magazineCount;

        private void Reset()
        {
            if (TryGetComponent(out Collider pickupCollider))
                pickupCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
                return;

            MagazineInventory inventory = other.GetComponentInParent<MagazineInventory>();
            if (inventory != null)
                CollectInto(inventory);
        }

        public int CollectInto(MagazineInventory inventory)
        {
            if (inventory == null || magazineCount <= 0)
                return 0;

            int collected = 0;
            int rounds = roundsPerMagazine <= 0 ? inventory.MagazineCapacity : roundsPerMagazine;
            while (magazineCount > 0 && inventory.AddMagazine(rounds))
            {
                magazineCount--;
                collected++;
                onMagazineCollected?.Invoke();
            }

            if (magazineCount > 0 && inventory.IsFull)
                onInventoryFull?.Invoke();
            if (magazineCount <= 0 && disableWhenEmpty)
                gameObject.SetActive(false);
            return collected;
        }

        public void ConfigureForTests(int count, int rounds = 0)
        {
            magazineCount = Mathf.Max(1, count);
            roundsPerMagazine = Mathf.Max(0, rounds);
        }

        private void OnValidate()
        {
            magazineCount = Mathf.Max(1, magazineCount);
            roundsPerMagazine = Mathf.Max(0, roundsPerMagazine);
            playerTag = playerTag != null ? playerTag.Trim() : string.Empty;
        }
    }
}
