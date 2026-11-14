using System;
using DialogueSystem.Scripts;
using InteractionSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.GlobalUtil {
    public class NPCInteractableScript : InteractableScript
    {
        public SpeakerScript speaker;

        public void Start() {
            // A duplicated NPC must never keep the original actor's speaker reference.
            var owner = GetComponentInParent<SpeakerScript>();
            if (owner) speaker = owner;
        }

        protected override void OnInteract() {
            if (speaker) speaker.Speak();
        }
    }
}
