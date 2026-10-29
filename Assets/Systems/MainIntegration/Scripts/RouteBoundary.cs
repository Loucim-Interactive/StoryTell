using System;
using Systems.EventSystem.Scripts;
using Systems.Missions.FindObjects;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(250)]
    public sealed class RouteBoundary : MonoBehaviour
    {
        [Serializable] public struct Section { public Vector3 from, to; [Min(1)] public float radius; }
        [Serializable] public struct Entrance { public Vector3 point; public float radius, exitZ; }
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController controller;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private LetterManager letter;
        [SerializeField] private Section[] sections;
        [SerializeField] private float frontGateZ;
        [SerializeField] private GameObject frontBarrier;
        [SerializeField] private Entrance[] entrances = System.Array.Empty<Entrance>();
        private int nextEntrance;
        private Vector3 lastSafe;
        private float nextThought;
        private bool initialized;
        public Section[] Sections => sections;
        public bool CanProceed => equipment.MissionCompleted && (!letter.MissionStarted || letter.MissionCompleted);
        public bool Contains(Vector3 position)
        {
            foreach(var s in sections)
            {
                Vector2 a=new Vector2(s.from.x,s.from.z), b=new Vector2(s.to.x,s.to.z), p=new Vector2(position.x,position.z);
                Vector2 d=b-a;float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;
                if((p-(a+d*t)).sqrMagnitude<=s.radius*s.radius)return true;
            }
            return false;
        }
        private void Start(){lastSafe=player.position;initialized=true;}
        private void LateUpdate()
        {
            bool locked=!CanProceed;
            if(frontBarrier && frontBarrier.activeSelf!=locked)frontBarrier.SetActive(locked);
            if(!initialized || !player || !controller || !controller.enabled)return;
            Vector3 current=player.position;
            if(nextEntrance<entrances.Length)
            {
                var entry=entrances[nextEntrance];var delta=current-entry.point;delta.y=0;
                if(delta.sqrMagnitude<=entry.radius*entry.radius)nextEntrance++;
            }
            bool outside=!Contains(current), gated=locked&&current.z>frontGateZ;
            bool skippedEntrance=nextEntrance<entrances.Length&&current.z>entrances[nextEntrance].exitZ;
            if(locked&&current.z>frontGateZ-2)Thought(true);
            if(!outside&&!gated&&!skippedEntrance){lastSafe=current;return;}
            // Revert movement in the same frame, including sprinting across an edge.
            controller.enabled=false;player.position=lastSafe;controller.enabled=true;
            Thought(gated);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.collider.name=="Route_OuterInvisibleWall")Thought(false);
            else if(hit.collider.name=="Front_MissionGate")Thought(true);
        }
        private void Thought(bool gated)
        {
            if(Time.unscaledTime>=nextThought)
            {
                string text=gated?(letter.MissionStarted&&!letter.MissionCompleted?"Antes de ir al frente, tengo que devolverle la carta a Mateo.":"No puedo ir al frente sin recoger mi equipo."):
                    "Tengo que seguir el camino y ayudar a mis compañeros.";
                GameEventBus.Raise(GameplayEvents.StateThought,text);nextThought=Time.unscaledTime+7;
            }
        }
        private void OnDrawGizmosSelected()
        {
            if(sections==null)return;Gizmos.color=Color.cyan;
            foreach(var s in sections){Gizmos.DrawWireSphere(s.from,s.radius);Gizmos.DrawWireSphere(s.to,s.radius);Gizmos.DrawLine(s.from,s.to);}
        }
    }
}
