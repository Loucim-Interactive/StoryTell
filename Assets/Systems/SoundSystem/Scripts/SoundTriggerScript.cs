using UnityEngine;

namespace Systems.SoundSystem.Scripts {
    public class SoundTriggerScript : MonoBehaviour
    {
        [SerializeField] private AmbientSoundScript soundScript;

        private void OnTriggerEnter(Collider other) {
            if (soundScript != null) {
                // forward the event to the sound script
                soundScript.OnProxyTriggerEnter(other);
            }
        }

    }
}
