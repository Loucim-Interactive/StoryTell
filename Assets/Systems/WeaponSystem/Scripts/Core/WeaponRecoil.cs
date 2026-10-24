using FirstPersonControllerSystem.Scripts.Input;
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
        [Header("Presentation and comfort")]
        [SerializeField, Range(0, 1)] private float shakeIntensity = 0.65f;
        [SerializeField, Range(0, 1)] private float recoilIntensity = 1;
        [SerializeField, Range(0, 1)] private float swayIntensity = 0.5f;
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField] private AudioSource weaponAudio;
        private InputScript input;
        private Vector3 kick, smoothNoise;
        private Vector2 sway;
        private float shake, equippedBlend, stateTime;
        private WeaponState previousState;
        public float CurrentKick => kick.magnitude;
        private void Awake() { if (look != null) input = look.GetComponent<InputScript>(); }
        private void OnEnable() { if (weapon != null) { weapon.ShotFired += OnShot; weapon.StateChanged += OnStateChanged; } }
        private void OnDisable()
        {
            if (weapon != null) { weapon.ShotFired -= OnShot; weapon.StateChanged -= OnStateChanged; }
            if (kickPivot != null) { kickPivot.localPosition = Vector3.zero; kickPivot.localRotation = Quaternion.identity; }
            if (look != null) look.SetWeaponShakeRotationOffset(Vector3.zero);
            if (weaponAudio != null) weaponAudio.pitch = 1;
        }
        private void OnStateChanged(WeaponState state)
        {
            if (state == WeaponState.Firing && weaponAudio != null) weaponAudio.pitch = Random.Range(0.97f, 1.03f);
            if (state == WeaponState.Reloading || state == WeaponState.ClearingJam)
            {
                stateTime = 0;
                if (weaponAudio != null) weaponAudio.pitch = 1;
            }
            if (state == WeaponState.Ready && previousState == WeaponState.Holstered) equippedBlend = 0;
            previousState = state;
        }
        private void OnShot()
        {
            if (look != null) look.SetCameraPitch(look.CameraPitch - pitchKick * recoilIntensity * (weapon.IsAiming ? 0.65f : 1));
            kick += new Vector3(-2, Random.Range(-0.5f, 0.5f), 0) * recoilIntensity;
            kick = Vector3.ClampMagnitude(kick, 7);
            shake = shakeDegrees * shakeIntensity;
        }
        private void LateUpdate()
        {
            if (kickPivot == null || weapon == null) return;
            float delta = Time.deltaTime;
            float blend = 1 - Mathf.Exp(-recovery * delta);
            kick = Vector3.Lerp(kick, Vector3.zero, blend);
            shake = Mathf.MoveTowards(shake, 0, delta * 3);
            Vector3 noise = shake > 0.001f ? Random.insideUnitSphere * shake : Vector3.zero;
            smoothNoise = Vector3.Lerp(smoothNoise, noise, 1 - Mathf.Exp(-25 * delta));
            if (look != null) look.SetWeaponShakeRotationOffset(smoothNoise);
            Vector2 desiredSway = input == null ? Vector2.zero : Vector2.ClampMagnitude(input.Look, 15) * (weapon.IsAiming ? 0.025f : 0.07f) * swayIntensity;
            sway = Vector2.Lerp(sway, desiredSway, blend);
            equippedBlend = Mathf.MoveTowards(equippedBlend, weapon.IsEquipped && (trial == null || !trial.IsFinishing) ? 1 : 0, delta * 3);
            float handling = 0;
            if (weapon.State == WeaponState.Reloading || weapon.State == WeaponState.ClearingJam)
            {
                stateTime += delta;
                float duration = weapon.State == WeaponState.Reloading ? weapon.Definition.ReloadDuration : weapon.Definition.ClearJamDuration;
                handling = Mathf.Sin(Mathf.Clamp01(stateTime / Mathf.Max(0.1f, duration)) * Mathf.PI);
            }
            float walk = input != null ? Mathf.Clamp01(input.Move.magnitude) * swayIntensity * (weapon.IsAiming ? 0.2f : 1) : 0;
            float bob = Mathf.Sin(Time.time * 9) * walk;
            float lowered = 1 - Mathf.SmoothStep(0, 1, equippedBlend);
            kickPivot.localRotation = Quaternion.Euler(kick + new Vector3(sway.y + handling * 13 + lowered * 18, -sway.x, -handling * 22 + bob * 0.5f));
            kickPivot.localPosition = new Vector3(bob * 0.003f, -handling * 0.065f - lowered * 0.22f + Mathf.Abs(bob) * 0.003f, -Mathf.Min(0.07f, kick.magnitude * 0.012f));
        }
    }
}
