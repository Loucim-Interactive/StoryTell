using Systems.InteractionSystem.Scripts;
using UnityEngine;
namespace StoryTell.MainIntegration
{
 public sealed class CampMeal:InteractableScript
 {
  [SerializeField] private CampRoutine routine;
  protected override void OnInteract(){routine.Eat();}
 }
}
