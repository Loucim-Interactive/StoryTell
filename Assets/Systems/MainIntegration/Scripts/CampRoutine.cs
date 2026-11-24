using System;
using DialogueSystem.Scripts;
using Systems.Missions.FindObjects;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class CampRoutine : MonoBehaviour
 {
  [SerializeField] private EquipmentManager equipment;
  [SerializeField] private LetterManager letter;
  [SerializeField] private SpeakerScript diego, mateo;
  [SerializeField] private ConversationSO beforeFood;
  [SerializeField] private ConversationSO beforeEquipment, beforeBriefing, diegoAfterFood;
  [SerializeField] private MissionAnnouncement feedback;
  [SerializeField] private SupplyDispatch dispatch;
  public bool Briefed {get;private set;}
  public bool Ate {get;private set;}
  public bool ReadyToLeaveCamp=>equipment.MissionCompleted&&Ate&&letter.MissionCompleted&&(!dispatch||dispatch.MayLeaveCamp);
  public bool ReadyForFront=>equipment.MissionCompleted&&Ate&&letter.MissionCompleted&&(!dispatch||dispatch.Delivered);
  public event Action Changed;
  private bool restoring;
  public void SetRestoring(bool value)=>restoring=value;
  private void OnEnable(){diego.ConversationCompleted+=DiegoFinished;mateo.ConversationCompleted+=MateoFinished;}
  private void OnDisable(){diego.ConversationCompleted-=DiegoFinished;mateo.ConversationCompleted-=MateoFinished;}
  private void DiegoFinished(){ReceiveMealInstruction(diego);}
  // The instruction has been given when its conversation opens, even if the player leaves early.
  public void ReceiveMealInstruction(SpeakerScript speaker)
  {
   if(restoring||speaker!=diego||!equipment.MissionCompleted||Briefed)return;
   Briefed=true;Changed?.Invoke();
  }
  private void MateoFinished(){if(!restoring&&Ate&&mateo.HasSpoken&&!letter.MissionStarted)letter.StartMission();}
  public ConversationSO ConversationOverride(SpeakerScript speaker)
  {
   if(speaker==diego&&Ate)return diegoAfterFood;
   if(speaker!=mateo||Ate)return null;
   if(!equipment.MissionCompleted)return beforeEquipment?beforeEquipment:beforeFood;
   if(!Briefed)return beforeBriefing?beforeBriefing:beforeFood;
   return beforeFood;
  }
  public bool Eat()
  {
   if(Ate)return false;
   if(!Briefed){feedback.ShowPickup(equipment.MissionCompleted?"Hablá con Diego primero.":"Primero recogé tu equipo con Diego.");return false;}
   Ate=true;feedback.ShowPickup("Comiste algo caliente.");Changed?.Invoke();return true;
  }
  public void Restore(bool briefed,bool ate){Briefed=briefed;Ate=ate;Changed?.Invoke();}
 }
}
