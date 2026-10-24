using UnityEngine;

namespace StoryTell.Weapons
{
    public sealed class WeaponCylinderTarget : MonoBehaviour, IWeaponDamageable
    {
        [SerializeField, Min(1)] private float maximumHealth = 100;
        [SerializeField] private Renderer targetRenderer;
        private float health;
        private MaterialPropertyBlock block;
        private float flashUntil;
        public bool IsDead => health <= 0;
        public float Health => health;
        private void Awake() { health = maximumHealth; block = new MaterialPropertyBlock(); }
        public bool ReceiveWeaponDamage(float damage, Vector3 point, Vector3 direction, GameObject source)
        {
            if (IsDead || damage <= 0) return false;
            health = Mathf.Max(0, health - damage);
            flashUntil = Time.time + 0.12f;
            if (IsDead) { foreach (Collider c in GetComponents<Collider>()) c.enabled = false; }
            return true;
        }
        private void Update()
        {
            if (IsDead) transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, 0, 90), Time.deltaTime * 5);
            if (targetRenderer == null) return;
            block.SetColor("_BaseColor", Time.time < flashUntil ? Color.white : IsDead ? Color.gray : new Color(0.6f, 0.16f, 0.1f));
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
