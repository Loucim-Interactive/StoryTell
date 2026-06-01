using System.Collections;
using UnityEngine;

namespace InteractionSystem.Scripts.Interactables.Animations {
    public class SimpleAnimationRunner : MonoBehaviour
    {   
        private Coroutine _turnKnobRoutine;
        
        public void TurnKnob(Transform target, float angles, Vector3 axis, float duration) {
            if (_turnKnobRoutine != null) StopCoroutine(_turnKnobRoutine);
            _turnKnobRoutine = StartCoroutine(TurnKnobRoutine(target, angles, axis, duration));
        }
        
        private IEnumerator TurnKnobRoutine(Transform target, float angles, Vector3 axis, float duration) {
            Quaternion startRotation = target.localRotation;
            Quaternion targetRotation = startRotation * Quaternion.AngleAxis(angles, axis);
                
            if (duration <= 0f) {
                target.localRotation = targetRotation;
                _turnKnobRoutine = null;
                yield break;
            }           
                            
            float elapsed = 0f; 
            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

                target.localRotation = Quaternion.SlerpUnclamped(
                    startRotation,
                    targetRotation,
                    easedTime
                );

                yield return null;
            }
            
            target.localRotation = targetRotation;
            _turnKnobRoutine = null;
        }
    }
}
