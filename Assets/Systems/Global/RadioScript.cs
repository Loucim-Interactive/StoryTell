using System;
using System.Collections;
using Systems.EventSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Radio;
using Systems.SoundSystem.Scripts;
using UnityEngine;

namespace Systems.Global {
    public class RadioScript : SoundScript {
        [Header("Radio settings")] 
        [SerializeField] private AudioClip _frequencyChangeSound;

        private int _currentIndex;
        private Coroutine _coroutine;
        private bool _isBeingInteracted;

        private void Start() {
            _currentIndex = 0;
            if (_frequencyChangeSound == null) Debug.LogWarning("Radio script requires a frequency change sound");
        }

        private void OnEnable() {
            GameEventBus.Subscribe<RadioInteractions>(GameplayEvents.InteractAction, HandleInteractions);
        }

        private void OnDisable() {
            GameEventBus.Unsubscribe<RadioInteractions>(GameplayEvents.InteractAction, HandleInteractions);
        }
        
        private void ChangeTune() {
            _currentIndex++;
            if (_currentIndex > soundClips.Length) {
                _currentIndex = 0;
            }

            _coroutine = StartCoroutine(PlayNextFreq(_currentIndex));
        }
        
        private void RaiseVolume() {
            audioSource.volume = Mathf.Clamp01(audioSource.volume + 0.1f);
            if (_currentIndex > soundClips.Length) 
                _currentIndex = 0;
            

            _coroutine = StartCoroutine(PlayNextFreq(_currentIndex));
        }

        private IEnumerator PlayNextFreq(int nextFreq) {
            InterruptPlay();
            PlaySound(_frequencyChangeSound);
            yield return new WaitWhile(() => audioSource.isPlaying);
            PlaySound(nextFreq);
        } 
        
        protected override void SendEffectEvent() { }

        private void HandleInteractions(RadioInteractions interaction) {
            switch (interaction) {
                case RadioInteractions.FrequencyChange:
                    ChangeTune();
                    break;
                case RadioInteractions.VolumeChange:
                    RaiseVolume();
                    break;
                default:
                    break;
            }
        }
    }
}
