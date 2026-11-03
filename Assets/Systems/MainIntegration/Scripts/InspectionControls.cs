using Systems.InspectionSystem.Scripts;
using TMPro;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class InspectionControls:MonoBehaviour
 {
  [SerializeField]private InspectionScript inspection;
  [SerializeField]private TMP_Text label;
  private void LateUpdate()
  {
   bool visible=inspection.IsInspecting&&inspection.CurrentTarget&&!inspection.CurrentTarget.GetComponent<InspectableDocument>();
   label.gameObject.SetActive(visible);
   if(visible)label.text="Clic izq. + ratón · Girar   |   WASD · Mover   |   E / Esc · Dejar";
  }
 }
}
