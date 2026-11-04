using InmersionSystem.Ambient.Configs;
using StoryTell.MainIntegration;
using UnityEngine;
namespace InmersionSystem.Ambient.Effects
{
 public class ColdBreathEffect:AmbientEffect<ColdBreathConfig>
 {
  [SerializeField]private ParticleSystem breathParticles;
  [SerializeField]private AudioSource audioSource;
  [SerializeField]private Rigidbody characterRigidbody;
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField,Min(0)]private float intervalVariation=1.2f;
  private CharacterController controller;
  private bool running,emitting;
  private float nextExhale,stopAt;
  public int ExhalationCount{get;private set;}
  public bool IsRunning=>running;
  public ParticleSystem Particles=>breathParticles;
  public float Interval=>config?config.breathInterval:9;
  private void Awake()
  {
   controller=GetComponentInParent<CharacterController>();
   if(!breathParticles)return;
   var main=breathParticles.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.simulationSpeed=1;
   breathParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  }
  private void OnEnable(){if(config&&config.enabledByDefault)OnEffectStart();}
  private void OnDisable()=>OnEffectStop();
  public override void OnEffectStart()
  {
   if(running||!isActiveAndEnabled||!config||!breathParticles)return;
   running=true;nextExhale=Time.time+Random.Range(3,5);
  }
  public override void OnEffectStop()
  {
   running=emitting=false;
   if(breathParticles)breathParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  }
  private void Update()
  {
   if(!running||!breathParticles||!config||Time.timeScale<=0)return;
   if(session&&session.Mode!=UiMode.None)
   {
    if(breathParticles.particleCount>0||emitting)breathParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    emitting=false;nextExhale=Mathf.Max(nextExhale,Time.time+1.5f);return;
   }
   if(emitting&&Time.time>=stopAt){breathParticles.Stop(true,ParticleSystemStopBehavior.StopEmitting);emitting=false;}
   if(Time.time<nextExhale)return;
   Vector3 motion=controller?controller.velocity:characterRigidbody?characterRigidbody.linearVelocity:Vector3.zero;
   var velocity=breathParticles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=motion.x*.2f+.025f;velocity.y=.025f;velocity.z=motion.z*.2f+.015f;
   breathParticles.Play(true);emitting=true;stopAt=Time.time+Mathf.Clamp(config.breathDuration,.25f,.65f);
   nextExhale=Time.time+Mathf.Max(6,config.breathInterval+Random.Range(-intervalVariation,intervalVariation));ExhalationCount++;
   if(audioSource&&config.exhaleSound)audioSource.PlayOneShot(config.exhaleSound,config.exhaleVolume);
  }
 }
}
