using System.Collections;
using InmersionSystem.Ambient.Configs;
using UnityEngine;

namespace InmersionSystem.Ambient.Effects {
    public class ColdBreathEffect : AmbientEffect<ColdBreathConfig>
    {
        [Header("References")]
        [SerializeField] private ParticleSystem breathParticles;
        [SerializeField] private AudioSource audioSource;
        
        [Header("Movement")]
        [SerializeField] private Rigidbody characterRigidbody; // optional, for velocity inherit
        
        private Coroutine _breathCycle;
        private ParticleSystem.MainModule _particleMain;
        private bool _isRunning;

        private void Awake()
        {
            if (breathParticles != null)
            {
                _particleMain = breathParticles.main;
                _particleMain.simulationSpace = ParticleSystemSimulationSpace.Local; // Way A
                _particleMain.simulationSpeed = config.simulationSpeed;
            }
        }

        private void Start() {
            _breathCycle = StartCoroutine(BreathCycleRoutine());
        }

        private void OnDisable() {
            OnEffectStop();
        }

        public override void OnEffectStart() {
            if (_isRunning) return;
            _breathCycle = StartCoroutine(BreathCycleRoutine());
            _isRunning = true;
        }

        public override void OnEffectStop()
        {
            if (_breathCycle != null)
                StopCoroutine(_breathCycle);
            
            breathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _isRunning = false;
        }

        private IEnumerator BreathCycleRoutine()
        {
            while (true)
            {
                Exhale();
                yield return new WaitForSeconds(config.breathInterval);
            }
        }

        private void Exhale()
        {
            // Inherit player velocity so breath doesn't shoot backwards when moving
            if (characterRigidbody)
            {
                var velocityOverLifetime = breathParticles.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.x = characterRigidbody.linearVelocity.x * 0.4f;
                velocityOverLifetime.y = characterRigidbody.linearVelocity.y * 0.4f;
                velocityOverLifetime.z = characterRigidbody.linearVelocity.z * 0.4f;
            }

            breathParticles.Play();

            if (audioSource && config.exhaleSound)
                audioSource.PlayOneShot(config.exhaleSound, config.exhaleVolume);
        }
    }
}
