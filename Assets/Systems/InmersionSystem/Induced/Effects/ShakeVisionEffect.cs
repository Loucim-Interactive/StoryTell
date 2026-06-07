using System.Collections;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using InmersionSystem.Induced;
using Systems.InmersionSystem.Induced.Configs;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Effects {
    public class ShakeVisionEffect : InducedEffect<ShakeVisionConfig>
    {
        private readonly Transform _cameraTransform;
        private readonly CameraControllerScript _cameraController;
        private Quaternion _initialRotation;
        private float _trauma;

        public ShakeVisionEffect(ShakeVisionConfig config, Transform cameraTransform) 
            : base(config)
        {
            _cameraTransform = cameraTransform;
            if (_cameraTransform != null) {
                _cameraController = _cameraTransform.GetComponentInParent<CameraControllerScript>();
            }
        }

        public override IEnumerator EffectRoutine(float intensity)
        {
            if (config == null) {
                Debug.LogWarning("[ShakeVisionEffect] Missing ShakeVisionConfig.");
                yield break;
            }

            if (_cameraTransform == null) {
                Debug.LogWarning("[ShakeVisionEffect] Missing camera transform.");
                yield break;
            }

            _initialRotation = _cameraTransform.localRotation;
            _trauma = Mathf.Clamp01(intensity);
            float duration = config.maxDuration * Mathf.Clamp01(intensity);
            float elapsed = 0f;

            while (elapsed < duration && _trauma > 0.01f)
            {
                elapsed += Time.deltaTime;

                float shake = Mathf.Pow(_trauma, config.traumaExponent) * config.maxMagnitude;
                float seed = Time.time * 32f;
                float offsetX = (Mathf.PerlinNoise(seed, 0f) * 2f - 1f) * shake;
                float offsetY = (Mathf.PerlinNoise(0f, seed) * 2f - 1f) * shake;

                ApplyShake(new Vector3(offsetX, offsetY, 0f));

                _trauma -= config.traumaDecaySpeed * Time.deltaTime;

                yield return null;
            }

            ApplyShake(Vector3.zero);
            if (_cameraController == null) {
                _cameraTransform.localRotation = _initialRotation;
            }

            _trauma = 0f;
        }

        private void ApplyShake(Vector3 offset) {
            if (_cameraController != null) {
                _cameraController.SetShakeRotationOffset(offset);
                return;
            }

            _cameraTransform.localRotation = _initialRotation * Quaternion.Euler(offset);
        }
    }
}
