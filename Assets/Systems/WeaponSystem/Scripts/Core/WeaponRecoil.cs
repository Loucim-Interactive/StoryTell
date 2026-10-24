using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using UnityEngine;

namespace StoryTell.Weapons
{
    [DefaultExecutionOrder(500)]
    public sealed class WeaponRecoil : MonoBehaviour
    {
        [SerializeField] private WeaponController weapon;
        [SerializeField] private CameraControllerScript look;
        [SerializeField] private Transform kickPivot;
        [SerializeField, Min(0)] private float pitchKick = 0.9f;
        [SerializeField, Min(0)] private float shakeDegrees = 0.35f;
        [SerializeField, Min(0.01f)] private float recovery = 12f;
        private Vector3 kick;
        private float shake;
        public float CurrentKick => kick.magnitude;
        private void OnEnable() { if (weapon != null) weapon.ShotFired += OnShot; }
        private void OnDisable()
        {
            if (weapon != null) weapon.ShotFired -= OnShot;
            if (kickPivot != null) { kickPivot.localPosition = Vector3.zero; kickPivot.localRotation = Quaternion.identity; }
            if (look != null) look.SetWeaponShakeRotationOffset(Vector3.zero);
        }
        private void OnShot()
        {
            if (look != null) look.SetCameraPitch(look.CameraPitch - pitchKick * (weapon.IsAiming ? 0.65f : 1f));
            kick += new Vector3(-2f, Random.Range(-0.5f, 0.5f), 0);
            shake = shakeDegrees;
        }
        private void LateUpdate()
        {
            if (kickPivot == null) return;
            kick = Vector3.Lerp(kick, Vector3.zero, 1f - Mathf.Exp(-recovery * Time.deltaTime));
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 3f);
            if (look != null) look.SetWeaponShakeRotationOffset(Random.insideUnitSphere * shake);
            kickPivot.localRotation = Quaternion.Euler(kick + Random.insideUnitSphere * shake);
            kickPivot.localPosition = new Vector3(0, 0, -Mathf.Min(0.07f, kick.magnitude * 0.012f));
        }
    }
}
