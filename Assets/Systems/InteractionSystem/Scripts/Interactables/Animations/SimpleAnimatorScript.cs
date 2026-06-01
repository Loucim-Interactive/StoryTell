using UnityEngine;

namespace InteractionSystem.Scripts.Interactables.Animations {
    public static class SimpleAnimatorScript
    {
        public static void TurnKnob(Transform t, float angles, Vector3 axis, float duration = 0.25f) {
            if (!t) return;
            if (axis.sqrMagnitude <= 0f) return;

            SimpleAnimationRunner runner = t.GetComponent<SimpleAnimationRunner>();
            if (!runner) runner = t.gameObject.AddComponent<SimpleAnimationRunner>();

            runner.TurnKnob(t, angles, axis.normalized, duration);
        }
    }
}
