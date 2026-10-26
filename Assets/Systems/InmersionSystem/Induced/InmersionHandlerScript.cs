using System.Collections;
using System.Collections.Generic;
using InmersionSystem;
using Systems.EventSystem.Scripts;
using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Effects;
using UnityEngine;
using Systems.InmersionSystem.Induced.Rendering;

namespace Systems.InmersionSystem.Induced {
    public class InmersionHandlerScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private AudioSource ringingEarsAudioSrc;

        [Header("Configs")]
        [SerializeField] private ShakeVisionConfig shakeVisionConfig;
        [SerializeField] private RingingEarsConfig ringingEarsConfig;
        [SerializeField] private BlurryVisionConfig blurryVisionConfig;
        [SerializeField] private TunnelVisionConfig tunnelVisionConfig;
        [SerializeField] private StepPrintingConfig stepPrintingConfig;
        [SerializeField] private MuffledHearingConfig muffledHearingConfig;
        private AudioLowPassFilter _ownedFilter;

        private readonly Dictionary<InmersiveElements.EInducedEffect, Coroutine> _activeCoroutines = new();
        private readonly Dictionary<InmersiveElements.EInducedEffect, InducedEffect> _effects = new();

        public static InmersionHandlerScript Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this) {
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
            if (Instance != this) return;
            GameEventBus.Subscribe<InmersiveElements.EInducedEffect>(GameplayEvents.VFX, Trigger);
        }
        
        public void OnDisable() {
            GameEventBus.Unsubscribe<InmersiveElements.EInducedEffect>(GameplayEvents.VFX, Trigger);
            StopAllCoroutines();
            _activeCoroutines.Clear();
            foreach (var effect in _effects.Values) effect.Reset();
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
            if (_ownedFilter) Destroy(_ownedFilter);
        }

        public void Trigger(InmersiveElements.EInducedEffect effect, float intensity) {
            if (!isActiveAndEnabled || Instance != this) return;
            if (!_effects.TryGetValue(effect, out var instance)) {
                Debug.LogWarning($"[InmersionHandler] No effect registered for {effect}");
                return;
            }

            ForceStop(effect);
            if (float.IsNaN(intensity) || intensity <= 0) return;
            _activeCoroutines[effect] = StartCoroutine(RunEffect(effect, instance, Mathf.Clamp01(intensity)));
        }
        
        public void Trigger(InmersiveElements.EInducedEffect effect) => Trigger(effect, 1f); 

        public void ForceStop(InmersiveElements.EInducedEffect effect)
        {
            if (_activeCoroutines.TryGetValue(effect, out var coroutine) && coroutine != null)
                StopCoroutine(coroutine);

            _activeCoroutines.Remove(effect);
            if (_effects.TryGetValue(effect, out var instance)) instance.Reset();
        }

        private IEnumerator RunEffect(InmersiveElements.EInducedEffect key, InducedEffect instance, float intensity)
        {
            yield return instance.EffectRoutine(intensity);
            instance.Reset();
            _activeCoroutines.Remove(key);
        }

        private void RegisterEffects() {
            if (cameraTransform == null) {
                Debug.LogWarning("[InmersionHandler] No camera transform found. ShakeVision will not affect the camera.", this);
            }
            
            if (ringingEarsAudioSrc == null) {
                Debug.LogWarning("[InmersionHandler] No ringing ears audio source assigned. RingingEars will not run.", this);
            }

            if (shakeVisionConfig == null) {
                Debug.LogWarning("[InmersionHandler] No ShakeVisionConfig assigned. ShakeVision will not run.", this);
            }
            
            if (ringingEarsConfig == null) {
                Debug.LogWarning("[InmersionHandler] No RingingEarsConfig assigned. RingingEars will not run.", this);
            }

            // Register all effects
            if (shakeVisionConfig && cameraTransform) _effects[InmersiveElements.EInducedEffect.ShakeVision] =
                new ShakeVisionEffect(shakeVisionConfig, cameraTransform);
            
            if (ringingEarsConfig && ringingEarsAudioSrc) _effects[InmersiveElements.EInducedEffect.RingingEars] =
                new RingingEarsEffect(ringingEarsConfig, ringingEarsAudioSrc);

            var camera = cameraTransform ? cameraTransform.GetComponentInChildren<Camera>() : null;
            if (!camera && Camera.main) camera = Camera.main;
            if (camera) {
                var state = camera.GetComponent<InducedVisionState>();
                if (!state) state = camera.gameObject.AddComponent<InducedVisionState>();
                if (blurryVisionConfig) _effects[InmersiveElements.EInducedEffect.BlurryVision] = new BlurryVisionEffect(blurryVisionConfig, state);
                if (tunnelVisionConfig) _effects[InmersiveElements.EInducedEffect.TunnelVision] = new TunnelVisionEffect(tunnelVisionConfig, state);
                if (stepPrintingConfig) _effects[InmersiveElements.EInducedEffect.StepPrinting] = new StepPrintingEffect(stepPrintingConfig, state);
                var listener = camera.GetComponentInChildren<AudioListener>();
                if (listener && muffledHearingConfig) {
                    var filter = listener.GetComponent<AudioLowPassFilter>();
                    if (!filter) {
                        filter = listener.gameObject.AddComponent<AudioLowPassFilter>();
                        filter.enabled = false;
                        _ownedFilter = filter;
                    }
                    _effects[InmersiveElements.EInducedEffect.MuffledHearing] = new MuffledHearingEffect(muffledHearingConfig, filter);
                }
            }
        }
    }
}
