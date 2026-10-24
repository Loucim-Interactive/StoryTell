using UnityEngine;

namespace StoryTell.Weapons
{
    public sealed class WeaponImpactFeedback : MonoBehaviour
    {
        [SerializeField] private WeaponController weapon;
        [SerializeField] private ParticleSystem[] impactPool;
        private int next;
        public int ImpactCount { get; private set; }
        private void OnEnable() { if (weapon != null) weapon.ShotHit += OnHit; }
        private void OnDisable()
        {
            if (weapon != null) weapon.ShotHit -= OnHit;
            if (impactPool != null) foreach (var effect in impactPool) if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnHit(RaycastHit hit)
        {
            ImpactCount++;
            if (impactPool == null || impactPool.Length == 0) return;
            ParticleSystem effect = impactPool[next++ % impactPool.Length];
            if (effect == null) return;
            bool metal = hit.collider.GetComponentInParent<WeaponCylinderTarget>() != null;
            var renderer = hit.collider.GetComponent<Renderer>();
            string material = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "";
            metal |= material.IndexOf("metal", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool wood = material.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0;
            for (int i = 0; i < (metal ? 7 : 11); i++)
            {
                Vector3 direction = (hit.normal + Random.insideUnitSphere * 0.8f).normalized;
                var particle = new ParticleSystem.EmitParams {
                    position = hit.point + hit.normal * 0.025f,
                    velocity = direction * Random.Range(metal ? 1.5f : 0.4f, metal ? 3.5f : 1.2f),
                    startColor = metal ? new Color(1, 0.65f, 0.22f, 0.9f) : wood ? new Color(0.46f, 0.34f, 0.2f, 0.6f) : new Color(0.52f, 0.49f, 0.42f, 0.55f),
                    startSize = metal ? Random.Range(0.015f, 0.035f) : Random.Range(0.07f, 0.15f),
                    startLifetime = metal ? Random.Range(0.10f, 0.25f) : Random.Range(0.2f, 0.45f)
                };
                effect.Emit(particle, 1);
            }
        }
    }
}
