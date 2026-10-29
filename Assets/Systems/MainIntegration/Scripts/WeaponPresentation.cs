using Systems.WeaponSystem;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(300)]
    public sealed class WeaponPresentation : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController weapon;
        [SerializeField] private Transform visual, muzzle;
        [SerializeField] private ParticleSystem flash, smoke;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private AudioSource mechanismAudio;
        [SerializeField] private AudioClip mechanismClip;
        private Vector3 restPosition;
        private float kick, lightUntil;
        private bool wasReloading;
        private void Awake()
        {
            restPosition=visual.localPosition;
            // Tiny one-shot effects must expire even outside the camera frustum.
            if(flash){var main=flash.main;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;}
            if(smoke){var main=smoke.main;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;}
        }
        private void OnEnable(){weapon.ShotFired+=OnShot;}
        private void OnDisable(){weapon.ShotFired-=OnShot;if(visual)visual.localPosition=restPosition;if(muzzleLight)muzzleLight.enabled=false;if(flash)flash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        private void OnShot()
        {
            kick=Mathf.Min(kick+.025f,.065f);lightUntil=Time.time+.035f;
            if(flash){flash.Play(true);flash.Emit(1);}if(smoke){smoke.Play(true);smoke.Emit(2);}
        }
        private void LateUpdate()
        {
            if(weapon.Reloading!=wasReloading){if(mechanismAudio&&mechanismClip)mechanismAudio.PlayOneShot(mechanismClip,.3f);wasReloading=weapon.Reloading;}
            kick=Mathf.MoveTowards(kick,0,Time.deltaTime*.18f);
            if(visual)visual.localPosition=restPosition+new Vector3(0,weapon.Reloading?-.07f:Mathf.Sin(Time.time*1.3f)*.0015f,-kick);
            if(muzzleLight)muzzleLight.enabled=weapon.CanShoot&&Time.time<lightUntil;
            // Hiding the rifle pauses its child systems. Expire the flash from the
            // persistent player component so it cannot reappear on the next equip.
            if(flash&&Time.time>=lightUntil+.02f&&flash.particleCount>0)
                flash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
