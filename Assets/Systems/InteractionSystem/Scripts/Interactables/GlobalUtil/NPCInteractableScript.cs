using System;
using DialogueSystem.Scripts;
using InteractionSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.GlobalUtil {
    public class NPCInteractableScript : InteractableScript
    {
        public SpeakerScript speaker;

        public void Start() {
            if (!speaker) {
                speaker = gameObject.GetComponentInParent<SpeakerScript>();
            }
        }

        protected override void OnInteract() {
            //speaker.Speak();
        }
    }
}
