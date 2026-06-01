using InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace InteractionSystem.Scripts.Interactables.Tests {
    public class CubeInteractableScript : InteractableScript {
        protected override void OnInteract() {
            Debug.Log("CUBE Interact: " + interactableName.ToUpper());
        }
    }
}
