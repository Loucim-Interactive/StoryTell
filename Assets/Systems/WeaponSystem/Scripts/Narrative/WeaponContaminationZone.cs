using UnityEngine;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WeaponContaminationZone : MonoBehaviour
    {
        [SerializeField] private WeaponContamination contamination = WeaponContamination.Mud;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;

        private bool triggered;

        private void Reset()
        {
            if (TryGetComponent(out Collider zoneCollider))
                zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((triggerOnce && triggered) ||
                (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)))
                return;

            WeaponCondition condition = other.GetComponentInParent<WeaponCondition>();
            if (condition == null)
                return;

            condition.ApplyContamination(contamination);
            triggered = true;
        }

        public void ApplyTo(WeaponCondition condition)
        {
            if (condition == null || (triggerOnce && triggered))
                return;
            condition.ApplyContamination(contamination);
            triggered = true;
        }

        public void ResetZone()
        {
            triggered = false;
        }
    }
}
