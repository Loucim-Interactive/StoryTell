using System;
using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.SoundSystem.Scripts {
    public class AmbientSoundScript : SoundScript {
        [Header("Ambient Sound Settings")] 
        [SerializeField] private bool playOnTrigger;
        [Tooltip("This is used for event bus, to help with inmersion logic (used for cam effects and more)")][SerializeField] private float intensity = 0.1f;
        [FormerlySerializedAs("SoundType")]
        [SerializeField] private AmbientSoundTypes soundType;
        [SerializeField] private SoundEvent[] soundEvents;

        [Serializable]
        public class SoundEvent {
            public GameplayEventType EventName;
        }
        

        private void Start() {
            if (!playOnTrigger) PlaySound();
        }

        private void Update() {
            if (replay && !audioSource.isPlaying && ReplayRoutine == null) {
                audioSource.Stop();
                ReplayRoutine = StartCoroutine(Replay());
            }
        }
        
        public void OnProxyTriggerEnter(Collider other) {
            if (!playOnTrigger) return;
            if (other.CompareTag("Player")) {
                PlaySound();
            }
        }

        protected override void SendEffectEvent() {
            if (soundType == AmbientSoundTypes.None) return;
            string eventName = ResolveEvent();
            if (string.IsNullOrEmpty(eventName)) return;
            GameEventBus.Raise(eventName, intensity);
        }

        private string ResolveEvent() {
            if (soundEvents == null) return string.Empty;

            foreach (SoundEvent soundEvent in soundEvents) {
                return GameplayEvents.GetName(soundEvent.EventName);
            }
            
            return string.Empty;
        }
    }
}
