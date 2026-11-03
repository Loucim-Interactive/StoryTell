using UnityEngine;
using UnityEngine.AI;
namespace StoryTell.MainIntegration
{
 public sealed class CombatNavigation:MonoBehaviour
 {
  [SerializeField]private NavMeshData data;
  private NavMeshDataInstance instance;
  private void OnEnable(){if(data)instance=NavMesh.AddNavMeshData(data);}
  private void OnDisable(){if(instance.valid)instance.Remove();}
 }
}
