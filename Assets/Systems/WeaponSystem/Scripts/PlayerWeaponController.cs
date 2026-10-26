using System;
using StoryTell.MainIntegration;
using TMPro;
using UnityEngine;

namespace Systems.WeaponSystem
{
    [DisallowMultipleComponent, RequireComponent(typeof(WeaponRecoil))]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private GameObject falVisual;
        [SerializeField] private WeaponRecoil recoil;
        [SerializeField,Min(.01f)] private float fireInterval=.15f;
        [SerializeField,Min(1)] private int magazineSize=20, reserveRounds=60;
        [SerializeField,Min(.1f)] private float reloadSeconds=2.1f;
        [SerializeField] private Camera firingCamera;
        [SerializeField] private AudioSource shotAudio;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private TMP_Text ammoLabel;
        [SerializeField] private GameObject ammoPanel;
        [SerializeField] private WeaponVignetteGraphic vignette;
        private readonly WeaponSprayStress stress=new();
        private readonly RaycastHit[] hits=new RaycastHit[64];
        private Systems.InspectionSystem.Scripts.ZoomScript zoom;
        private float nextShotTime,reloadUntil,originalFov;
        private bool presentationBlocked,aiming;
        private Quaternion originalVisualRotation;
        public string EquippedWeapon {get;private set;}
        public int RoundsInMagazine {get;private set;}
        public int ReserveRounds => reserveRounds;
        public bool Reloading {get;private set;}
        public bool CanShoot=>isActiveAndEnabled&&EquippedWeapon=="FAL"&&!presentationBlocked&&!Reloading&&RoundsInMagazine>0;
        public event Action ShotFired;
        private void Awake()
        {
            if(!recoil)recoil=GetComponent<WeaponRecoil>();if(!firingCamera)firingCamera=GetComponentInChildren<Camera>();
            zoom=GetComponentInParent<Systems.InspectionSystem.Scripts.ZoomScript>();
            RoundsInMagazine=magazineSize;originalFov=firingCamera?firingCamera.fieldOfView:60;
            if(falVisual){originalVisualRotation=falVisual.transform.localRotation;falVisual.SetActive(false);}
        }
        public bool EquipWeapon(string weaponId)
        {
            if(!string.Equals(weaponId,"FAL",StringComparison.Ordinal))return false;
            EquippedWeapon=weaponId;if(falVisual)falVisual.SetActive(!presentationBlocked);return true;
        }
        public void HolsterWeapon(){EquippedWeapon=null;Reloading=false;aiming=false;if(falVisual){falVisual.SetActive(false);falVisual.transform.localRotation=originalVisualRotation;}if(recoil)recoil.Clear();if(firingCamera)firingCamera.fieldOfView=originalFov;}
        public void SetPresentationBlocked(bool blocked){presentationBlocked=blocked;if(blocked)aiming=false;if(falVisual)falVisual.SetActive(EquippedWeapon=="FAL"&&!blocked);}
        public void SetAim(bool value){aiming=value&&EquippedWeapon=="FAL"&&!Reloading&&!presentationBlocked;}
        public bool TryReload()
        {
            if(!isActiveAndEnabled||EquippedWeapon!="FAL"||presentationBlocked||Reloading||RoundsInMagazine>=magazineSize||reserveRounds<=0)return false;
            Reloading=true;aiming=false;reloadUntil=Time.time+reloadSeconds;return true;
        }
        public bool TryShoot()
        {
            if(!CanShoot||Time.timeScale<=0||Time.time<nextShotTime)return false;
            nextShotTime=Time.time+Mathf.Max(.01f,fireInterval);RoundsInMagazine--;ApplyRecoil();stress.RecordShot(Time.time,false);
            if(shotAudio&&shotClip)shotAudio.PlayOneShot(shotClip,0.55f);
            if(firingCamera)
            {
                int count=Physics.RaycastNonAlloc(firingCamera.transform.position,firingCamera.transform.forward,hits,250,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                Collider nearest=null;float distance=float.PositiveInfinity;
                for(int i=0;i<count;i++){var h=hits[i];if(h.collider.transform.IsChildOf(transform)||h.distance>=distance)continue;distance=h.distance;nearest=h.collider;}
                var target=nearest?nearest.GetComponentInParent<Systems.BattleSystem.ShadowDamageTarget>():null;if(target)target.ReceiveHit();
            }
            ShotFired?.Invoke();return true;
        }
        public void ApplyRecoil(){if(recoil)recoil.ApplyRecoil();}
        private void Update()
        {
            stress.Tick(Time.time,Time.deltaTime);
            if(Reloading&&Time.time>=reloadUntil){int amount=Mathf.Min(magazineSize-RoundsInMagazine,reserveRounds);RoundsInMagazine+=amount;reserveRounds-=amount;Reloading=false;}
        }
        private void LateUpdate()
        {
            bool visible=EquippedWeapon=="FAL"&&!presentationBlocked;
            if(zoom)zoom.SetExternalFov(visible);
            if(ammoPanel)ammoPanel.SetActive(visible);
            if(ammoLabel)ammoLabel.text="FN FAL\n"+RoundsInMagazine.ToString("00")+" / "+reserveRounds.ToString("00")+(Reloading?"\nRecargando…":"\n[R] Recargar");
            if(vignette)vignette.Intensity=visible?stress.Darkness*0.65f:0;
            if(visible&&firingCamera)firingCamera.fieldOfView=Mathf.Lerp(firingCamera.fieldOfView,aiming?38:originalFov,1-Mathf.Exp(-12*Time.deltaTime));
            if(falVisual)falVisual.transform.localRotation=originalVisualRotation*(Reloading?Quaternion.Euler(-8,0,12)*Quaternion.Euler(0,0,Mathf.Sin(Time.time*4)*2):Quaternion.identity);
        }
        private void OnDisable(){if(zoom)zoom.SetExternalFov(false);if(recoil)recoil.Clear();if(vignette)vignette.Intensity=0;}
    }
}
