using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using UnityEngine;

namespace Systems.WeaponSystem {
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class WeaponRecoil : MonoBehaviour {
        [SerializeField] private CameraControllerScript cameraController;
        [SerializeField, Min(0)] private float recoilIntensity = 1.2f;
        [SerializeField, Min(0)] private float maxRecoil = 6f;
        [SerializeField, Min(.01f)] private float recoverySpeed = 10f;
        private float pitchOffset;
        public float PitchOffset => pitchOffset;

        private void Awake() {
            if (!cameraController) cameraController = GetComponentInParent<CameraControllerScript>();
        }

        public void ApplyRecoil() {
            if (!isActiveAndEnabled) return;
            pitchOffset = Mathf.Max(-Mathf.Max(0, maxRecoil), pitchOffset - Mathf.Max(0, recoilIntensity));
            if (cameraController) cameraController.SetRecoilPitchOffset(pitchOffset);
        }

        private void Update() {
            pitchOffset = Mathf.MoveTowards(pitchOffset, 0, Mathf.Max(.01f, recoverySpeed) * Time.deltaTime);
            if (cameraController) cameraController.SetRecoilPitchOffset(pitchOffset);
        }

        public void Clear() {
            pitchOffset = 0;
            if (cameraController) cameraController.SetRecoilPitchOffset(0);
        }
        private void OnDisable() => Clear();
    }
}
