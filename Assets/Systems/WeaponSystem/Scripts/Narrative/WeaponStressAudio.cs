using UnityEngine;

namespace StoryTell.Weapons
{
    [DefaultExecutionOrder(600)]
    public sealed class WeaponStressAudio : MonoBehaviour
    {
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField] private AudioSource breathing;
        [SerializeField] private AudioLowPassFilter weaponFilter;
        [SerializeField] private AudioSource[] endingSources;
        [SerializeField, Range(0, 1)] private float breathingVolume = 0.23f;
        private float nextBreath;
        private float originalCutoff = 22000;
        private float[] originalVolumes;
        private bool endingCaptured;
        private void Awake() { if (weaponFilter != null) originalCutoff = weaponFilter.cutoffFrequency; }
        private void LateUpdate()
        {
            if (trial == null) return;
            float tension = trial.Darkness;
            if (weaponFilter != null) weaponFilter.cutoffFrequency = Mathf.Lerp(originalCutoff, 2800, tension);
            if (breathing != null && trial.HasRifle && !trial.IsFinishing)
            {
                breathing.volume = Mathf.SmoothStep(0, breathingVolume, tension);
                if (tension > 0.18f && !breathing.isPlaying && Time.time >= nextBreath)
                {
                    breathing.Play();
                    nextBreath = Time.time + Mathf.Lerp(4, 1.5f, tension);
                }
            }
            if (!trial.IsFinishing) return;
            if (breathing != null) breathing.volume = breathingVolume * (1 - trial.CentralDarkness);
            if (!endingCaptured)
            {
                endingCaptured = true;
                originalVolumes = new float[endingSources.Length];
                for (int i = 0; i < endingSources.Length; i++) if (endingSources[i] != null) originalVolumes[i] = endingSources[i].volume;
            }
            for (int i = 0; i < endingSources.Length; i++)
                if (endingSources[i] != null) endingSources[i].volume = originalVolumes[i] * (1 - trial.CentralDarkness);
        }
        private void OnDisable()
        {
            if (breathing != null) breathing.Stop();
            if (weaponFilter != null) weaponFilter.cutoffFrequency = originalCutoff;
            if (endingCaptured && endingSources != null)
                for (int i = 0; i < endingSources.Length; i++) if (endingSources[i] != null) endingSources[i].volume = originalVolumes[i];
            endingCaptured = false;
        }
    }
}
