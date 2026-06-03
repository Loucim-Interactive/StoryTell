using System;
using System.Collections;
using UnityEngine;

namespace Systems.SoundSystem.Scripts {
    public class AmbientSoundScript : SoundScript {
        [Header("Ambient Sound Settings")] 
        [SerializeField] private bool playOnTrigger;

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
    }
}
