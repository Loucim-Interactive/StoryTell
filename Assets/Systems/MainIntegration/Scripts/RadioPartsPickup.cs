using Systems.InteractionSystem.Scripts;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class RadioPartsPickup:InteractableScript
 {
  [SerializeField]private SupplyDispatch dispatch;
  protected override void OnInteract()=>dispatch.CollectPackage();
 }
}
