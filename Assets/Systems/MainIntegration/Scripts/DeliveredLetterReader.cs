using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace StoryTell.MainIntegration
{
 public sealed class DeliveredLetterReader:MonoBehaviour
 {
  [SerializeField]private UiSessionCoordinator session;
  [SerializeField]private GameObject panel;
  [SerializeField]private TMP_Text body;
  [SerializeField]private ScrollRect scroll;
  private InputAction close,wheel;
  private float openedAt;
  public bool IsOpen=>panel&&panel.activeSelf;
  public string Content=>body.text;
  private void Awake(){panel.SetActive(false);}
  private void OnEnable(){close??=new InputAction("CloseDeliveredLetter",InputActionType.Button);if(close.bindings.Count==0){close.AddBinding("<Keyboard>/escape");close.AddBinding("<Keyboard>/e");close.AddBinding("<Keyboard>/l");}wheel??=new InputAction("ScrollDeliveredLetter",InputActionType.Value,"<Mouse>/scroll/y");close.Enable();wheel.Enable();}
  private void OnDisable(){Close();close?.Disable();wheel?.Disable();}
  private void OnDestroy(){close?.Dispose();wheel?.Dispose();}
  public bool Open(string text)
  {
   if(IsOpen)return true;if(!session.TryAcquire(UiMode.LetterText,this))return false;
   body.text=text;panel.SetActive(true);Canvas.ForceUpdateCanvases();body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(scroll.viewport.rect.height,body.GetPreferredValues(text,scroll.viewport.rect.width,Mathf.Infinity).y+12));scroll.verticalNormalizedPosition=1;openedAt=Time.unscaledTime;return true;
  }
  public void Close(){if(panel)panel.SetActive(false);if(session)session.Release(this);}
  private void Update()
  {
   if(!IsOpen)return;if(Time.unscaledTime-openedAt>.2f&&close.WasPressedThisFrame()){Close();return;}
   float height=scroll.content.rect.height-scroll.viewport.rect.height;if(height>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+wheel.ReadValue<float>()/120*70/height);
  }
 }
}
