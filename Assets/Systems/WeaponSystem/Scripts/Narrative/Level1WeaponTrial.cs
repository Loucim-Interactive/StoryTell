using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using Systems.EventSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InteractionSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StoryTell.Weapons
{
    public sealed class Level1WeaponTrial : MonoBehaviour
    {
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform player;
        [SerializeField] private Camera firingCamera, viewCamera;
        [SerializeField] private GameObject heldVisual;
        [SerializeField] private BoxCollider useZone;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private PlayerControllerScript movement;
        [SerializeField] private CameraControllerScript look;
        [SerializeField] private InteractionScript interaction;
        [SerializeField] private TMP_Text status, completion;
        [SerializeField] private Image darkness;
        [SerializeField] private WeaponSprayStress stress = new();
        [Header("Presentation")]
        [SerializeField] private WeaponVignetteGraphic vignette;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private CanvasGroup combatHUD, completionGroup;
        [SerializeField] private GameObject ammoPanel;
        [SerializeField, Min(1)] private int finalizationShots = 20;
        [SerializeField, Min(0.1f)] private float fadeDuration = 1.3f;
        [SerializeField, Min(0)] private float blackPause = 0.45f;
        [SerializeField, Min(0.1f)] private float titleFadeDuration = 0.75f;
        [SerializeField, Range(0, 1)] private float peripheralDarkness = 1;
        [Tooltip("Disable when an external input context owns the pointer, for example touch controls.")]
        [SerializeField] private bool requireLockedPointer = true;

        private bool collected, inspecting, ended, finishing, controlsLocked;
        private bool mapWasEnabled, movementWasEnabled, interactionWasEnabled;
        private float endingTime, finalAlpha, hintUntil, noticeUntil;
        private string notice;
        private int shots;
        public WeaponController Weapon => weapon;
        public bool HasRifle => collected;
        public bool Completed => ended;
        public bool IsFinishing => finishing;
        public int ShotsFired => shots;
        public int FinalizationShots => finalizationShots;
        public float Darkness => Mathf.Max(stress.Darkness * peripheralDarkness, finalAlpha);
        public float CentralDarkness => finalAlpha;
        public bool CanUseWeapon => collected && !finishing && !inspecting && (map == null || !map.IsOpen)
            && (!requireLockedPointer || Cursor.lockState == CursorLockMode.Locked) && IsInsideZone;
        public bool IsInsideZone
        {
            get
            {
                if (useZone == null || player == null) return false;
                Vector3 p = useZone.transform.InverseTransformPoint(player.position) - useZone.center;
                Vector3 half = useZone.size * 0.5f;
                return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
            }
        }
        private void OnEnable()
        {
            if (weapon != null) weapon.ShotFired += OnShot;
            GameEventBus.Subscribe<GameObject>(GameplayEvents.StartInspection, BeginInspect);
            GameEventBus.Subscribe(GameplayEvents.EndInspection, EndInspect);
        }
        private void OnDisable()
        {
            if (weapon != null) weapon.ShotFired -= OnShot;
            GameEventBus.Unsubscribe<GameObject>(GameplayEvents.StartInspection, BeginInspect);
            GameEventBus.Unsubscribe(GameplayEvents.EndInspection, EndInspect);
            // Restore only the controls this component disabled if its owner is removed at runtime.
            if (controlsLocked)
            {
                if (movement != null) movement.enabled = movementWasEnabled;
                if (interaction != null) interaction.enabled = interactionWasEnabled;
                if (map != null) map.enabled = mapWasEnabled;
                if (look != null) look.SetInputLocked(false);
                controlsLocked = false;
            }
        }
        private void BeginInspect(GameObject _) { inspecting = true; weapon.SetAim(false); }
        private void EndInspect() { inspecting = false; }
        public bool CollectRifle()
        {
            if (collected || finishing || !IsInsideZone) return false;
            collected = true;
            weapon.Equip();
            weapon.SetInputEnabled(true);
            hintUntil = Time.unscaledTime + 9;
            ShowNotice("FN FAL recogido", 2);
            return true;
        }
        public bool CollectAmmunition(int count) => CollectMagazineCount(count) > 0;
        public int CollectMagazineCount(int count)
        {
            if (finishing) return 0;
            int added = weapon.MagazineInventory.AddFullMagazines(count);
            ShowNotice(added > 0 ? $"+{added} cargadores" : "No podés llevar más cargadores", 2);
            return added;
        }
        private void ShowNotice(string value, float duration) { notice = value; noticeUntil = Time.unscaledTime + duration; }
        private void Start()
        {
            weapon.BeginSequence(weapon.Definition.MagazineCapacity, 0);
            weapon.EndSequence();
            completion.gameObject.SetActive(false);
            if (completionGroup != null) completionGroup.alpha = 0;
        }
        private void OnShot()
        {
            shots++;
            stress.RecordShot(Time.time);
            // Story progression is persistent; temporary spray stress may recover between shots.
            if (shots >= Mathf.Max(1, finalizationShots))
            {
                finishing = true;
                weapon.SetInputEnabled(false);
                if (map != null) map.SetOpen(false, true);
            }
        }
        private void UpdateEnding()
        {
            if (!finishing || ended) return;
            endingTime += Time.unscaledDeltaTime;
            finalAlpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(endingTime / Mathf.Max(0.1f, fadeDuration)));
            if (finalAlpha >= 0.995f && !controlsLocked)
            {
                controlsLocked = true;
                movementWasEnabled = movement.enabled; movement.enabled = false;
                interactionWasEnabled = interaction.enabled; interaction.enabled = false;
                mapWasEnabled = map != null && map.enabled;
                if (map != null) { map.SetOpen(false, true); map.enabled = false; }
                look.SetInputLocked(true);
                weapon.EndSequence();
            }
            float titleTime = endingTime - fadeDuration - blackPause;
            if (titleTime >= 0)
            {
                completion.gameObject.SetActive(true);
                if (completionGroup != null) completionGroup.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(titleTime / Mathf.Max(0.1f, titleFadeDuration)));
                ended = titleTime >= titleFadeDuration;
            }
        }
        private void Update()
        {
            stress.Tick(Time.time, Time.deltaTime);
            UpdateEnding();
            if (vignette != null) vignette.Intensity = stress.Darkness * peripheralDarkness;
            Color color = Color.black; color.a = finalAlpha; darkness.color = color;
            bool visible = collected && finalAlpha < 0.995f && !inspecting && (map == null || !map.IsOpen);
            heldVisual.SetActive(visible);
            viewCamera.enabled = visible;
            viewCamera.fieldOfView = firingCamera.fieldOfView;
            viewCamera.aspect = firingCamera.aspect;
            if (combatHUD != null) combatHUD.alpha = inspecting || (map != null && map.IsOpen) ? 0 : 1 - finalAlpha;
            UpdateHUD();
        }
        private void UpdateHUD()
        {
            if (ammoPanel != null) ammoPanel.SetActive(collected && !finishing);
            if (status != null)
            {
                status.text = !collected || finishing ? "" : $"<size=18>FN FAL</size>\n<size=36>{weapon.RoundsInMagazine:00}</size> <color=#AAB2B5>/ {weapon.MagazineInventory.TotalReserveRounds:00}</color>";
            }
            if (hint == null) return;
            if (finishing) { hint.text = ""; return; }
            var pickup = interaction != null ? interaction.CurrentTarget as WeaponTrialPickup : null;
            if (pickup != null)
            {
                hint.text = pickup.IsAmmunition ? "<color=#D8C59C>[E]</color> Recoger cargadores" : "<color=#D8C59C>[E]</color> Recoger FN FAL";
                return;
            }
            if (Time.unscaledTime < noticeUntil) { hint.text = notice; return; }
            if (!collected) { hint.text = "Recogé el FAL y los cargadores junto al inicio"; return; }
            hint.text = weapon.State == WeaponState.Jammed ? "Arma encasquillada · <color=#D8C59C>[R]</color> Destrabar" :
                weapon.State == WeaponState.Reloading ? "Recargando…" : weapon.State == WeaponState.ClearingJam ? "Destrabando…" :
                !IsInsideZone ? "Volvé a la zona de prueba para usar el arma" : weapon.RoundsInMagazine == 0 ?
                (weapon.MagazineInventory.MagazineCount > 0 ? "<color=#D8C59C>[R]</color> Recargar" : "Sin munición · Buscá cargadores") :
                Time.unscaledTime < hintUntil ? "CLIC IZQ · Disparar     CLIC DER · Apuntar     R · Recargar" : "";
        }
    }
}
