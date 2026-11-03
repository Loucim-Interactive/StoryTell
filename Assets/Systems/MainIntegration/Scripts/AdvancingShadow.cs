using UnityEngine;
using UnityEngine.AI;
namespace StoryTell.MainIntegration
{
 [RequireComponent(typeof(NavMeshAgent))]
 public sealed class AdvancingShadow:MonoBehaviour
 {
  private Transform player;
  private CharacterController playerBody;
  private NavMeshAgent agent;
  private CombatConclusion conclusion;
  private float nextPath;
  public void Configure(Transform target,CombatConclusion ending){player=target;playerBody=target.GetComponent<CharacterController>();conclusion=ending;agent=GetComponent<NavMeshAgent>();}
  private void Update()
  {
   if(!player||!agent||!agent.isOnNavMesh)return;
   if(conclusion.Ending){agent.isStopped=true;return;}
   if(Time.time>=nextPath){agent.SetDestination(player.position);nextPath=Time.time+.25f;}
   if(playerBody&&Vector3.Distance(GetComponent<Collider>().ClosestPoint(playerBody.bounds.center),playerBody.bounds.center)<=playerBody.radius+.12f)conclusion.Finish(true);
  }
  private void OnTriggerEnter(Collider other){if(player&&other.transform.IsChildOf(player))conclusion.Finish(true);}
 }
}
