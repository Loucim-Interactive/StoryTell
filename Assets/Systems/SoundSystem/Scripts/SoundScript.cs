using System.Collections;
using UnityEngine;

namespace Systems.SoundSystem.Scripts
{
    public abstract class SoundScript : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip[] soundClips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool chooseRandomly = true;

        [Header("Replay")]
        [SerializeField] protected bool replay = false;
        [SerializeField] private bool useReplayWaitTimeRange = false;
        [SerializeField] private UnityEditorSerializables.FloatRange replayWaitTimeRange;
        [SerializeField] private float defaultWaitTime = 2f;

        protected Coroutine ReplayRoutine;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            CheckRefs();
            OnAwake();
        }

        protected virtual void OnAwake() { }
        protected abstract void SendEffectEvent();

        #region Play API

        protected void InterruptPlay() {
            if (ReplayRoutine != null) StopCoroutine(ReplayRoutine);
            ReplayRoutine = null;
            audioSource.Stop();
            audioSource.clip = null;
        }

        protected void PlaySound() {
            if (soundClips == null || soundClips.Length == 0) return;
            int index = chooseRandomly ? GetRandomClipIndex() : 0;
            PlayClipAt(index);
        }

        protected void PlaySound(int index) {
            if (soundClips == null || index < 0 || index >= soundClips.Length) return;
            PlayClipAt(index);
        }

        protected void PlaySound(AudioClip clip) {
            if (!clip) return;
            if (audioSource.isPlaying) return;
            audioSource.clip   = clip;
            audioSource.volume = volume;
            audioSource.Play();
            SendEffectEvent();
        }
        
        #endregion

        // Single internal method that actually drives the AudioSource
        private void PlayClipAt(int index) {
            if (audioSource.isPlaying) return;
            audioSource.clip   = soundClips[index];
            audioSource.volume = volume;
            audioSource.Play();
            SendEffectEvent();
        }
        
        protected IEnumerator Replay() {
            yield return new WaitForSeconds(GetReplayWaitTime());
            PlaySound();
            ReplayRoutine = null;
        }

        #region helpers

        private int GetRandomClipIndex() => Random.Range(0, soundClips.Length);

        private float GetReplayWaitTime() => useReplayWaitTimeRange ? 
            Random.Range(replayWaitTimeRange.min, replayWaitTimeRange.max) : defaultWaitTime;

        private void CheckRefs() {
            if (audioSource == null)
                Debug.LogWarning($"[{nameof(SoundScript)}] No AudioSource on {gameObject.name}.");
            if (soundClips == null || soundClips.Length == 0)
                Debug.LogWarning($"[{nameof(SoundScript)}] No clips assigned on {gameObject.name}.");
        }
        
        #endregion
    }
}