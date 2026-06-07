using System.Collections;
using UnityEngine;

namespace Systems.SoundSystem.Scripts {
    public abstract class SoundScript : MonoBehaviour
    {
        [Header("General Sound Settings")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] private AudioClip[] soundClips;
        [SerializeField] private bool chooseRandomly = true;
        [SerializeField, Range(0, 1)] private float volume = 1f;
        [SerializeField, Range(0, 1)] private float volume1 = 1f;
        [Header("Replay Settings")]
        [SerializeField] protected bool replay = false;
        [SerializeField] private bool useReplayWaitTimeRange = false;
        [SerializeField] private UnityEditorSerializables.FloatRange replayWaitTimeRange;
        [SerializeField] private float defaultWaitTime = 2f;

        protected Coroutine ReplayRoutine;

        private void Awake() {
            if (audioSource == null) audioSource = GetComponent<AudioSource>(); 
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            CheckRefs();
        }

        protected abstract void SendEffectEvent();
        
        protected void PlaySound() {
            if (audioSource.isPlaying) return;
            if (chooseRandomly) {
                audioSource.clip = GetRandomClip();
                audioSource.volume = volume;
                audioSource.Play();
            }
            else {
                audioSource.clip = soundClips[0];
                audioSource.Play();
            }

            SendEffectEvent();
            Debug.Log("Playing-sound: " + audioSource.clip.name);
        }
        
        protected IEnumerator Replay() {
            yield return new WaitForSeconds(GetReplayWaitTime());
            PlaySound();
            ReplayRoutine = null;
        }
        
        private AudioClip GetRandomClip() {
            return soundClips[Random.Range(0, soundClips.Length)];
        }

        private float GetReplayWaitTime() {
            if (useReplayWaitTimeRange) {
                return Random.Range(replayWaitTimeRange.min, replayWaitTimeRange.max);
            }
            else {
                return defaultWaitTime;
            }
        }

        private void CheckRefs() {
            if (audioSource == null) {
                Debug.LogWarning("No audio source assigned to SoundScript");
            }
            if (soundClips.Length == 0) {
                Debug.LogWarning("Sound clips are empty!");
            };
        }
    }
}
