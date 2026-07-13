using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.Radio {

    public enum RadioInteractions {
        FrequencyChange,
        VolumeChange,
    }
    public class RadioKnobInteractable : KnobInteractable<RadioInteractions> { }
}
