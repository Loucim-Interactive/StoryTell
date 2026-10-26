using System;
using UnityEngine;

namespace Systems.WeaponSystem {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponRecoil))]
    public sealed class PlayerWeaponController : MonoBehaviour {
        [SerializeField] private GameObject falVisual;
        [SerializeField] private WeaponRecoil recoil;
        [SerializeField, Min(.01f)] private float fireInterval = .15f;
        private float nextShotTime;
        public string EquippedWeapon { get; private set; }
        public bool CanShoot => isActiveAndEnabled && EquippedWeapon == "FAL";
        public event Action ShotFired;

        private void Awake() {
            if (!recoil) recoil = GetComponent<WeaponRecoil>();
            if (falVisual) falVisual.SetActive(false);
        }

        public bool EquipWeapon(string weaponId) {
            if (!string.Equals(weaponId, "FAL", StringComparison.Ordinal)) return false;
            EquippedWeapon = weaponId;
            if (falVisual) falVisual.SetActive(true);
            return true;
        }

        public void HolsterWeapon() {
            EquippedWeapon = null;
            if (falVisual) falVisual.SetActive(false);
            if (recoil) recoil.Clear();
        }

        public bool TryShoot() {
            if (!CanShoot || Time.timeScale <= 0 || Time.time < nextShotTime) return false;
            nextShotTime = Time.time + Mathf.Max(.01f, fireInterval);
            ApplyRecoil();
            ShotFired?.Invoke();
            return true;
        }

        public void ApplyRecoil() { if (recoil) recoil.ApplyRecoil(); }
        private void OnDisable() { if (recoil) recoil.Clear(); }
    }
}
