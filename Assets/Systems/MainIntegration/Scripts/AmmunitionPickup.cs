using Systems.InteractionSystem.Scripts;
using Systems.WeaponSystem;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class AmmunitionPickup:InteractableScript
 {
  [SerializeField] private PlayerWeaponController weapon;
  [SerializeField] private MissionAnnouncement feedback;
  [SerializeField,Min(1)] private int rounds=40;
  public bool Collected{get;private set;}
  public event System.Action Changed;
  protected override void OnInteract(){if(Collected)return;RestoreCollected();feedback.ShowPickup("Munición guardada · +"+rounds);}
  public void RestoreCollected(){if(Collected)return;Collected=true;weapon.AddReserve(rounds);gameObject.SetActive(false);Changed?.Invoke();}
 }
}
