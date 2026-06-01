using InteractionSystem.Scripts.Interactables.Animations;
using InteractionSystem.Scripts.Utils;
using UnityEngine;

namespace InteractionSystem.Scripts.Interactables.Radio {
    public class VolumeKnobInteractableScript : InteractableScript
    {
        private enum Direction {
            Up,
            Left,
            Down,
            Right,
            Forward,
            Backward,
        }
        
        [Header("Config")] 
        [SerializeField] private float knobTurnAngles = 20;
        [SerializeField] private float knobTurnDuration = 0.25f;
        [SerializeField] private Direction knobTurnAxis = Direction.Up;

        [SerializeField] private AudioSource audioSource;
        protected override void OnInteract() {
            Vector3 axis = ResolveDir(knobTurnAxis);
            SimpleAnimatorScript.TurnKnob(transform, knobTurnAngles, axis, knobTurnDuration);
        }

        private Vector3 ResolveDir(Direction direction) {
            switch (direction) {
                case Direction.Up:
                    return Vector3.up;
                case Direction.Left:
                    return Vector3.left;
                case Direction.Down:
                    return Vector3.down;
                case Direction.Right:
                    return Vector3.right;
                case Direction.Forward:
                    return Vector3.forward;
                case Direction.Backward:
                    return Vector3.back;
                default:    
                    return Vector3.zero;
            }
        }
    }
}
