using Systems.WeaponSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace StoryTell.MainIntegration
{
 public sealed class CombatConclusion:MonoBehaviour
 {
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private PlayerWeaponController weapon;
  [SerializeField]private Image blackout;
  [SerializeField]private TMP_Text title;
  [SerializeField]private GameObject[] hideAtEnd;
  [SerializeField]private MainPresentation presentation;
  [SerializeField]private Systems.InspectionSystem.Scripts.InspectionScript inspection;
  [SerializeField]private Systems.DialogueSystem.Scripts.DialogueManagerScript dialogue;
  [SerializeField]private Systems.WalkieSystem.Scripts.WalkieTalkieScript radio;
  [SerializeField]private PauseMenu pause;
  public bool Ending{get;private set;}
  public bool Finished{get;private set;}
  public string Reason{get;private set;}
  private float elapsed;
  private bool locked;
  private void Awake(){blackout.gameObject.SetActive(false);title.gameObject.SetActive(false);}
  public void Finish(bool touched)
  {
   if(Ending)return;Ending=true;Reason=touched?"contact":"cleared";
   if(pause)pause.Close();if(presentation){presentation.Map.SetOpen(false,true);presentation.Puzzle.Close();}if(inspection)inspection.StopInspect();if(dialogue)dialogue.CancelConversation();if(radio)radio.ExitWalkie();
   weapon.HolsterWeapon();blackout.gameObject.SetActive(true);blackout.color=new Color(0,0,0,0);
  }
  private void LateUpdate()
  {
   if(!Ending)return;
   if(!locked){locked=session.TryAcquire(UiMode.Pause,this);if(!locked)return;foreach(var go in hideAtEnd)if(go)go.SetActive(false);}
   elapsed+=Time.unscaledDeltaTime;float alpha=Mathf.SmoothStep(0,1,elapsed/2.2f);blackout.color=new Color(0,0,0,alpha);
   if(alpha>=1){Finished=true;title.gameObject.SetActive(true);title.text="Finalización escena 1";}
  }
  private void OnDisable(){if(session)session.Release(this);}
 }
}
