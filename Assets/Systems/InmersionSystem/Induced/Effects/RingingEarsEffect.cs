using System.Collections;
using InmersionSystem.Induced;
using InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Configs;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Effects {
    public class RingingEarsEffect : InducedEffect<RingingEarsConfig>
    {
        private readonly AudioSource _audioSource;
        private float _originalVolume;
        private bool _playing;

        public RingingEarsEffect(RingingEarsConfig config, AudioSource audioSource) : base(config) {
            this._audioSource = audioSource;
        }

        public override IEnumerator EffectRoutine(float intensity) {
            if (!config || !_audioSource || !config.ringingClip) {
                yield break;
            }
            _originalVolume = _audioSource.volume;
            _playing = true;
            _audioSource.clip = config.ringingClip;
            _audioSource.Play();
            float duration = config.ringingClip.length / Mathf.Max(.01f, Mathf.Abs(_audioSource.pitch));
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime) {
                if (!_audioSource) break;
                float curve = config.volumeCurve != null && config.volumeCurve.length > 0 ? config.volumeCurve.Evaluate(elapsed / duration) : 1;
                _audioSource.volume = _originalVolume * Mathf.Clamp01(intensity) * config.maxIntensity * Mathf.Clamp01(curve);
                yield return null;
            }
            Reset();
        }

        public override void Reset() {
            if (_playing && _audioSource) { _audioSource.Stop(); _audioSource.volume = _originalVolume; }
            _playing = false;
        }
    }
}
