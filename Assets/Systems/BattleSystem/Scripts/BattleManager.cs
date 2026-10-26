using Systems.EventSystem.Scripts;
using Systems.WeaponSystem;
using UnityEngine;

namespace Systems.BattleSystem {
    [DisallowMultipleComponent]
    public sealed class BattleManager : MonoBehaviour {
        [SerializeField] private PlayerWeaponController player;
        [SerializeField] private ShadowSpawner shadowSpawner;
        [SerializeField, Min(0)] private int spawnCount = 12;
        [SerializeField] private bool triggerOnce = true;
        private bool started;

        private void OnEnable() => GameEventBus.Subscribe(GameplayEvents.OnBattleTriggered, StartBattle);
        private void OnDisable() => GameEventBus.Unsubscribe(GameplayEvents.OnBattleTriggered, StartBattle);

        private void StartBattle() {
            if (triggerOnce && started) return;
            if (!player || !shadowSpawner || !shadowSpawner.IsConfigured) {
                Debug.LogWarning("BattleManager needs a player, a spawn area and an Unlit shadow material.", this);
                return;
            }
            if (!player.EquipWeapon("FAL")) return;
            started = true;
            shadowSpawner.Spawn(spawnCount);
        }
    }
}
