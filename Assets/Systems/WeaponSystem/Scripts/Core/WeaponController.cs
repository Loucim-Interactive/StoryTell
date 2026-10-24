using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace StoryTell.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MagazineInventory), typeof(WeaponCondition))]
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private MagazineInventory magazineInventory;
        [SerializeField] private WeaponCondition condition;
        [SerializeField] private Camera firingCamera;
        private Transform firingOwner;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private Level1WeaponTrial useRestriction;

        [Header("View")]
        [SerializeField] private GameObject weaponVisual;
        [SerializeField] private Transform weaponView;
        [SerializeField] private Transform hipPose;
        [SerializeField] private Transform aimPose;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Animator animator;
        [SerializeField] private string fireTrigger = "Fire";
        [SerializeField] private string reloadTrigger = "Reload";
        [SerializeField] private string jamTrigger = "Jam";
        [SerializeField] private string clearJamTrigger = "ClearJam";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioClip emptyClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip jamClip;
        [SerializeField] private AudioClip clearJamClip;

        [Header("Startup")]
        [SerializeField] private bool startEquipped;
        [SerializeField] private bool inputEnabled = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onEquipped;
        [SerializeField] private UnityEvent onHolstered;
        [SerializeField] private UnityEvent onShot;
        [SerializeField] private UnityEvent onImpact;
        [SerializeField] private UnityEvent onEmpty;
        [SerializeField] private UnityEvent onReloadStarted;
        [SerializeField] private UnityEvent onReloadCompleted;
        [SerializeField] private UnityEvent onJammed;
        [SerializeField] private UnityEvent onJamCleared;

        private Coroutine reloadRoutine;
        private Coroutine clearJamRoutine;
        private int roundsInMagazine;
        private int pendingMagazineRounds;
        private float nextFireTime;
        private float defaultFieldOfView = 60f;
        private bool isAiming;
        private readonly RaycastHit[] shotHits = new RaycastHit[64];

        public event Action ShotFired;
        public event Action<WeaponState> StateChanged;
        public event Action<int> MagazineRoundsChanged;
        public event Action<RaycastHit> ShotHit;

        public WeaponDefinition Definition => definition;
        public MagazineInventory MagazineInventory => magazineInventory;
        public WeaponCondition Condition => condition;
        public WeaponState State { get; private set; } = WeaponState.Holstered;
        public int RoundsInMagazine => roundsInMagazine;
        public bool IsAiming => isAiming;
        public bool InputEnabled => inputEnabled;
        public bool IsEquipped => State != WeaponState.Holstered && State != WeaponState.Disabled;

        private void Awake()
        {
            ResolveReferences();
            if (firingCamera != null)
                defaultFieldOfView = firingCamera.fieldOfView;

            if (definition != null && magazineInventory != null)
                magazineInventory.Configure(definition.MagazineCapacity, definition.MaxCarriedMagazines);

            if (startEquipped)
                Equip();
            else
                ApplyHolsteredVisualState();
        }

        private void Update()
        {
            UpdateAimPresentation();
        }

        public void Configure(
            WeaponDefinition weaponDefinition,
            MagazineInventory inventory = null,
            WeaponCondition weaponCondition = null,
            Camera camera = null)
        {
            definition = weaponDefinition;
            if (inventory != null)
                magazineInventory = inventory;
            if (weaponCondition != null)
                condition = weaponCondition;
            if (camera != null)
                firingCamera = camera;

            ResolveReferences();
            if (definition != null && magazineInventory != null)
                magazineInventory.Configure(definition.MagazineCapacity, definition.MaxCarriedMagazines);
            if (firingCamera != null)
                defaultFieldOfView = firingCamera.fieldOfView;
        }

        public void ConfigurePresentation(
            GameObject visual,
            Transform view,
            Transform hip,
            Transform aim)
        {
            weaponVisual = visual;
            weaponView = view;
            hipPose = hip;
            aimPose = aim;
        }

        public void BeginSequence(int loadedRounds = -1, int fullReserveMagazines = 0, bool cleanWeapon = true)
        {
            if (definition == null)
            {
                Debug.LogWarning("WeaponController needs a WeaponDefinition before beginning a sequence.", this);
                return;
            }

            CancelTimedActions(true);
            magazineInventory.Configure(definition.MagazineCapacity, definition.MaxCarriedMagazines);
            magazineInventory.Clear();
            magazineInventory.AddFullMagazines(fullReserveMagazines);
            roundsInMagazine = loadedRounds < 0
                ? definition.MagazineCapacity
                : Mathf.Clamp(loadedRounds, 0, definition.MagazineCapacity);
            MagazineRoundsChanged?.Invoke(roundsInMagazine);
            if (cleanWeapon)
                condition.CleanWeapon();
            inputEnabled = true;
            Equip();
        }

        public void EndSequence()
        {
            inputEnabled = false;
            Holster();
        }

        public void Equip()
        {
            if (definition == null)
                return;

            if (weaponVisual != null)
                weaponVisual.SetActive(true);
            SetState(WeaponState.Ready);
            onEquipped?.Invoke();
        }

        public void Holster()
        {
            CancelTimedActions(true);
            SetAim(false);
            SetState(WeaponState.Holstered);
            ApplyHolsteredVisualState();
            onHolstered?.Invoke();
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (!value)
                SetAim(false);
        }

        public void SetAim(bool value)
        {
            bool canAim = inputEnabled && (State == WeaponState.Ready || State == WeaponState.Aiming);
            isAiming = value && canAim;
            if (State == WeaponState.Ready || State == WeaponState.Aiming)
                SetState(isAiming ? WeaponState.Aiming : WeaponState.Ready);
        }

        public WeaponFireResult TryFire()
        {
            if ((useRestriction != null && !useRestriction.CanUseWeapon) || !inputEnabled || definition == null ||
                (State != WeaponState.Ready && State != WeaponState.Aiming))
                return WeaponFireResult.NotReady;

            if (Time.time < nextFireTime)
                return WeaponFireResult.Cooldown;

            if (roundsInMagazine <= 0)
            {
                PlayOneShot(emptyClip);
                onEmpty?.Invoke();
                return WeaponFireResult.Empty;
            }

            if (condition != null && condition.EvaluateShotForJam())
            {
                isAiming = false;
                SetState(WeaponState.Jammed);
                TriggerAnimator(jamTrigger);
                PlayOneShot(jamClip);
                onJammed?.Invoke();
                return WeaponFireResult.Jammed;
            }

            roundsInMagazine--;
            MagazineRoundsChanged?.Invoke(roundsInMagazine);
            nextFireTime = Time.time + definition.FireInterval;

            bool wasAiming = isAiming;
            SetState(WeaponState.Firing);
            TriggerAnimator(fireTrigger);
            if (muzzleFlash != null)
                muzzleFlash.Play(true);
            PlayOneShot(fireClip);
            onShot?.Invoke();
            PerformHitscan(wasAiming);
            SetState(wasAiming ? WeaponState.Aiming : WeaponState.Ready);
            ShotFired?.Invoke();
            return WeaponFireResult.Fired;
        }

        public bool TryReload()
        {
            if (!inputEnabled || definition == null)
                return false;

            if (State == WeaponState.Jammed)
                return BeginClearJam();

            if ((State != WeaponState.Ready && State != WeaponState.Aiming) ||
                roundsInMagazine >= definition.MagazineCapacity ||
                !magazineInventory.TryTakeNextMagazine(out pendingMagazineRounds))
                return false;

            // The removed magazine is deliberately discarded with every round it still contained.
            roundsInMagazine = 0;
            MagazineRoundsChanged?.Invoke(roundsInMagazine);
            isAiming = false;
            SetState(WeaponState.Reloading);
            TriggerAnimator(reloadTrigger);
            PlayOneShot(reloadClip);
            onReloadStarted?.Invoke();
            reloadRoutine = StartCoroutine(ReloadAfterDelay(definition.ReloadDuration));
            return true;
        }

        public void CompleteReload()
        {
            if (State != WeaponState.Reloading)
                return;

            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }

            roundsInMagazine = Mathf.Clamp(pendingMagazineRounds, 0, definition.MagazineCapacity);
            pendingMagazineRounds = 0;
            MagazineRoundsChanged?.Invoke(roundsInMagazine);
            SetState(WeaponState.Ready);
            onReloadCompleted?.Invoke();
        }

        public bool BeginClearJam()
        {
            if ((useRestriction != null && !useRestriction.CanUseWeapon) || !inputEnabled || definition == null || State != WeaponState.Jammed)
                return false;

            SetState(WeaponState.ClearingJam);
            TriggerAnimator(clearJamTrigger);
            PlayOneShot(clearJamClip);
            clearJamRoutine = StartCoroutine(ClearJamAfterDelay(definition.ClearJamDuration));
            return true;
        }

        public void CompleteClearJam()
        {
            if (State != WeaponState.ClearingJam)
                return;

            if (clearJamRoutine != null)
            {
                StopCoroutine(clearJamRoutine);
                clearJamRoutine = null;
            }

            SetState(WeaponState.Ready);
            onJamCleared?.Invoke();
        }

        public void ForceNextJam()
        {
            condition?.ForceNextJam();
        }

        public void ApplyContamination(WeaponContamination contamination)
        {
            condition?.ApplyContamination(contamination);
        }

        private void PerformHitscan(bool aimed)
        {
            if (firingCamera == null)
                return;

            float spread = aimed ? definition.AimSpreadDegrees : definition.HipSpreadDegrees;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 direction = (firingCamera.transform.forward +
                                 firingCamera.transform.right * offset.x +
                                 firingCamera.transform.up * offset.y).normalized;

            int count = Physics.RaycastNonAlloc(firingCamera.transform.position, direction, shotHits,
                definition.Range, hitMask, QueryTriggerInteraction.Ignore);
            RaycastHit hit = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (shotHits[i].collider.transform.IsChildOf(firingOwner != null ? firingOwner : transform) || shotHits[i].distance >= nearest) continue;
                hit = shotHits[i]; nearest = hit.distance;
            }
            if (float.IsPositiveInfinity(nearest)) return;

            onImpact?.Invoke();
            ShotHit?.Invoke(hit);
            IWeaponDamageable damageable = hit.collider.GetComponentInParent<IWeaponDamageable>();
            damageable?.ReceiveWeaponDamage(definition.Damage, hit.point, direction, gameObject);
        }

        private IEnumerator ReloadAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            reloadRoutine = null;
            CompleteReload();
        }

        private IEnumerator ClearJamAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            clearJamRoutine = null;
            CompleteClearJam();
        }

        private void UpdateAimPresentation()
        {
            if (definition == null || !IsEquipped)
                return;

            float speed = definition.AimTransitionSpeed * Time.deltaTime;
            Transform targetPose = isAiming ? aimPose : hipPose;
            if (weaponView != null && targetPose != null)
            {
                weaponView.localPosition = Vector3.Lerp(weaponView.localPosition, targetPose.localPosition, speed);
                weaponView.localRotation = Quaternion.Slerp(weaponView.localRotation, targetPose.localRotation, speed);
            }

            if (firingCamera != null)
            {
                float targetFov = isAiming ? definition.AimFieldOfView : defaultFieldOfView;
                firingCamera.fieldOfView = Mathf.Lerp(firingCamera.fieldOfView, targetFov, speed);
            }
        }

        private void CancelTimedActions(bool returnPendingMagazine)
        {
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
            if (clearJamRoutine != null)
            {
                StopCoroutine(clearJamRoutine);
                clearJamRoutine = null;
            }

            if (returnPendingMagazine && pendingMagazineRounds > 0 && magazineInventory != null)
                magazineInventory.AddMagazine(pendingMagazineRounds);
            pendingMagazineRounds = 0;
        }

        private void ResolveReferences()
        {
            if (firingOwner == null) { var character = GetComponentInParent<CharacterController>(); firingOwner = character != null ? character.transform : transform; }
            if (magazineInventory == null)
                TryGetComponent(out magazineInventory);
            if (condition == null)
                TryGetComponent(out condition);
            if (firingCamera == null)
                firingCamera = Camera.main;
            if (audioSource == null)
                TryGetComponent(out audioSource);
        }

        private void SetState(WeaponState value)
        {
            if (State == value)
                return;
            State = value;
            StateChanged?.Invoke(State);
        }

        private void ApplyHolsteredVisualState()
        {
            if (weaponVisual != null)
                weaponVisual.SetActive(false);
            if (firingCamera != null)
                firingCamera.fieldOfView = defaultFieldOfView;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void TriggerAnimator(string parameterName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName))
                return;
            for (int i = 0; i < animator.parameters.Length; i++)
            {
                if (animator.parameters[i].type == AnimatorControllerParameterType.Trigger &&
                    animator.parameters[i].name == parameterName)
                {
                    animator.SetTrigger(parameterName);
                    return;
                }
            }
        }

        private void OnDisable()
        {
            CancelTimedActions(true);
            isAiming = false;
            if (firingCamera != null)
                firingCamera.fieldOfView = defaultFieldOfView;
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
