using System;
using System.Collections;
using System.Collections.Generic;
using InmersionSystem;
using Systems.EventSystem.Scripts;
using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Effects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.InmersionSystem.Induced {
    public class InmersionHandlerScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private AudioSource ringingEarsAudioSrc;

        [Header("Configs")]
        [SerializeField] private ShakeVisionConfig shakeVisionConfig;
        [SerializeField] private RingingEarsConfig ringingEarsConfig;
        // add others as you build them

        private readonly Dictionary<InmersiveElements.EInducedEffect, Coroutine> _activeCoroutines = new();
        private readonly Dictionary<InmersiveElements.EInducedEffect, InducedEffect> _effects = new();

        public static InmersionHandlerScript Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null) {
                Destroy(this);
                return;
            }
            
            Instance = this;

            if (cameraTransform == null && Camera.main != null) {
                cameraTransform = Camera.main.transform;
            }

            RegisterEffects();
        }

        public void OnEnable() {
            GameEventBus.Subscribe<InmersiveElements.EInducedEffect>(GameplayEvents.VFX, Trigger);
        }
        
        public void OnDisable() {
            GameEventBus.Unsubscribe<InmersiveElements.EInducedEffect>(GameplayEvents.VFX, Trigger);
        }

        public void Trigger(InmersiveElements.EInducedEffect effect, float intensity) {
            if (!_effects.TryGetValue(effect, out var instance)) {
                Debug.LogWarning($"[InmersionHandler] No effect registered for {effect}");
                return;
            }

            // Restart if already running
            if (_activeCoroutines.TryGetValue(effect, out var existing) && existing != null)
                StopCoroutine(existing);

            _activeCoroutines[effect] = StartCoroutine(RunEffect(effect, instance, intensity));
        }
        
        public void Trigger(InmersiveElements.EInducedEffect effect) => Trigger(effect, 1f); 

        public void ForceStop(InmersiveElements.EInducedEffect effect)
        {
            if (_activeCoroutines.TryGetValue(effect, out var coroutine) && coroutine != null)
                StopCoroutine(coroutine);

            _activeCoroutines.Remove(effect);
        }

        private IEnumerator RunEffect(InmersiveElements.EInducedEffect key, InducedEffect instance, float intensity)
        {
            yield return instance.EffectRoutine(intensity);
            _activeCoroutines.Remove(key);
        }

        private void RegisterEffects() {
            if (cameraTransform == null) {
                Debug.LogWarning("[InmersionHandler] No camera transform found. ShakeVision will not affect the camera.", this);
            }
            
            if (ringingEarsAudioSrc == null) {
                Debug.LogWarning("[InmersionHandler] No camera transform found. ShakeVision will not affect the camera.", this);
            }

            if (shakeVisionConfig == null) {
                Debug.LogWarning("[InmersionHandler] No ShakeVisionConfig assigned. ShakeVision will not run.", this);
                return;
            }
            
            if (ringingEarsConfig == null) {
                Debug.LogWarning("[InmersionHandler] No ShakeVisionConfig assigned. ShakeVision will not run.", this);
                return;
            }

            // Register all effects
            _effects[InmersiveElements.EInducedEffect.ShakeVision] = 
                new ShakeVisionEffect(shakeVisionConfig, cameraTransform);
            
            _effects[InmersiveElements.EInducedEffect.RingingEars] = 
                new RingingEarsEffect(ringingEarsConfig, ringingEarsAudioSrc);
        }
    }
}
