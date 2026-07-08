using System;
using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.InmersionSystem.Elements {
    public class LookAtPointScript : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("This should be the look target")] 
        [SerializeField] private Transform targetLookAt;
        [Tooltip("This should be anything you want, that lets you visualize the collider")] 
        [SerializeField] private GameObject visualizationObject;

        [Header("Settings")]
        [Tooltip("Determines whether to induce the lookAt on collider trigger")] 
        [SerializeField] private bool canRepeatLookAt = true;
        [Tooltip("Determines whether to induce the lookAt on collider trigger")] 
        [SerializeField] private bool lookAtOnTrigger = false;
        [Tooltip("How many seconds this lookAt will last")] 
        [SerializeField] private float lookAtDurationSeconds = 5f;

        private float _lookAtTimer;
        private bool _isLooking;
        private bool _hasLooked;

        public void StartLookAt() {
            _hasLooked = true;
            _isLooking = true;
            _lookAtTimer = lookAtDurationSeconds;
            GameEventBus.Raise(GameplayEvents.StartLookAtPoint, targetLookAt);
            GameEventBus.Raise(GameplayEvents.MaxZoom);
            Debug.Log("LookAt started");
        }
        
        public void EndLookAt() {
            _isLooking = false;
            _lookAtTimer = lookAtDurationSeconds;
            GameEventBus.Raise(GameplayEvents.EndLookAtPoint);
            GameEventBus.Raise(GameplayEvents.DefaultZoom);
            Debug.Log("LookAt ended");
        }
        
        public void Update() {
            if (!_isLooking) return;
            _lookAtTimer -= Time.deltaTime;
            if (_lookAtTimer <= 0) EndLookAt();
        }

        private void OnTriggerEnter(Collider other) {
            if (!canRepeatLookAt && _hasLooked) return;
            if (!lookAtOnTrigger) return;
            if (other.CompareTag("Player")) StartLookAt();
        }
    }
}
