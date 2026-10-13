using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponDamageReceiver : MonoBehaviour, IWeaponDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private bool acceptsWeaponDamage = true;
        [SerializeField] private UnityEvent onDamaged;
        [SerializeField] private UnityEvent onKilled;

        public float CurrentHealth { get; private set; }
        public float MaximumHealth => maximumHealth;
        public bool IsDead => CurrentHealth <= 0f;
        public bool AcceptsWeaponDamage => acceptsWeaponDamage;

        private void Awake()
        {
            ResetHealth();
        }

        public bool ReceiveWeaponDamage(float damage, Vector3 hitPoint, Vector3 shotDirection, GameObject source)
        {
            if (!acceptsWeaponDamage || IsDead || damage <= 0f)
                return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            onDamaged?.Invoke();
            if (IsDead)
                onKilled?.Invoke();
            return true;
        }

        public void SetAcceptsWeaponDamage(bool value)
        {
            acceptsWeaponDamage = value;
        }

        public void ResetHealth()
        {
            CurrentHealth = Mathf.Max(1f, maximumHealth);
        }

        public void ConfigureForTests(float health, bool acceptsDamage = true)
        {
            maximumHealth = Mathf.Max(1f, health);
            acceptsWeaponDamage = acceptsDamage;
            ResetHealth();
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
        }
    }
}
