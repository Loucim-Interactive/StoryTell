using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.BattleSystem {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TriggerZone : MonoBehaviour {
        [SerializeField] private bool triggerOnce = true;
        private bool triggered;

        private void Reset() => GetComponent<Collider>().isTrigger = true;
        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other) {
            if (!isActiveAndEnabled || (triggerOnce && triggered)) return;
            for (var current = other.transform; current != null; current = current.parent) {
                if (!current.CompareTag("Player")) continue;
                triggered = true;
                GameEventBus.Raise(GameplayEvents.OnBattleTriggered);
                return;
            }
        }
    }
}
