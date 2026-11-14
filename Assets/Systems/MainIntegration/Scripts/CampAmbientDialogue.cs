using System;
using DialogueSystem.Scripts;
using Systems.DialogueSystem.Scripts;
using TMPro;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class CampAmbientDialogue:MonoBehaviour
 {
  [Serializable]public sealed class Voice
  {
   public SpeakerScript actor;
   public string displayName;
   [TextArea]public string[] lines;
   [NonSerialized]public float nextTime;
   [NonSerialized]public int nextLine;
  }
  [SerializeField]private Transform player;
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private LevelProgress progress;
  [SerializeField]private DialogueManagerScript dialogue;
  [SerializeField]private TMP_Text subtitle;
  [SerializeField]private Voice[] voices;
  private float nextLine=8,hideAt,nextScan;
  public string CurrentLine=>subtitle?subtitle.text:"";
  public bool IsShowing=>subtitle&&subtitle.gameObject.activeSelf;
  private bool Available=>session.Mode==UiMode.None&&!dialogue.IsInConversation&&!dialogue.IsThoughtActive&&progress.Stage<RouteStage.Approach;
  private void Update()
  {
   if(!Available||Time.time>=hideAt)subtitle.gameObject.SetActive(false);
   if(!Available||Time.time<nextScan||Time.time<nextLine)return;
   nextScan=Time.time+.5f;
   Voice nearest=null;float distance=5*5;
   foreach(var voice in voices)
   {
    if(!voice.actor||!voice.actor.gameObject.activeInHierarchy||voice.lines==null||voice.lines.Length==0||Time.time<voice.nextTime)continue;
    float d=(voice.actor.transform.position-player.position).sqrMagnitude;
    if(d<distance){nearest=voice;distance=d;}
   }
   if(nearest==null)return;
   var source=nearest.actor.transform.position+Vector3.up*1.4f;var destination=player.position+Vector3.up*1.4f;
   if(Physics.Linecast(source,destination,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(player)&&!hit.transform.IsChildOf(nearest.actor.transform))return;
   subtitle.text=nearest.displayName+": «"+nearest.lines[nearest.nextLine++%nearest.lines.Length]+"»";
   subtitle.gameObject.SetActive(true);hideAt=Time.time+5;nextLine=Time.time+22;nearest.nextTime=Time.time+85;
  }
  private void OnDisable(){if(subtitle)subtitle.gameObject.SetActive(false);}
 }
}
