using System.Collections;
using InteractionSystem.Scripts;
using UnityEngine;

namespace Systems.InteractionSystem.Scripts.Interactables.Chests {
    public class SimpleChestInteractableScript : InteractableScript {
        public bool canOpen = true;
        public Transform ChestLid;
        public float hingeAmount = 90f;
        public float lidSpeed = 5f;

        private bool _isOpen;
        private bool _isRotating;
        private float _initialRot;

        private void Start() {
            if (ChestLid == null) {
                Debug.LogError("ChestLid is not assigned.", this);
                return;
            }

            _initialRot = ChestLid.localEulerAngles.x;
        }

        protected override void OnInteract() {
            if (!canOpen) return;
            if (_isRotating) return;

            if (!_isOpen) {
                OpenChest();
            }
        }

        private IEnumerator RotateLid(float degrees) {
            _isRotating = true;

            float targetRotation = _initialRot + degrees;

            Quaternion startRot = ChestLid.localRotation;
            Quaternion targetRot = Quaternion.Euler(targetRotation, 0f, 0f);

            while (Quaternion.Angle(ChestLid.localRotation, targetRot) > 0.1f) {
                ChestLid.localRotation = Quaternion.Slerp(
                    ChestLid.localRotation,
                    targetRot,
                    lidSpeed * Time.deltaTime
                );

                yield return null;
            }

            ChestLid.localRotation = targetRot;
            _isRotating = false;
        }

        private void OpenChest() {
            _isOpen = true;
            StartCoroutine(RotateLid(hingeAmount));
        }
    }
}