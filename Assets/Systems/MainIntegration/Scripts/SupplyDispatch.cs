using System;
using DialogueSystem.Scripts;
using Systems.Missions.FindObjects;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public enum DispatchStage { None, Sergeant, Outpost, Collect, Carrying, Delivered }
 public sealed class SupplyDispatch:MonoBehaviour
 {
  [SerializeField]private LetterManager letter;
  [SerializeField]private SpeakerScript sergeant,outpost,receiver;
  [SerializeField]private ConversationSO sergeantBlocked,sergeantOrder,sergeantAfter,outpostBlocked,outpostOrder,outpostAfter,receiverBlocked,receiverDelivery,receiverAfter;
  [SerializeField]private GameObject package;
  [SerializeField]private MissionAnnouncement feedback;
  private bool restoring;
  public DispatchStage Stage{get;private set;}
  public bool MayLeaveCamp=>Stage>=DispatchStage.Outpost;
  public bool Delivered=>Stage==DispatchStage.Delivered;
  public bool HasPackage=>Stage==DispatchStage.Carrying;
  public event Action Changed;
  private void OnEnable(){letter.Changed+=LetterChanged;sergeant.ConversationCompleted+=SergeantFinished;outpost.ConversationCompleted+=OutpostFinished;receiver.ConversationCompleted+=ReceiverFinished;}
  private void OnDisable(){letter.Changed-=LetterChanged;sergeant.ConversationCompleted-=SergeantFinished;outpost.ConversationCompleted-=OutpostFinished;receiver.ConversationCompleted-=ReceiverFinished;}
  private void Start()=>Refresh();
  public void SetRestoring(bool value)=>restoring=value;
  private void LetterChanged(){if(!restoring&&letter.MissionCompleted&&Stage==DispatchStage.None)Advance(DispatchStage.Sergeant);}
  private void SergeantFinished(){if(!restoring&&letter.MissionCompleted&&Stage==DispatchStage.Sergeant)Advance(DispatchStage.Outpost);}
  private void OutpostFinished(){if(!restoring&&Stage==DispatchStage.Outpost)Advance(DispatchStage.Collect);}
  private void ReceiverFinished(){if(!restoring&&Stage==DispatchStage.Carrying){Advance(DispatchStage.Delivered);feedback.ShowPickup("✓ Repuestos entregados");}}
  public bool CollectPackage()
  {
   if(Stage!=DispatchStage.Collect){feedback.ShowPickup(Stage<DispatchStage.Outpost?"Primero hablá con el sargento Ibarra.":"Primero hablá con Rubén.");return false;}
   Advance(DispatchStage.Carrying);feedback.ShowPickup("Repuestos guardados en la mochila");return true;
  }
  private void Advance(DispatchStage stage){Stage=stage;Refresh();Changed?.Invoke();}
  public void Restore(DispatchStage stage)
  {
   Stage=letter.MissionCompleted?(DispatchStage)Mathf.Clamp((int)stage,1,5):DispatchStage.None;Refresh();Changed?.Invoke();
  }
  private void Refresh()
  {
   sergeant.SetConversation(Stage==DispatchStage.None?sergeantBlocked:Stage==DispatchStage.Sergeant?sergeantOrder:sergeantAfter);
   outpost.SetConversation(Stage<DispatchStage.Outpost?outpostBlocked:Stage==DispatchStage.Outpost?outpostOrder:outpostAfter);
   receiver.SetConversation(Stage<DispatchStage.Carrying?receiverBlocked:Stage==DispatchStage.Carrying?receiverDelivery:receiverAfter);
   if(package)package.SetActive(Stage==DispatchStage.Collect);
  }
  public string Objective=>Stage switch
  {
   DispatchStage.Sergeant=>"Hablá con el sargento Ibarra\nEn la carpa de mando.",
   DispatchStage.Outpost=>"Hablá con Rubén\nEn el puesto avanzado.",
   DispatchStage.Collect=>"Guardá los repuestos de radio\nJunto a los cajones del puesto.",
   DispatchStage.Carrying=>"Llevá los repuestos a Benítez\nEn el puesto de radio del frente.",
   DispatchStage.Delivered=>"Seguí hacia el frente\nPor el paso entre los sacos de arena.",_=>""
  };
  public int Destination=>Stage==DispatchStage.Sergeant?5:Stage==DispatchStage.Carrying?6:Stage==DispatchStage.Delivered?3:2;
 }
}
