using System.Collections;
using InmersionSystem.Induced;
using InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Configs;
using UnityEngine;

namespace Systems.InmersionSystem.Induced.Effects {
    public class RingingEarsEffect : InducedEffect<RingingEarsConfig>
    {
        private readonly AudioSource _audioSource;

        public RingingEarsEffect(RingingEarsConfig config, AudioSource audioSource) : base(config) {
            this._audioSource = audioSource;
        }

        public override IEnumerator EffectRoutine(float intensity) {
            if (!config) {
                Debug.LogWarning("[ShakeVisionEffect] Missing ShakeVisionConfig.");
                yield break;
            }

            _audioSource.clip = config.ringingClip;
            // there is an AnimationCurve that comes from "config.volumeCurve" i want it to dictate how strongly you hear the clip over time
            _audioSource.volume = _audioSource.volume; // placeholder
            _audioSource.Play();
        }
    }
}
