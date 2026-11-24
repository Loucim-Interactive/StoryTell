using System;
using DialogueSystem.Scripts;
using StoryTell.MainIntegration;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class CampSocialGroup:MonoBehaviour
 {
  [SerializeField]private CampAmbientDialogue ambient;
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private SpeakerScript[] actors;
  private NPCAnimatorScript active;
  private float until;
  private void OnEnable(){if(ambient)ambient.LineStarted+=Gesture;}
  private void OnDisable(){if(ambient)ambient.LineStarted-=Gesture;Release();}
  public void Gesture(SpeakerScript actor)
  {
   if(session.Mode!=UiMode.None||Array.IndexOf(actors,actor)<0)return;
   var driver=actor.GetComponentInChildren<NPCAnimatorScript>();if(!driver||driver.IsAnimationLocked)return;
   Release();active=driver;active.LockAnimation(NPCAnimatorScript.AnimationState.Talking);until=Time.time+4;
  }
  private void Update(){if(active&&(Time.time>=until||session.Mode!=UiMode.None||!ambient.IsShowing))Release();}
  private void Release(){if(active)active.UnlockAnimation();active=null;}
 }
}
