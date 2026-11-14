using System;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StoryTell.MainIntegration
{
 // Brief, interruptible camera invitations; never acquires movement or interaction ownership.
 [DefaultExecutionOrder(300)]
 public sealed class ScenicFocus:MonoBehaviour
 {
  [Serializable]public sealed class View
  {
   public Transform entrance,target;
   public string caption;
   public float radius=5;
   [NonSerialized]public bool visited;
  }
  [SerializeField]private View[] views;
  [SerializeField]private Transform player;
  [SerializeField]private Camera viewCamera;
  [SerializeField]private CameraControllerScript look;
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private LevelProgress progress;
  [SerializeField]private TMP_Text hint;
  private View active,nearby;
  private InputAction focus;
  private float elapsed,automaticCooldown;
  private CharacterController body;
  public bool IsFocusing=>active!=null;
  public bool AutomaticEnabled{get;set;}=true;
  private void Awake(){if(player)body=player.GetComponent<CharacterController>();}
  private void OnEnable(){focus??=new InputAction("FocusView",InputActionType.Button,"<Keyboard>/f");focus.Enable();}
  private void OnDisable(){Cancel();focus?.Disable();if(hint)hint.gameObject.SetActive(false);}
  private void OnDestroy()=>focus?.Dispose();
  public void Cancel(){if(active!=null&&look)look.SetInputLocked(false);active=null;automaticCooldown=Time.time+10;}
  private bool Available=>look&&look.enabled&&(active!=null||!look.IsInputLocked)&&session.Mode==UiMode.None&&progress.Stage<RouteStage.Battle;
  public bool Begin(View view)
  {
   if(!Available||view==null||!view.target)return false;
   active=view;elapsed=0;view.visited=true;look.SetInputLocked(true);return true;
  }
  private void LateUpdate()
  {
   nearby=null;
   if(Available)foreach(var candidate in views)if(candidate.entrance&&Vector3.Distance(player.position,candidate.entrance.position)<candidate.radius){nearby=candidate;break;}
   hint.gameObject.SetActive(nearby!=null);
   if(nearby!=null)hint.text=IsFocusing?"Mové el ratón para seguir mirando":"[F] Mirar · "+nearby.caption;
   bool manual=focus.WasPressedThisFrame();
   if(active!=null)
   {
    if(!Available||nearby!=active||(Mouse.current!=null&&Mouse.current.delta.ReadValue().sqrMagnitude>4)||manual){Cancel();return;}
    elapsed+=Time.unscaledDeltaTime;
    var direction=active.target.position-viewCamera.transform.position;
    if(direction.sqrMagnitude>.1f)
    {
     var desired=Quaternion.LookRotation(direction);
     float yaw=desired.eulerAngles.y,pitch=Mathf.DeltaAngle(0,desired.eulerAngles.x);
     player.rotation=Quaternion.Slerp(player.rotation,Quaternion.Euler(0,yaw,0),Time.unscaledDeltaTime*2);
     look.SetCameraPitch(Mathf.LerpAngle(look.CameraPitch,pitch,Time.unscaledDeltaTime*2));
    }
    if(elapsed>=1.8f)Cancel();
    return;
   }
   if(nearby==null)return;
   if(manual){Begin(nearby);return;}
   // Automatic nudges only when the player already faces the subject (no sudden turns).
   var to=nearby.target.position-viewCamera.transform.position;
   bool stopped=!body||Vector3.ProjectOnPlane(body.velocity,Vector3.up).sqrMagnitude<.04f;
   if(AutomaticEnabled&&stopped&&!nearby.visited&&Time.time>=automaticCooldown&&Vector3.Angle(viewCamera.transform.forward,to)<50&&(Mouse.current==null||Mouse.current.delta.ReadValue().sqrMagnitude<1))Begin(nearby);
  }
 }
}
