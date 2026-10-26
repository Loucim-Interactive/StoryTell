using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Missions.FindObjects {
    public class PickableManager<T> : MonoBehaviour
    {
        [Header("references")]
        [SerializeField] private Camera playerCam;
        [SerializeField] private LayerMask pickableLayer;

        [Header("settings")]
        [SerializeField] private float pickupDistance = 10f;

        private int _pickedUpCount;
        
        private void OnEnable() {
            GameEventBus.Subscribe<T>(GameplayEvents.PickupAction, HandlePickup);
        }
        
        private void OnDisable() {
            GameEventBus.Unsubscribe<T>(GameplayEvents.PickupAction, HandlePickup);
        }

        protected virtual void OnPickup(T obj) { } // inheritors override
        
        private void HandlePickup(T pickedUp) {
            OnPickup(pickedUp);
            _pickedUpCount++;
            Debug.Log("New picked up count: " + _pickedUpCount);
        }
    }
}
