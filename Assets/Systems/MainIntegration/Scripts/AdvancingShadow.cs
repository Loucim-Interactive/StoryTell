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
  private CapsuleCollider body;
  private float nextPath;
  public void Configure(Transform target,CombatConclusion ending){player=target;playerBody=target.GetComponent<CharacterController>();conclusion=ending;agent=GetComponent<NavMeshAgent>();body=GetComponent<CapsuleCollider>();}
  private void Update()
  {
   if(!player||!agent||!agent.isOnNavMesh)return;
   if(conclusion.Ending){agent.isStopped=true;return;}
   if(Time.time>=nextPath){agent.SetDestination(player.position);nextPath=Time.time+.25f;}
   if(TouchesPlayer())conclusion.Finish(true);
  }
  private bool TouchesPlayer()
  {
   if(!playerBody||!body)return false;
   // Both actors are upright capsules. Use their actual axes, not a broad trigger
   // or penetration against CharacterController (which PhysX may already have separated).
   var center=body.transform.TransformPoint(body.center);var other=playerBody.transform.TransformPoint(playerBody.center);
   var scale=body.transform.lossyScale;var playerScale=playerBody.transform.lossyScale;
   float radius=body.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));float playerRadius=playerBody.radius*Mathf.Max(Mathf.Abs(playerScale.x),Mathf.Abs(playerScale.z));
   float half=Mathf.Max(0,body.height*Mathf.Abs(scale.y)*.5f-radius);float playerHalf=Mathf.Max(0,playerBody.height*Mathf.Abs(playerScale.y)*.5f-playerRadius);
   float vertical=Mathf.Max(0,Mathf.Abs(center.y-other.y)-half-playerHalf);var horizontal=new Vector2(center.x-other.x,center.z-other.z);
   float contact=radius+playerRadius+playerBody.skinWidth+.01f;
   return horizontal.sqrMagnitude+vertical*vertical<=contact*contact;
  }
 }
}
