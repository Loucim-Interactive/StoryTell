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
        private bool collected, inspecting, ended;
        private int shots;
        public WeaponController Weapon => weapon;
        public bool HasRifle => collected;
        public bool Completed => ended;
        public int ShotsFired => shots;
        public float Darkness => stress.Darkness;
        public bool CanUseWeapon => collected && !ended && !inspecting && (map == null || !map.IsOpen)
            && Cursor.lockState == CursorLockMode.Locked && IsInsideZone;
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
        }
        private void BeginInspect(GameObject _) { inspecting = true; weapon.SetAim(false); }
        private void EndInspect() { inspecting = false; }
        public bool CollectRifle()
        {
            if (collected || ended || !IsInsideZone) return false;
            collected = true;
            // Preserve any ammunition collected before the rifle.
            weapon.Equip();
            weapon.SetInputEnabled(true);
            return true;
        }
        public bool CollectAmmunition(int count)
        {
            return !ended && weapon.MagazineInventory.AddFullMagazines(count) > 0;
        }
        private void Start()
        {
            weapon.BeginSequence(weapon.Definition.MagazineCapacity, 0);
            weapon.EndSequence();
            completion.gameObject.SetActive(false);
        }
        private void OnShot()
        {
            shots++;
            stress.RecordShot(Time.time);
            if (stress.Completed) EndScene();
        }
        private void EndScene()
        {
            ended = true;
            // Defer holstering until TryFire has finished resolving its final shot.
            completion.gameObject.SetActive(true);
            if (map != null) { map.SetOpen(false, true); map.enabled = false; }
            movement.enabled = false;
            look.SetInputLocked(true);
            interaction.enabled = false;
        }
        private void Update()
        {
            stress.Tick(Time.time, Time.deltaTime);
            if (ended && weapon.IsEquipped) weapon.EndSequence();
            Color color = Color.black; color.a = stress.Darkness; darkness.color = color;
            bool visible = collected && !ended && !inspecting && (map == null || !map.IsOpen);
            heldVisual.SetActive(visible);
            viewCamera.enabled = visible;
            viewCamera.fieldOfView = firingCamera.fieldOfView;
            status.text = ended ? "" : !collected ? "PRUEBA DE ARMAS\nBuscá el FAL y los cargadores en el piso, junto al inicio.\nMirá el objeto y pulsá ESPACIO para recogerlo." :
                $"FN FAL    {weapon.RoundsInMagazine:00} / {weapon.MagazineInventory.TotalReserveRounds:00}\n" +
                (weapon.State == WeaponState.Jammed ? "ENCASQUILLADA · R para destrabar" : weapon.State == WeaponState.Reloading ? "Recargando…" : weapon.State == WeaponState.ClearingJam ? "Destrabando…" : !IsInsideZone ? "Volvé a la zona de prueba para disparar" : map != null && map.IsOpen ? "Cerrá el mapa para usar el arma" :
                "CLIC IZQ mantener: disparar · CLIC DER: apuntar · R: recargar\nLas ráfagas oscurecen la visión; soltá el gatillo para recuperarla.");
        }
    }
}
