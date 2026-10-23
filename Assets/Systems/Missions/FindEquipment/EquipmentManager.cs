using Systems.EventSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using UnityEngine;

namespace Systems.Missions.FindEquipment {
    public class EquipmentManager : MonoBehaviour {
    
    
        [Header("references")]
        [SerializeField] private Camera playerCam;
        [SerializeField] private LayerMask equipmentLayer;

        [Header("settings")]
        [SerializeField] private float equipDistance = 10f;
        [SerializeField] private GameplayEventType eventType;

        private int _equippedCount;
        
        void OnEnable() {
            GameEventBus.Subscribe(GameplayEvents.EquipAction, HandleEquip);
        }
        
        void OnDisable() {
            GameEventBus.Unsubscribe(GameplayEvents.EquipAction, HandleEquip);
        }
        
        void HandleEquip() {
            _equippedCount++;
            Debug.Log("New equipped count: " + _equippedCount);
        }
    }
}
