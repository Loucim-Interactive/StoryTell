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
        private Vector3 weaponShake;
        private float pitchOffset;
        public float PitchOffset => pitchOffset;

        private void Awake() {
            if (!cameraController) cameraController = GetComponentInParent<CameraControllerScript>();
        }

        public void ApplyRecoil() {
            if (!isActiveAndEnabled) return;
            weaponShake = new Vector3(Random.Range(-0.14f,0.14f), Random.Range(-0.10f,0.10f), Random.Range(-0.08f,0.08f));
            pitchOffset = Mathf.Max(-Mathf.Max(0, maxRecoil), pitchOffset - Mathf.Max(0, recoilIntensity));
            if (cameraController) cameraController.SetRecoilPitchOffset(pitchOffset);
        }

        private void Update() {
            weaponShake = Vector3.Lerp(weaponShake, Vector3.zero, 1-Mathf.Exp(-18*Time.deltaTime)); if (cameraController) cameraController.SetWeaponShakeRotationOffset(weaponShake);
            pitchOffset = Mathf.MoveTowards(pitchOffset, 0, Mathf.Max(.01f, recoverySpeed) * Time.deltaTime);
            if (cameraController) cameraController.SetRecoilPitchOffset(pitchOffset);
        }

        public void Clear() {
            pitchOffset = 0; weaponShake = Vector3.zero; if (cameraController) cameraController.SetWeaponShakeRotationOffset(Vector3.zero);
            if (cameraController) cameraController.SetRecoilPitchOffset(0);
        }
        private void OnDisable() => Clear();
    }
}
