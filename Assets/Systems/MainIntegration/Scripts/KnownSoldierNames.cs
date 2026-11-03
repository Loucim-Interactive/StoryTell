using System;
using DialogueSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StoryTell.MainIntegration
{
 [DefaultExecutionOrder(600)]
 public sealed class KnownSoldierNames:MonoBehaviour
 {
  [Serializable]public sealed class Soldier{public SpeakerScript speaker;public Transform head;public TMP_Text label;public string displayName;}
  [SerializeField]private Camera view;
  [SerializeField]private RectTransform area;
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private Soldier[] soldiers;
  private InputAction reveal;
  private readonly RaycastHit[] hits=new RaycastHit[32];
  private void OnEnable(){reveal??=new InputAction("KnownSoldiers",InputActionType.Button,"<Keyboard>/n");reveal.Enable();}
  private void OnDisable(){reveal?.Disable();if(soldiers!=null)foreach(var s in soldiers)if(s.label)s.label.gameObject.SetActive(false);}
  private void OnDestroy()=>reveal?.Dispose();
  private void LateUpdate()=>Refresh(reveal.IsPressed());
  public void Refresh(bool held)
  {
   foreach(var s in soldiers)
   {
    Vector3 world=s.head?s.head.position+Vector3.up*.35f:s.speaker.transform.position+Vector3.up*2.1f;
    Vector3 screen=view.WorldToScreenPoint(world);float distance=Vector3.Distance(view.transform.position,world);
    bool visible=held&&session.Mode==UiMode.None&&s.speaker.HasMet&&distance<24&&screen.z>0&&screen.x>=0&&screen.y>=0&&screen.x<=Screen.width&&screen.y<=Screen.height;
    if(visible){int count=Physics.RaycastNonAlloc(view.transform.position,(world-view.transform.position).normalized,hits,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(s.speaker.transform)&&!view.transform.IsChildOf(hits[i].transform)&&hits[i].distance<distance-.45f){visible=false;break;}}
    s.label.gameObject.SetActive(visible);if(!visible)continue;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(area,screen,null,out var point);s.label.rectTransform.anchoredPosition=point;s.label.text=s.displayName;
   }
  }
 }
}
