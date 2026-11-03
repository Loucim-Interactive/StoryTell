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
  [SerializeField] private MissionAnnouncement feedback;
  public bool Briefed {get;private set;}
  public bool Ate {get;private set;}
  public bool ReadyForFront=>equipment.MissionCompleted&&Ate&&letter.MissionCompleted;
  public event Action Changed;
  private bool restoring;
  public void SetRestoring(bool value)=>restoring=value;
  private void OnEnable(){diego.ConversationCompleted+=DiegoFinished;mateo.ConversationCompleted+=MateoFinished;}
  private void OnDisable(){diego.ConversationCompleted-=DiegoFinished;mateo.ConversationCompleted-=MateoFinished;}
  private void DiegoFinished(){if(restoring||!equipment.MissionCompleted||Briefed)return;Briefed=true;Changed?.Invoke();}
  private void MateoFinished(){if(!restoring&&Ate&&mateo.HasSpoken&&!letter.MissionStarted)letter.StartMission();}
  public ConversationSO ConversationOverride(SpeakerScript speaker)=>speaker==mateo&&!Ate?beforeFood:null;
  public bool Eat()
  {
   if(Ate)return false;
   if(!Briefed){feedback.ShowPickup(equipment.MissionCompleted?"Hablá con Diego primero.":"Primero recogé tu equipo con Diego.");return false;}
   Ate=true;feedback.ShowPickup("Comiste algo caliente.");Changed?.Invoke();return true;
  }
  public void Restore(bool briefed,bool ate){Briefed=briefed;Ate=ate;Changed?.Invoke();}
 }
}
